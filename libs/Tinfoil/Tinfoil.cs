using System;
using System.Collections.Generic;
using System.Text;
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
		private bool _isRunning = true;


		public void Abort()
		{
			_isRunning = false;
			Disconnect();
		}

		public void Install(List<FileContainer> files)
		{
			if (!Connect()) return;
			SendNSPList(files);
            PollCommands();
			Disconnect();
		}

		private bool Connect()
		{
			bool result = true;

			try
			{
				// Try to connect to the switch
				//_usb = new UsbHandler(0x57E, 0x3000);

				var pat = new KLST_PATTERN_MATCH { DeviceID = @"USB\VID_057E&PID_3000" };
				var lst = new LstK(0, ref pat);
				lst.MoveNext(out var dinfo);
				_usb = new UsbK(dinfo);
			}
			catch (Exception ex)
			{
				result = false;
				NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled, "No usb connection found. Are you sure the Switch is connected and that you have the correct USB drivers installed?"));
				NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "No usb connection found. Are you sure the Switch is connected and that you have the correct USB drivers installed?"));
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
			}

			return result;
		}

		private void Disconnect()
		{
			if (_usb != null)
			{
				try
				{
					//_usb.Write(CommandExit);
				}
				catch { }
			}
		}

		private void SendNSPList(List<FileContainer> files)
		{
			var length = 0;
			foreach (var file in files)
			{
				length += file.Name.Length + 1;
			}

			_usb.Write(Encoding.UTF8.GetBytes("TUL0"));
			_usb.Write((uint)length);
			_usb.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

			foreach (var file in files)
			{
				_usb.Write(Encoding.UTF8.GetBytes($"{file.Name}\n"));
			}
		}

		private void PollCommands()
		{
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
					NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Finished, "Finished I guess??"));
					break;
				}
				else if (BitConverter.ToUInt32(command, 0) == 1)
				{
					NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled, "NYI"));
                    break;
					//SendNSPContent();
				}
			}
		}

		private void SendResponseHeader(uint command, ulong length)
		{
			_usb.Write(Encoding.UTF8.GetBytes("TUC0"));
			_usb.Write(new byte[] { CommandTypeResponse, 0, 0, 0 });

			var buffer = new byte[4];
			_usb.Write(command);

			buffer = new byte[8];
			_usb.Write(length);

			_usb.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
		}

		private void SendNSPContent()
		{
			throw new NotImplementedException();
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
