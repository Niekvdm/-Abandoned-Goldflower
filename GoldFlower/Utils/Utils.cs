using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GoldFlower
{
    internal class Utils
    {
        /// <summary>
        /// Attempt to launch a browser (IE, Chrome, Firefox, etc...) with the given url.
        /// </summary>
        /// <param name="url">The url to open</param>
        public static void LaunchBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}")); // Works ok on windows
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);  // Works ok on linux
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url); // Not tested
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}