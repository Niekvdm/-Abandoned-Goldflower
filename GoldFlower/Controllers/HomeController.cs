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
		[Route("/installer/install/{type}")]
		public IActionResult Install([FromBody] List<FileContainer> files, ProcessorType type)
		{
			App.Instance.SetProcessorByType(type);
			App.Instance.SetFiles(files);
			App.Instance.SetLogLevel(_options?.Value?.LogLevel ?? 3);
			App.Instance.Install();

			return new JsonResult(App.Instance.GetAppState());
		}

		[HttpPost]
		[Route("/installer/abort")]
		public IActionResult AbortInstaller()
		{
			App.Instance.Abort();

			var state = App.Instance.GetAppState();
			state.Status = InstallState.Aborted;

			return new JsonResult(state);
		}

		[HttpPost]
		[Route("/installer/complete")]
		public IActionResult CompleteInstaller()
		{
			App.Instance.Complete();

			return new JsonResult(App.Instance.GetAppState());
		}

		[HttpGet]
		[Route("/installer/progress")]
		public IActionResult Progress()
		{
			return new JsonResult(App.Instance.GetAppState());
		}

		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
