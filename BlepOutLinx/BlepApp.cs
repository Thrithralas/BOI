using System;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Blep
{
    internal static class BlepApp
    {
        public static StreamWriter ConsoleCrutch;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //if (foreach )
            var argl = args?.ToList() ?? new List<string>(); ;
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "showConsole.txt")) || argl.Contains("-nc") || argl.Contains("--new-console"))
            {
                Backend.BoiCustom.AllocConsole();
                Console.WriteLine("Launching BOI with output to a new console window.");
                Console.WriteLine("Reminder: you can always select text in console and then copy it by pressing enter. It also pauses the app.\n");
            }
            else if (argl.Contains("-ac") || argl.Contains("--attach-console"))
            {
                Backend.BoiCustom.AttachConsole(-1);
                Console.WriteLine("\nLaunching BOI and attempting to attach parent process console.");
            }
            if (argl.Contains("-fu") || argl.Contains("--force-update")) forceUpdate = true;
            BlepOut Currblep = new BlepOut();
            Application.Run(Currblep);
            if (!argl.Contains("-nu")) TrySelfUpdate();
            Backend.Wood.Lifetime = 0;
        }

        public const string REPOADDRESS = "https://api.github.com/repos/Rain-World-Modding/BOI/releases/latest";
        static bool forceUpdate;

        private static async void TrySelfUpdate()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = true;
            try
            {
                var dumpFolder = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "downloadDump"));
                if (!dumpFolder.Exists) dumpFolder.Create(); foreach (var f in dumpFolder.GetFiles()) f.Delete();
                using (var ht = new HttpClient())
                {
                    ht.DefaultRequestHeaders.Clear();
                    ht.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                    ht.DefaultRequestHeaders.Add("User-Agent", "Rain-World-Modding/BOI");
                    //ht.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                    //ht.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Rain-World-Modding_BOI", BlepOut.VersionNumber));
                    var reqTask = ht.GetAsync(REPOADDRESS);
                    var responsejson = Newtonsoft.Json.Linq.JObject.Parse(await reqTask.Result.Content.ReadAsStringAsync());
#warning add better diff detection
                    if ((string)responsejson["release_tag"] == BlepOut.VersionNumber && !forceUpdate) return;
                    ht.DefaultRequestHeaders.Clear();
                    ht.DefaultRequestHeaders.Add("User-Agent", "Rain-World-Modding/BOI");
                    ht.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
                    foreach (var asset in responsejson["assets"] as Newtonsoft.Json.Linq.JArray)
                    {
                        Backend.Wood.WriteLine(asset.ToString(Newtonsoft.Json.Formatting.Indented));
                        using (var wc = new WebClient()) wc.DownloadFile((string)asset["browser_download_url"], Path.Combine(dumpFolder.FullName, (string)asset["name"]));
                    }
                }
                foreach (var f in dumpFolder.GetFiles())
                {
                    if (f.Extension == ".zip") ZipFile.ExtractToDirectory(f.FullName, dumpFolder.FullName);
                }
                var xcs = new System.Diagnostics.ProcessStartInfo("cmd.exe");
                xcs.Arguments = $"/c xcopy /Y {dumpFolder.Name} \"{Directory.GetCurrentDirectory()}\"";
                System.Diagnostics.Process.Start(xcs);
            }
            catch (Exception e)
            {
                Backend.Wood.WriteLine("Unhandled exception while attempting self-update:");
                Backend.Wood.WriteLine(e, 1);
            }
            
        }
    }
}
