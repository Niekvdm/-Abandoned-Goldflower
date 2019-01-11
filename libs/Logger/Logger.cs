namespace Logger
{
	using System;
	using System.Collections.Generic;
	using App.Data;
	using App.Data.Enums;

	public class MessageItem
	{
		public MessageType Type { get; set; }
		public DateTime DateTime { get; set; }
		public string Message { get; set; }
	}

	public class Logger
	{
		public List<MessageItem> MessageBag { get; set; }

		public Logger()
		{
			MessageBag = new List<MessageItem>();
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
			MessageBag.Add(new MessageItem()
			{
				Type = type,
				DateTime = DateTime.Now,
				Message = message
			});

			Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] {message}");
		}
	}
}