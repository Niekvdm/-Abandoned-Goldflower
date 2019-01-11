
using System.Collections.Generic;
using App.Data.Enums;
using App.Data.Models;

namespace GoldFlower
{
	public class AppState
	{
		public InstallState Status { get; set; }
		public int Progress { get; set; }
		public List<FileContainer> Files { get; set; }
		public FileContainer CurrentFile { get; set; }
		public Dictionary<string, List<string>> Events { get; set; }
	}
}