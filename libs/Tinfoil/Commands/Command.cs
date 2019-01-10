using Tinfoil.Commands.Enums;
using System;
using System.Collections.Generic;

namespace Tinfoil.Commands
{
	public class Command
	{
		public uint Magic { get; set; }
		public CommandIds CommandId { get; set; }

		public static readonly uint GLUC = 0x43554c47;

		public Command()
		{
			Magic = GLUC;
		}

		public Command(CommandIds commandId)
		{
			Magic = GLUC;
			this.CommandId = commandId;
		}

		public bool MagicOk()
		{
			return (Magic == GLUC);
		}

		public bool IsCommandId(CommandIds commandId)
		{
			return (CommandId == commandId);
		}

		public byte[] AsData()
		{
			List<byte> fcmd = new List<byte>();
			byte[] emg = BitConverter.GetBytes(Magic);
			fcmd.AddRange(emg);
			fcmd.AddRange(BitConverter.GetBytes((uint)CommandId));
			return fcmd.ToArray();
		}
	}
}
