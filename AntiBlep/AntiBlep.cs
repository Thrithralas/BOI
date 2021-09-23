using System;
using System.Net;
using Newtonsoft.Json;


namespace AntiBlep
{
    class AntiBlep
    {

        static void Main(string[] args)
        {
            using (var wc = new WebClient())
            {
                
                try
                {
                    string rdbjson = wc.DownloadString("https://beestuff.pythonanywhere.com/audb/api/v2/enduservisible");
                    
                    var ml = JsonConvert.DeserializeObject(rdbjson);
                    Console.WriteLine(JsonConvert.SerializeObject(ml, Formatting.Indented));
                    Console.WriteLine(ml.GetType());
                    Console.ReadKey();

                }
                catch (WebException we)
                {
                    Console.WriteLine(we);
                }
            }
                
        }
    }
}
