using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using App.Data.Enums;
using App.Data.Events;
using App.Data.Interfaces;
using App.Data.Models;
using LibUsb.Windows;
using Tinfoil.Extensions;

namespace Tinfoil
{
	public class Tinfoil : IProcessor
	{

		public event ProgressChanged OnProgressChanged;
		public event FileChanged OnFileChanged;
		public event FileStateChanged OnFileStateChanged;
		public event InstallStateChanged OnInstallStateChanged;
		public event MessageReceived OnMessageReceived;

		private byte CommandTypeResponse = 1;
		private ulong CommandExit = 0;
		private uint CommandRequestNSP = 1;

		private UsbK _usb = null;
		private bool _isRunning = false;

		private List<FileContainer> _files = null;

		private InstallState _state = InstallState.Idle;

		private FileContainer _currentFile = null;
		private int _currentProgress = 0;


		public void Abort()
		{
			_isRunning = false;
			NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Aborted, "Abort command received from user"));
			Disconnect();
		}

		public void Install(List<FileContainer> files)
		{
			_files = files;
			_isRunning = true;

			var isConnected = false;

			while (_isRunning)
			{
				isConnected = Connect();
				if (isConnected) break;

				NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.AwaitingUserInput));

				Thread.Sleep(2500);
			}

			if (isConnected)
			{
				SendNSPList();
				PollCommands();
				Disconnect();
			}
		}

		private bool Connect()
		{
			bool result = true;

			try
			{
				NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Attempting to connect to the Switch"));

				// Try to connect to the switch
				var pat = new KLST_PATTERN_MATCH { DeviceID = @"USB\VID_057E&PID_3000" };
				var lst = new LstK(0, ref pat);
				lst.MoveNext(out var dinfo);
				_usb = new UsbK(dinfo);

				NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Successfully connected to the Switch"));
			}
			catch (Exception ex)
			{
				result = false;
				NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "Failed to connect to the Switch"));
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
			}

			return result;
		}

		private void Disconnect()
		{
			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Disconnecting..."));

			if (_state.Equals(InstallState.AwaitingUserInput))
			{
				NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled, "Install process abruptly ended"));
			}

			_isRunning = false;

			if (_usb != null)
			{
				_usb = null;

				try
				{
					// Disabled as it is making Tinfoil crash the Switch
					//_usb.Write(CommandExit);

				}
				catch { }
			}
		}

		private void SendNSPList()
		{
			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Sending NSP list to the Switch"));

			var length = 0;
			foreach (var file in _files)
			{
				length += file.Name.Length + 1;
			}

			_usb.Write(Encoding.UTF8.GetBytes("TUL0"));
			_usb.Write((uint)length);
			_usb.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

			foreach (var file in _files)
			{
				_usb.Write(Encoding.UTF8.GetBytes($"{file.Name}\n"));
			}

			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "NSP list received by the Switch"));
		}

		private void PollCommands()
		{
			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Polling for commands from Tinfoil"));

			while (_isRunning)
			{
				var buffer = _usb.Read(32);

				var magic = new byte[4];
				Array.Copy(buffer, magic, 4);

				if (Encoding.UTF8.GetString(magic) != "TUC0") continue;

				var command = new byte[4];
				Array.Copy(buffer, 8, command, 0, 4);

				if (BitConverter.ToUInt32(command, 0) == CommandExit)
				{
					NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Finished));
					break;
				}
				else if (BitConverter.ToUInt32(command, 0) == 1)
				{
					NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Installing));
					SendNSPContent();
				}
			}
		}

		private void SendResponseHeader(uint command, ulong length)
		{
			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Sending response header to the Switch"));

			_usb.Write(Encoding.UTF8.GetBytes("TUC0"));
			_usb.Write(new byte[] { CommandTypeResponse, 0, 0, 0 });
			_usb.Write(command);
			_usb.Write(length);
			_usb.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Response header received by the Switch"));
		}

		private void SendNSPContent()
		{
			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Sending NSP content to the Switch"));

			var buffer = _usb.Read(32);

			var requestedLengthBytes = new byte[8];
			Array.Copy(buffer, 0, requestedLengthBytes, 0, 8);
			var requestedLength = BitConverter.ToUInt64(requestedLengthBytes, 0);

			var offsetBytes = new byte[8];
			Array.Copy(buffer, 8, offsetBytes, 0, 8);
			var offset = (long)BitConverter.ToUInt64(offsetBytes, 0);

			var nameLengthBytes = new byte[8];
			Array.Copy(buffer, 16, nameLengthBytes, 0, 8);
			var nameLength = BitConverter.ToUInt64(nameLengthBytes, 0);

			// Read NSP file name
			var nameBytes = _usb.Read((int)nameLength);
			var name = Encoding.UTF8.GetString(nameBytes);

			SendResponseHeader(CommandRequestNSP, requestedLength);

			var file = _files.FirstOrDefault(x => x.Name.ToLower().Equals(name.ToLower()));

			if (file == null)
			{
				NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled, "File does not exists or it's name is malformed"));
				NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, $"Name received from Tinfoil: {name}"));
				return;
			}

			if (_currentFile != file)
			{
				if(file != null) {
					NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Finished));
				}

				_currentFile = file;
				_currentProgress = 0;
				NotifyProgressChanged(new ProgressChangedEventArgs(_currentProgress));
				NotifyFileChanged(new FileChangedEventArgs(file));
				NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Installing));
			}

			try
			{
				// Send NSP content
				using (var fileStream = new FileStream(file.FullName, FileMode.Open))
				{
					using (var binaryReader = new BinaryReader(fileStream))
					{
						ulong currentOffset = (ulong)offset;
						ulong readLength = 1048576;
						long bytesSent = 0;
						long totalBytes = new FileInfo(file.FullName).Length;

						binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);

						int tmpProgress = 0;

						while (currentOffset < requestedLength)
						{
							if (currentOffset + readLength >= requestedLength)
							{
								readLength = requestedLength - currentOffset;
							}

							binaryReader.BaseStream.Position = (long)currentOffset;

							var readBuffer = new byte[readLength];
							var bytesRead = binaryReader.Read(readBuffer, 0, (int)readLength);

							_usb.Write(readBuffer);

							currentOffset += (ulong)bytesRead;
							bytesSent += bytesRead;
							tmpProgress = (int)(bytesSent * 100 / totalBytes);

							NotifyProgressChanged(new ProgressChangedEventArgs(tmpProgress));
						}

						_currentProgress += tmpProgress;

						if (_currentProgress > 100)
						{
							_currentProgress = 100;
							NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Finished));
						}

						NotifyProgressChanged(new ProgressChangedEventArgs(_currentProgress));
					}
				}

				//NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Finished));
				//NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "NSP content received by the Switch"));
			}
			catch (Exception ex)
			{
				// Oops, we got an exception along the way
				NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Failed));
				NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled, ex.Message));
				NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Failed to send NSP content to the Switch"));
			}
		}

		public void NotifyProgressChanged(ProgressChangedEventArgs e)
		{
			OnProgressChanged?.Invoke(e);
		}

		public void NotifyFileChanged(FileChangedEventArgs e)
		{
			OnFileChanged?.Invoke(e);
		}

		public void NotifyFileStateChanged(FileStateChangedEventArgs e)
		{
			OnFileStateChanged?.Invoke(e);
		}

		public void NotifyInstallStateChanged(InstallStateChangedEventArgs e)
		{
			_state = e.State;

			if (e.State == InstallState.Cancelled || e.State == InstallState.Aborted || e.State == InstallState.Failed)
			{
				_isRunning = false;
			}

			OnInstallStateChanged?.Invoke(e);
		}

		public void NotifiyMessageReceived(MessageReceivedEventArgs e)
		{
			OnMessageReceived?.Invoke(e);
		}
	}
}
