using System;
using System.Collections.Generic;
using System.Text;

namespace App.Data.Models
{
    public class DirectoryContainer
	{
		private string _path = string.Empty;

		public string Path
		{
			get
			{
				var val = _path?.Trim();

				// Make sure we dont start at root
				if (val.Length == 1 && val.StartsWith("/") || val.StartsWith("\\"))
				{
					val = string.Empty;
				}

				return val;
			}
			set
			{
				_path = value;
			}
		}
	}
}