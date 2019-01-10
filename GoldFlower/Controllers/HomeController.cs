using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GoldFlower.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using App.Data.Enums;
using App.Data.Models;
using App.Data;
using GoldFlower;

namespace GoldFlower.Controllers
{
	public class HomeController : Controller
	{
		private readonly IWritableOptions<ApplicationSettings> _options;

		public HomeController(IWritableOptions<ApplicationSettings> options)
		{
			_options = options;
		}

		public IActionResult Index()
		{
			ViewBag.Path = _options.Value.Path;

			return View();
		}

		[HttpPost]
		[Route("/installer/select-directory")]
		public IActionResult SelectDirectory([FromBody] DirectoryContainer directoryContainer)
		{
			var result = new List<FileContainer>();

			try
			{
				var directory = Directory.GetFiles(directoryContainer?.Path, "*.nsp", SearchOption.AllDirectories);

				foreach (var filePath in directory.OrderBy(x => x).ToList())
				{
					var fileInfo = new FileInfo(filePath);
					result.Add(new FileContainer()
					{
						Name = fileInfo.Name,
						FullName = fileInfo.FullName,
						Size = fileInfo.Length,
						Path = fileInfo.DirectoryName,
						Extension = fileInfo.Extension
					});
				}

				_options.Update(options =>
				{
					options.Path = directoryContainer?.Path;
				});
			}
			catch (Exception ex)
			{
				return new JsonResult(new { Error = ex.Message });
			}

			return new JsonResult(new { Result = result });
		}

		[HttpPost]
		[Route("/installer/install")]
		public IActionResult Install([FromBody] List<FileContainer> files)
		{
			App.Instance.SetProcessor(new Goldtree.Goldtree());
			App.Instance.SetFiles(files);
			App.Instance.Install();

			return new JsonResult(new { Status = App.Instance.InstallState, Progress = App.Instance.Progress, CurrentFile = App.Instance.CurrentFile, Files = App.Instance.Files, Events = App.Instance.Logger.MessageBag });
		}

		[HttpPost]
		[Route("/installer/abort")]
		public IActionResult AbortInstaller()
		{
			App.Instance.Abort();

			return new JsonResult(new { Status = InstallState.Aborted, Progress = App.Instance.Progress, CurrentFile = App.Instance.CurrentFile, Files = App.Instance.Files, Events = App.Instance.Logger.MessageBag });
		}

		[HttpPost]
		[Route("/installer/complete")]
		public IActionResult CompleteInstaller()
		{
			//App.Instance.Complete();

			return new JsonResult(new { Status = App.Instance.InstallState, Progress = App.Instance.Progress, CurrentFile = App.Instance.CurrentFile, Files = App.Instance.Files, Events = App.Instance.Logger.MessageBag });
		}

		[HttpGet]
		[Route("/installer/progress")]
		public IActionResult Progress()
		{
			return new JsonResult(new { Status = App.Instance.InstallState, Progress = App.Instance.Progress, CurrentFile = App.Instance.CurrentFile, Files = App.Instance.Files, Events = App.Instance.Logger.MessageBag });
		}

		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
