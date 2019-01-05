using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Goldtree.Commands;
using Goldtree.Commands.Enums;
using Goldtree.Enums;
using Goldtree.Models;
using LibHac;
using LibHac.IO;
using libusbK;

namespace Goldtree
{
    public class Goldtree
    {
        private UsbK _usb = null;
        
        public Goldtree() { }

        private void AddError(string message)
        {
            AddMessage("error", message);
        }

        private void AddWarning(string message)
        {
            AddMessage("warning", message);
        }

        private void AddInfo(string message)
        {
            AddMessage("info", message);
        }

        private void AddSuccess(string message)
        {
            AddMessage("success", message);
        }

        private void AddMessage(string type, string message)
        {
            message = $"[{DateTime.Now.ToShortTimeString()}] {message}";

            if (!ApplicationState.MessageBag.ContainsKey(type))
            {
                ApplicationState.MessageBag.Add(type, new List<string>());
            }

            ApplicationState.MessageBag[type].Add(message);

            Console.WriteLine(message);
        }

        public void Install()
        {
            var files = ApplicationState.Files;

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

                ApplicationState.InstallState = InstallState.Cancelled;
                AddError("No usb connection found. Are you sure the Switch is connected and that you have the correct USB drivers installed?");
                return;
            }

            try
            {
                foreach (var fileContainer in files)
                {
                    if(fileContainer.State == InstallState.Installing || fileContainer.State == InstallState.Failed) continue;
                    ApplicationState.Progress = 0;
                    ApplicationState.CurrentFile = fileContainer;
                    ApplicationState.InstallState = InstallState.AwaitingUserInput;

                    AddInfo($"Processing {fileContainer.Name}");

                    var connectionRequestCommand = new Command(CommandIds.ConnectionRequest);
                    _usb.Write(connectionRequestCommand);

                    AddInfo("Attempting to connect to Goldleaf through USB...");

                    var connectionResponseCommand = _usb.Read();

                    if (connectionResponseCommand.MagicOk())
                    {
                        if (connectionResponseCommand.IsCommandId(CommandIds.ConnectionResponse))
                        {
                            ApplicationState.InstallState = InstallState.Installing;
                            AddInfo("Connection established with Goldleaf");

                            var nspNameCommand = new Command(CommandIds.NSPName);
                            _usb.Write(nspNameCommand);
                            _usb.Write((uint)fileContainer.Name.Length);
                            _usb.Write(fileContainer.Name);

                            AddInfo("NSP info was sent to Goldleaf. Waiting for approval...");

                            var startCommand = _usb.Read();

                            if (startCommand.MagicOk())
                            {
                                if (startCommand.IsCommandId(CommandIds.Start))
                                {
                                    AddInfo("Goldleaf is ready for installation");

                                    fileContainer.State = InstallState.Installing;

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
                                            while (ApplicationState.InstallState.Equals(InstallState.Installing))
                                            {
                                                var mainStreamCommand = _usb.Read();

                                                if (mainStreamCommand.MagicOk())
                                                {
                                                    if (mainStreamCommand.IsCommandId(CommandIds.NSPContent))
                                                    {
                                                        _usb.Read(out uint idx);

                                                        ApplicationState.Progress = 0;

                                                        AddInfo($"Sending content \"{pnsp.Files[idx].Name}\"... {(idx + 1)} / {pnsp.Files.Length}");

                                                        PfsFileEntry ent = pnsp.Files[idx];
                                                        long rsize = 1048576;
                                                        long coffset = pnsp.HeaderSize + ent.Offset;
                                                        long toread = ent.Size;
                                                        long tmpread = 1;
                                                        long bytesSent = 0;
                                                        byte[] bufb;

                                                        while ((tmpread > 0) && (toread > 0) && (coffset < (coffset + ent.Size)))
                                                        {
                                                            ApplicationState.Progress = (int)(bytesSent * 100 / ent.Size);
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

                                                        ApplicationState.Progress = 100;
                                                        AddInfo("Content was sent to Goldleaf");
                                                    }
                                                    else if (mainStreamCommand.IsCommandId(CommandIds.NSPTicket))
                                                    {
                                                        ApplicationState.Progress = 0;
                                                        AddInfo("Sending ticket file...");

                                                        PfsFileEntry tik = pnsp.Files[tikidx];
                                                        br.BaseStream.Seek(pnsp.HeaderSize + tik.Offset, SeekOrigin.Begin);
                                                        byte[] file = br.ReadBytes((int)tik.Size);
                                                        _usb.Write(file);

                                                        ApplicationState.Progress = 100;
                                                        AddInfo("Ticket was sent to Goldleaf.");
                                                    }
                                                    else if (mainStreamCommand.IsCommandId(CommandIds.NSPCert))
                                                    {
                                                        ApplicationState.Progress = 0;
                                                        AddInfo("Sending certificate file...");

                                                        PfsFileEntry cert = pnsp.Files[certidx];
                                                        br.BaseStream.Seek(pnsp.HeaderSize + cert.Offset, SeekOrigin.Begin);
                                                        byte[] file = br.ReadBytes((int)cert.Size);
                                                        _usb.Write(file);
                                                        ApplicationState.Progress = 100;
                                                        AddInfo("Certificate was sent to Goldleaf.");
                                                    }
                                                    else if (mainStreamCommand.IsCommandId(CommandIds.Finish))
                                                    {
                                                        fileContainer.State = InstallState.Finished;
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        AddError("Invalid command received. Are you sure Goldleaf is active?");
                                                        ApplicationState.InstallState = InstallState.Cancelled;
                                                        fileContainer.State = InstallState.Failed;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    AddError("Invalid command received. Are you sure Goldleaf is active?");
                                                    ApplicationState.InstallState = InstallState.Cancelled;
                                                    fileContainer.State = InstallState.Failed;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                        Console.WriteLine(ex.StackTrace);
                                        AddError("An error occured opening the select NSP. Are you sure it's a valid NSP?");
                                        fileContainer.State = InstallState.Failed;
                                    }
                                }
                                else if (startCommand.IsCommandId(CommandIds.Finish))
                                {
                                    AddError("Goldleaf has cancelled the installation");
                                }
                                else
                                {
                                    AddError("Invalid command received. Are you sure Goldleaf is active?");
                                    ApplicationState.InstallState = InstallState.Cancelled;
                                }
                            }
                            else
                            {
                                AddError("Invalid command received. Are you sure Goldleaf is active?");
                                ApplicationState.InstallState = InstallState.Cancelled;
                            }
                        }
                        else if (connectionResponseCommand.IsCommandId(CommandIds.Finish))
                        {
                            AddError("Goldleaf has cancelled the installation");
                        }
                        else
                        {
                            AddError("Invalid command received. Are you sure Goldleaf is active?");
                            ApplicationState.InstallState = InstallState.Cancelled;
                        }
                    }
                    else
                    {
                        AddError("Invalid command received. Are you sure Goldleaf is active?");
                        ApplicationState.InstallState = InstallState.Cancelled;
                    }
                }
                // }
                // else
                // {
                //     AddWarning("Unable to connect to Goldleaf. Are you sure Goldleaf is active and you have the correct USB drivers installed?");
                //     ApplicationState.InstallState = InstallState.Cancelled;
                // }
            }
            catch (Exception ex)
            {
                ApplicationState.InstallState = InstallState.Cancelled;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }


            if (ApplicationState.InstallState != InstallState.Cancelled)
            {
                AddSuccess("Finished installing");
                ApplicationState.InstallState = InstallState.Finished;
            }

            Close();
        }

        public void Abort()
        {
            ApplicationState.InstallState = InstallState.Aborted;
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
    }
}
