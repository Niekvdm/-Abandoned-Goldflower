using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Goldtree.Commands;
using Goldtree.Commands.Enums;
using App.Data.Enums;
using Goldtree.Extensions;
using App.Data.Models;
using App;
using Logger;
using LibHac;
using LibHac.IO;
using LibUsb.Windows;
using App.Data.Interfaces;
using App.Data.Events;

namespace Goldtree
{
    public class Goldtree : IManagerDelegates, IProcessor
    {
        private UsbK _usb = null;

        public event ProgressChanged OnProgressChanged;
        public event FileChanged OnFileChanged;
        public event FileStateChanged OnFileStateChanged;
        public event InstallStateChanged OnInstallStateChanged;
        public event MessageReceived OnMessageReceived;

        public bool IsRunning { get; set; } = true;

        public void Install(List<FileContainer> files)
        {
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
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled, "No usb connection found. Are you sure the Switch is connected and that you have the correct USB drivers installed?"));
                NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "No usb connection found. Are you sure the Switch is connected and that you have the correct USB drivers installed?"));
                return;
            }

            try
            {
                foreach (var fileContainer in files)
                {
                    if (fileContainer.State == InstallState.Installing || fileContainer.State == InstallState.Failed || fileContainer.State == InstallState.Cancelled) continue;

                    NotifyProgressChanged(new ProgressChangedEventArgs(0));
                    NotifyFileChanged(new FileChangedEventArgs(fileContainer));
                    NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.AwaitingUserInput, "Awaiting user input on the Switch"));

                    NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Info, $"Processing {fileContainer.Name}"));

                    var connectionRequestCommand = new Command(CommandIds.ConnectionRequest);
                    _usb.Write(connectionRequestCommand);

                    NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Info, "Attempting to connect to Goldleaf through USB..."));

                    var connectionResponseCommand = _usb.Read();

                    if (connectionResponseCommand.MagicOk())
                    {
                        if (connectionResponseCommand.IsCommandId(CommandIds.ConnectionResponse))
                        {
                            NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Installing));
                            NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Info, "Connection established with Goldleaf"));

                            var nspNameCommand = new Command(CommandIds.NSPName);
                            _usb.Write(nspNameCommand);
                            _usb.Write((uint)fileContainer.Name.Length);
                            _usb.Write(fileContainer.Name);

                            NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Info, "NSP info was sent to Goldleaf. Waiting for approval..."));

                            var startCommand = _usb.Read();

                            if (startCommand.MagicOk())
                            {
                                if (startCommand.IsCommandId(CommandIds.Start))
                                {
                                    NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Info, "Goldleaf is ready for installation"));

                                    NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Installing));

                                    try
                                    {
                                        FileStream fs = new FileStream(fileContainer.FullName, FileMode.Open);
                                        StreamStorage ist = new StreamStorage(fs, true);
                                        Pfs pnsp = new Pfs(ist);
                                        ist.Dispose();
                                        fs.Close();
                                        fs = new FileStream(fileContainer.FullName, FileMode.Open);
                                        uint filecount = (uint)pnsp.Files.Length;

                                        var nspDataCommand = new Command(CommandIds.NSPData);
                                        _usb.Write(nspDataCommand);
                                        _usb.Write(filecount);

                                        int tikidx = -1;
                                        int certidx = -1;
                                        int tmpidx = 0;

                                        foreach (var file in pnsp.Files)
                                        {
                                            ulong offset = (ulong)pnsp.HeaderSize + (ulong)file.Offset;
                                            ulong size = (ulong)file.Size;
                                            uint len = (uint)file.Name.Length;
                                            _usb.Write(len);
                                            _usb.Write(file.Name);
                                            _usb.Write(offset);
                                            _usb.Write(size);
                                            if (Path.GetExtension(file.Name).Replace(".", "").ToLower() == "tik") tikidx = tmpidx;
                                            else if (Path.GetExtension(file.Name).Replace(".", "").ToLower() == "cert") certidx = tmpidx;
                                            tmpidx++;
                                        }

                                        using (var br = new BinaryReader(fs))
                                        {
                                            while (IsRunning)
                                            {
                                                var mainStreamCommand = _usb.Read();

                                                if (mainStreamCommand.MagicOk())
                                                {
                                                    if (mainStreamCommand.IsCommandId(CommandIds.NSPContent))
                                                    {
                                                        _usb.Read(out uint idx);

                                                        NotifyProgressChanged(new ProgressChangedEventArgs(0));

                                                        NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, $"Sending content \"{pnsp.Files[idx].Name}\"... {(idx + 1)} / {pnsp.Files.Length}"));

                                                        PfsFileEntry ent = pnsp.Files[idx];
                                                        long rsize = 1048576;
                                                        long coffset = pnsp.HeaderSize + ent.Offset;
                                                        long toread = ent.Size;
                                                        long tmpread = 1;
                                                        long bytesSent = 0;
                                                        byte[] bufb;

                                                        while ((tmpread > 0) && (toread > 0) && (coffset < (coffset + ent.Size)))
                                                        {
                                                            NotifyProgressChanged(new ProgressChangedEventArgs((int)(bytesSent * 100 / ent.Size)));
                                                            if (rsize >= ent.Size) rsize = ent.Size;
                                                            int tor = (int)Math.Min(rsize, toread);
                                                            bufb = new byte[tor];
                                                            br.BaseStream.Position = coffset;
                                                            tmpread = br.Read(bufb, 0, bufb.Length);

                                                            _usb.Write(bufb);

                                                            bytesSent += tmpread;
                                                            coffset += tmpread;
                                                            toread -= tmpread;
                                                        }

                                                        NotifyProgressChanged(new ProgressChangedEventArgs(100));

                                                        NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Content was sent to Goldleaf"));
                                                    }
                                                    else if (mainStreamCommand.IsCommandId(CommandIds.NSPTicket))
                                                    {
                                                        NotifyProgressChanged(new ProgressChangedEventArgs(0));
                                                        NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Sending ticket file..."));

                                                        PfsFileEntry tik = pnsp.Files[tikidx];
                                                        br.BaseStream.Seek(pnsp.HeaderSize + tik.Offset, SeekOrigin.Begin);
                                                        byte[] file = br.ReadBytes((int)tik.Size);
                                                        _usb.Write(file);

                                                        NotifyProgressChanged(new ProgressChangedEventArgs(100));
                                                        NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Ticket was sent to Goldleaf."));
                                                    }
                                                    else if (mainStreamCommand.IsCommandId(CommandIds.NSPCert))
                                                    {
                                                        NotifyProgressChanged(new ProgressChangedEventArgs(0));
                                                        NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Sending certificate file..."));

                                                        PfsFileEntry cert = pnsp.Files[certidx];
                                                        br.BaseStream.Seek(pnsp.HeaderSize + cert.Offset, SeekOrigin.Begin);
                                                        byte[] file = br.ReadBytes((int)cert.Size);
                                                        _usb.Write(file);
                                                        NotifyProgressChanged(new ProgressChangedEventArgs(100));
                                                        NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Debug, "Certificate was sent to Goldleaf."));
                                                    }
                                                    else if (mainStreamCommand.IsCommandId(CommandIds.Finish))
                                                    {
                                                        NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Finished));
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "Invalid command received. Are you sure Goldleaf is active?"));
                                                        NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Failed));
                                                        NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Failed));
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "Invalid command received. Are you sure Goldleaf is active?"));
                                                    NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled));
                                                    NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Failed));
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "An error occured opening the select NSP. Are you sure it's a valid NSP?"));
                                        Console.WriteLine(ex.Message);
                                        Console.WriteLine(ex.StackTrace);
                                        NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Failed));
                                    }
                                }
                                else if (startCommand.IsCommandId(CommandIds.Finish))
                                {
                                    NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "Goldleaf has cancelled the installation"));
                                    NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Cancelled));
                                }
                                else
                                {
                                    NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "Invalid command received. Are you sure Goldleaf is active?"));
                                    NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Failed));
                                    NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled));
                                }
                            }
                            else
                            {
                                NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "Invalid command received. Are you sure Goldleaf is active?"));
                                NotifyFileStateChanged(new FileStateChangedEventArgs(InstallState.Failed));
                                NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled));
                            }
                        }
                        else if (connectionResponseCommand.IsCommandId(CommandIds.Finish))
                        {
                            NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "Goldleaf has cancelled the installation"));
                        }
                        else
                        {
                            NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "Invalid command received. Are you sure Goldleaf is active?"));
                            NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled));
                        }
                    }
                    else
                    {
                        NotifiyMessageReceived(new MessageReceivedEventArgs(MessageType.Error, "Invalid command received. Are you sure Goldleaf is active?"));
                        NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled));
                    }
                }
                // }
                // else
                // {
                //     AddWarning("Unable to connect to Goldleaf. Are you sure Goldleaf is active and you have the correct USB drivers installed?");
                //     Manager.InstallState = InstallState.Cancelled;
                // }
            }
            catch (Exception ex)
            {
                NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Cancelled));
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }


            NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Finished));

            Close();
        }

        public void Abort()
        {
            NotifyInstallStateChanged(new InstallStateChangedEventArgs(InstallState.Aborted));
            Close();
        }

        private void Close()
        {
            if (_usb != null)
            {
                var finishCommand = new Command(CommandIds.Finish);

                try
                {
                    _usb.Write(finishCommand);
                }
                catch { }
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
            if (e.State == InstallState.Cancelled || e.State == InstallState.Aborted || e.State == InstallState.Failed)
            {
                IsRunning = false;
            }

            OnInstallStateChanged?.Invoke(e);
        }

        public void NotifiyMessageReceived(MessageReceivedEventArgs e)
        {
            OnMessageReceived?.Invoke(e);
        }
    }
}
