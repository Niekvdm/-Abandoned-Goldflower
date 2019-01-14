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
		private int _logLevel = 3;
		public List<MessageItem> MessageBag { get; set; }

		public Logger(int logLevel = 3)
		{
			_logLevel = logLevel;
			MessageBag = new List<MessageItem>();
		}

		public void SetLogLevel(int logLevel)
		{
			_logLevel = logLevel;
		}

		public void AddError(string message)
		{
			if (_logLevel >= 0)
			{
				AddMessage(MessageType.Error, message);
			}
		}

		public void AddWarning(string message)
		{
			if (_logLevel >= 1)
			{
				AddMessage(MessageType.Warning, message);
			}
		}

		public void AddSuccess(string message)
		{
			if (_logLevel >= 2)
			{
				AddMessage(MessageType.Success, message);
			}
		}

		public void AddInfo(string message)
		{
			if (_logLevel >= 3)
			{
				AddMessage(MessageType.Info, message);
			}
		}

		public void AddMessage(MessageType type, string message)
		{
			if (_logLevel < (int)type) return;

			if (MessageBag.Count >= 50)
			{
				MessageBag.RemoveAt(0);
			}

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