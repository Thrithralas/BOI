using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Security;


namespace AntiBlep
{
    class AntiBlep
    {
        const string rAddress = "https://api.github.com/repos/Rain-World-Modding/BOI/releases/latest";
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = true;
            using (var ht = new HttpClient())
            {
                ht.DefaultRequestHeaders.Clear();
                ht.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                ht.DefaultRequestHeaders.Add("User-Agent", "Rain-World-Modding/BOI");
                var rt = ht.GetAsync(rAddress);
                Task.WaitAll(rt);
                Console.WriteLine(rt.Result.StatusCode);
                var textask = rt.Result.Content.ReadAsStringAsync(); textask.Wait();
                Console.WriteLine(textask.Result);
            }
            Console.ReadKey();    
        }
    }
}
