using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Tinfoil.Commands;
using Tinfoil.Commands.Enums;
using LibUsb.Windows;

namespace Tinfoil.Extensions
{
    public static class UsbKWriteExtensions
    {
        public static void Write(this UsbK USB, byte[] Data)
        {
            USB.WritePipe(1, Data, Data.Length, out _, IntPtr.Zero);
        }

        public static void Write(this UsbK USB, uint Data)
        {
            USB.Write(BitConverter.GetBytes(Data));
        }

        public static void Write(this UsbK USB, ulong Data)
        {
            USB.Write(BitConverter.GetBytes(Data));
        }

        public static void Write(this UsbK USB, string Data)
        {
            USB.Write(Encoding.UTF8.GetBytes(Data));
        }
    }

    public static class UsbKReadExtensions
    {
        public static byte[] Read(this UsbK USB, int Length)
        {
            byte[] b = new byte[Length];
            USB.ReadPipe(0x81, b, Length, out _, IntPtr.Zero);
            return b;
        }

        public static void Read(this UsbK USB, out uint Data)
        {
            Data = BitConverter.ToUInt32(USB.Read(4), 0);
        }

        public static void Read(this UsbK USB, out ulong Data)
        {
            Data = BitConverter.ToUInt64(USB.Read(8), 0);
        }

        public static void Read(this UsbK USB, out string Data, uint Length)
        {
            Data = Encoding.UTF8.GetString(USB.Read((int)Length));
        }
    }
}