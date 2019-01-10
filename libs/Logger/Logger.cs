namespace Logger
{
	using System;
	using System.Collections.Generic;
	using App.Data;
	using App.Data.Enums;

	public class Logger
	{
		public Dictionary<string, List<string>> MessageBag { get; set; }

		public Logger()
		{
			MessageBag = new Dictionary<string, List<string>>();
		}

		public void AddError(string message)
		{
			AddMessage(MessageType.Error, message);
		}

		public void AddWarning(string message)
		{
			AddMessage(MessageType.Warning, message);
		}

		public void AddInfo(string message)
		{
			AddMessage(MessageType.Info, message);
		}

		public void AddSuccess(string message)
		{
			AddMessage(MessageType.Success, message);
		}

		public void AddMessage(MessageType type, string message)
		{
			message = $"[{DateTime.Now.ToShortTimeString()}] {message}";

			if (!MessageBag.ContainsKey(type.ToString()))
			{
				MessageBag.Add(type.ToString(), new List<string>());
			}

			MessageBag[type.ToString()].Add(message);

			Console.WriteLine(message);
		}
	}
}