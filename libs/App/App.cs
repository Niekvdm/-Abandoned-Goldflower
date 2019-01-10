using Logger;
using App.Data;
using App.Data.Models;
using App.Data.Interfaces;
using App.Data.Events;
using System.Linq;
using System;
using System.Threading;
using System.Collections.Generic;
using App.Data.Enums;

namespace GoldFlower
{
	public class App
	{
		private static App _instance = null;

		public static App Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new App();
				}

				return _instance;
			}
		}

		public Logger.Logger Logger { get; set; }
		private IProcessor _processor = null;

		public List<FileContainer> Files = null;
		public InstallState InstallState { get; set; } = InstallState.Idle;
		public FileContainer CurrentFile { get; set; }
		public int Progress { get; set; }

		public App()
		{
			Logger = new Logger.Logger();
		}

		public void SetProcessor(IProcessor processor)
		{
			_processor = processor;
			_processor.OnProgressChanged += this.OnProgressChanged;
			_processor.OnFileChanged += this.OnFileChanged;
			_processor.OnFileStateChanged += this.OnFileStateChanged;
			_processor.OnInstallStateChanged += this.OnInstallStateChanged;
			_processor.OnMessageReceived += this.OnMessageReceived;
		}

		public void SetFiles(List<FileContainer> files)
		{
			Files = files;
		}

		public void Install()
		{
			Logger.MessageBag.Clear();
			InstallState = InstallState.Installing;
			CurrentFile = Files.FirstOrDefault(x => x.State.Equals(InstallState.Idle));

			new Thread(() =>
			{
				_processor?.Install(Files);
			}).Start();
		}

		public void Abort()
		{
			_processor?.Abort();

			InstallState = InstallState.Idle;
			Files = null;
			CurrentFile = null;
			Progress = 0;
		}

		public void Complete()
		{
			CurrentFile = null;
			Files = null;
			Progress = 0;
			InstallState = InstallState.Idle;
			Logger.MessageBag.Clear();
		}

		private void OnProgressChanged(ProgressChangedEventArgs e)
		{
			Progress = e.Percentage;
		}

		private void OnFileChanged(FileChangedEventArgs e)
		{
			CurrentFile = e.File;
		}

		private void OnFileStateChanged(FileStateChangedEventArgs e)
		{
			var file = Files.FirstOrDefault(x => x.Name == CurrentFile.Name);

			if (file != null)
			{
				file.State = e.State;
			}
		}

		private void OnInstallStateChanged(InstallStateChangedEventArgs e)
		{
			if(InstallState == InstallState.Cancelled && e.State == InstallState.Finished) return; 
			InstallState = e.State;
		}

		private void OnMessageReceived(MessageReceivedEventArgs e)
		{
			Logger.AddMessage(e.Type, e.Message);
		}
	}
}