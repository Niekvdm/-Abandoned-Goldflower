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
using Tinfoil.Commands.Enums;
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

		private UsbK _usb = null;
		private bool _isRunning = false;

		private List<FileContainer> _files = null;

		private InstallState _state = InstallState.Idle;

		private FileContainer _currentFile = null;
		private float _currentProgress = 0;


		private ulong BUFFER_DATA_LENGTH = 0x100000;
		private ulong PADDING_LENGTH = 0x1000;


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

				var magic = Encoding.UTF8.GetString(GetBytesFromBuffer(buffer, 0, 4));

				if (magic != "TUC0") continue;

				var command = BitConverter.ToUInt32(GetBytesFromBuffer(buffer, 8, 12), 0);

				if (command == CommandIds.Exit)
				{
					NotifyProgressChanged(new ProgressChangedEventArgs(100));
					NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Finished));
					NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Success, $"Fininshed installing {_currentFile.Name}"));
					NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Finished));
					break;
				}
				else if (command == CommandIds.Nsp || command == CommandIds.NspPadding)
				{
					NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, $"Command: {command}"));

					NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Installing));
					SendNSPContent(command, command == CommandIds.NspPadding ? true : false);
				}
			}
		}

		private byte[] GetBytesFromBuffer(byte[] buffer, int startIndex, int endIndex)
		{
			int length = Math.Abs(startIndex - endIndex);
			byte[] result = new byte[length];
			Array.Copy(buffer, startIndex, result, 0, length);
			return result;
		}

		private void SendResponseHeader(uint command, ulong length)
		{
			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Sending response header to the Switch"));

			_usb.Write("TUC0");
			_usb.Write(new byte[] { CommandIds.TypeResponse, 0x00, 0x00, 0x00 });
			_usb.Write(command);
			_usb.Write(length);
			_usb.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Response header received by the Switch"));
		}

		private void SendNSPContent(uint command, bool padding = false)
		{
			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Sending NSP part to the Switch"));

			byte[] buffer = _usb.Read(32);

			ulong requestedPartLength = BitConverter.ToUInt64(GetBytesFromBuffer(buffer, 0, 8), 0);
			long requestedPartOffset = (long)BitConverter.ToUInt64(GetBytesFromBuffer(buffer, 8, 16), 0);
			ulong nspNameLength = BitConverter.ToUInt64(GetBytesFromBuffer(buffer, 16, 24), 0);

			// Read NSP file name
			buffer = _usb.Read((int)nspNameLength);
			string nspName = Encoding.UTF8.GetString(buffer);

			NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, $"Part size: {requestedPartLength}, Part offset: {requestedPartOffset}, Name length: {nspNameLength}, Name: {nspName}"));

			SendResponseHeader(command, requestedPartLength);

			var file = _files.FirstOrDefault(x => x.Name.ToLower().Equals(nspName.ToLower()));

			if (file == null)
			{
				NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled, "File does not exists or it's name is malformed"));
				NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, $"Name received from Tinfoil: {nspName}"));
				return;
			}

			if (_currentFile != file)
			{
				if (_currentFile != null)
				{
					NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Finished));
					NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Success, $"Fininshed installing {_currentFile.Name}"));
				}

				_currentFile = file;
				_currentProgress = 0;
				NotifyProgressChanged(new ProgressChangedEventArgs((int)_currentProgress));
				NotifyFileChanged(new FileChangedEventArgs(file));
				NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Installing));
				NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Info, $"Installing: {_currentFile.Name}"));
			}

			try
			{
				// Send NSP content
				using (var fileStream = new FileStream(file.FullName, FileMode.Open))
				{
					using (var binaryReader = new BinaryReader(fileStream))
					{
						ulong currentOffset = 0;
						ulong endOffset = requestedPartLength;
						ulong readLength = BUFFER_DATA_LENGTH;

						if (padding)
						{
							readLength -= PADDING_LENGTH;
						}

						long bytesSent = 0;
						long totalBytes = new FileInfo(file.FullName).Length;

						binaryReader.BaseStream.Seek(requestedPartOffset, SeekOrigin.Begin);

						float tmpProgress = 0;
						byte[] readBuffer;

						while (currentOffset < endOffset)
						{
							if (currentOffset + readLength >= endOffset)
							{
								readLength = endOffset - currentOffset;
							}

							readBuffer = new byte[readLength];
							var bytesRead = binaryReader.Read(readBuffer, 0, (int)readLength);

							if (padding)
							{
								byte[] paddedBuffer = new byte[(int)PADDING_LENGTH + readBuffer.Length];

								for (var i = 0; i < (int)PADDING_LENGTH; i++)
								{
									paddedBuffer[i] = 0x00;
								}

								Array.Copy(readBuffer, 0, paddedBuffer, (int)PADDING_LENGTH, readBuffer.Length);

								readBuffer = paddedBuffer;
							}

							_usb.Write(readBuffer);

							currentOffset += readLength;
							bytesSent += (long)readLength;
							tmpProgress = bytesSent * 100 / totalBytes;

							NotifyProgressChanged(new ProgressChangedEventArgs((int)tmpProgress));
						}

						_currentProgress += tmpProgress;

						if (_currentProgress > 100)
						{
							_currentProgress = 100;
							NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Finished));
						}

						NotifyProgressChanged(new ProgressChangedEventArgs((int)_currentProgress));
					}
				}

				//NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Finished));
				NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "NSP part received by the Switch"));
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
