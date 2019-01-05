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
using Goldtree.Enums;
using Goldtree.Models;
using Goldtree;

namespace GoldFlower.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Path = System.IO.File.Exists($"{Environment.CurrentDirectory}/config.txt") ? System.IO.File.ReadAllText($"{Environment.CurrentDirectory}/config.txt") : "";

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

                System.IO.File.WriteAllText($"{Environment.CurrentDirectory}/config.txt", directoryContainer?.Path);
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
            ApplicationState.Install(files);

            return new JsonResult(new { Status = ApplicationState.InstallState, Progress = ApplicationState.Progress, CurrentFile = ApplicationState.CurrentFile, Files = ApplicationState.Files, Events = ApplicationState.MessageBag });
        }

        [HttpPost]
        [Route("/installer/abort")]
        public IActionResult AbortInstaller()
        {
            ApplicationState.Abort();

            return new JsonResult(new { Status = InstallState.Aborted, Progress = ApplicationState.Progress, CurrentFile = ApplicationState.CurrentFile, Files = ApplicationState.Files, Events = ApplicationState.MessageBag });
        }

        [HttpPost]
        [Route("/installer/complete")]
        public IActionResult CompleteInstaller()
        {
            ApplicationState.Complete();

            return new JsonResult(new { Status = InstallState.Idle, Progress = ApplicationState.Progress, CurrentFile = ApplicationState.CurrentFile, Files = ApplicationState.Files, Events = ApplicationState.MessageBag });
        }

        [HttpGet]
        [Route("/installer/progress")]
        public IActionResult Progress()
        {
            return new JsonResult(new { Status = ApplicationState.InstallState, Progress = ApplicationState.Progress, CurrentFile = ApplicationState.CurrentFile, Files = ApplicationState.Files, Events = ApplicationState.MessageBag });
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
