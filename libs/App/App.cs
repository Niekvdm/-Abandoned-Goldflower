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

		private List<FileContainer> _files = null;
		private InstallState _installState { get; set; } = InstallState.Idle;
		private FileContainer _currentFile { get; set; }
		private int _progress { get; set; }
		private ProcessorType _processorType { get; set; }
		private IProcessor _processor = null;

		public App()
		{
			Logger = new Logger.Logger();
		}

		public void SetProcessorByType(ProcessorType type)
		{
			_processorType = type;

			switch (type)
			{
				case ProcessorType.Tinfoil:
					_processor = new Tinfoil.Tinfoil();
					break;
					
				case ProcessorType.Goldleaf:
					_processor = new Goldtree.Goldtree();
					break;
			}

			_processor.OnProgressChanged += this.OnProgressChanged;
			_processor.OnFileChanged += this.OnFileChanged;
			_processor.OnFileStateChanged += this.OnFileStateChanged;
			_processor.OnInstallStateChanged += this.OnInstallStateChanged;
			_processor.OnMessageReceived += this.OnMessageReceived;
		}

		public void SetLogLevel(int logLevel)
		{
			Logger.SetLogLevel(logLevel);
		}

		public void SetFiles(List<FileContainer> files)
		{
			_files = files;
		}

		public void Install()
		{
			Logger.MessageBag.Clear();
			_installState = InstallState.Installing;
			_currentFile = _files.FirstOrDefault(x => x.State.Equals(InstallState.Idle));

			new Thread(() =>
			{
				_processor?.Install(_files);
			}).Start();
		}

		public void Abort()
		{
			_processor?.Abort();

			_installState = InstallState.Idle;
			_files = null;
			_currentFile = null;
			_progress = 0;
		}

		public void Complete()
		{
			Logger.MessageBag.Clear();
			_currentFile = null;
			_files = null;
			_progress = 0;
			_installState = InstallState.Idle;
		}

		public AppState GetAppState()
		{
			return new AppState()
			{
				Status = _installState,
				Progress = _progress,
				ProcessorType = _processorType,
				CurrentFile = _currentFile,
				Files = _files,
				Events = Logger.MessageBag
			};
		}

		private void OnProgressChanged(ProgressChangedEventArgs e)
		{
			_progress = e.Percentage;
		}

		private void OnFileChanged(FileChangedEventArgs e)
		{
			if (_currentFile != e.File)
			{
				_currentFile = e.File;
			}
		}

		private void OnFileStateChanged(FileStateChangedEventArgs e)
		{
			var file = _files.FirstOrDefault(x => x.Name == _currentFile.Name);

			if (file != null)
			{
				file.State = e.State;
			}
		}

		private void OnInstallStateChanged(InstallStateChangedEventArgs e)
		{
			if (_installState == InstallState.Cancelled && e.State == InstallState.Finished) return;
			_installState = e.State;

			if (!string.IsNullOrEmpty(e.Message))
			{
				Logger.AddMessage(MessageType.Error, e.Message);
			}
		}

		private void OnMessageReceived(MessageReceivedEventArgs e)
		{
			Logger.AddMessage(e.Type, e.Message);
		}
	}
}