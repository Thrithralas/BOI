using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using System.Security.Cryptography;
using System.IO;

namespace Blep.Backend
{
    public static class VoiceOfBees
    {
        public static bool FetchList()
        {
            using (var wc = new WebClient())
            {
                try
                {
                    ModEntryList.Clear();
                    Wood.WriteLine($"Fetching mod entries from AUDB... {DateTime.Now}");
                    string euv_json = wc.DownloadString("https://beestuff.pythonanywhere.com/audb/api/v2/enduservisible");
                    var jo = JArray.Parse(euv_json);
                    foreach (JToken entry in jo)
                    {
                        try
                        {
                            AUDBEntryRelay rel = entry.ToObject<AUDBEntryRelay>();
                            ModEntryList.Add(rel);
                        }
                        catch (JsonReaderException je)
                        {
                            Wood.WriteLine($"Error deserializing AUDB entry :");
                            Wood.WriteLine(je, 1);
                            Wood.WriteLine("Json text:");
                            Wood.WriteLine(entry, 1);
                        }
                    }
                    Wood.WriteLine("Entrylist fetched and parsed:");
                    Wood.Indent();
                    foreach (var entry in ModEntryList) { Wood.WriteLine(entry.name); }
                    Wood.Unindent();
                    return true;
                }
                catch (WebException we) { Wood.WriteLine("Error fetching AUDB entries:");  Wood.WriteLine(we.Response, 1); }
                catch (JsonException jse) { Wood.WriteLine("Error deserializing AUDB entry list"); Wood.WriteLine(jse.Message, 1); }
                return false;
            }
        }
        public static List<AUDBEntryRelay> ModEntryList { get { _el = _el ?? new List<AUDBEntryRelay>(); return _el; } set { _el = value; } }
        private static List<AUDBEntryRelay> _el;


        public class AUDBEntryRelay : IEquatable<AUDBEntryRelay>
        {
            public List<AUDBEntryRelay> deps { get { _deps = _deps ?? new List<AUDBEntryRelay>(); return _deps; } set { _deps = value; } }
            private List<AUDBEntryRelay> _deps;
            public KEY key;
            public string name;
            public string author;
            public string description;
            public string download;
            public string sig;
            public List<string> relativePath;
            public string fileExtension = ".dll";

            public class KEY
            {
                public string e;
                public string n;
                public KEYSRCDATA sig;
            }
            public class KEYSRCDATA
            {
                public KEY by;
                public string data;
            }

            public override string ToString()
            {
                return $"{name}";
            }

            public bool TryDownload(string TargetDirectory)
            {
                using (var dwc = new WebClient())
                {
                    try
                    {
                        var mcts = dwc.DownloadData(download);
                        var sha = new SHA512Managed();
                        var modhash = sha.ComputeHash(mcts);
                        var sigbytes = Convert.FromBase64String(sig);
                        var keyData = new RSAParameters();
                        keyData.Exponent = Convert.FromBase64String(key.e);
                        keyData.Modulus = Convert.FromBase64String(key.n);
                        var rsa = RSA.Create();
                        rsa.ImportParameters(keyData);
                        var def = new RSAPKCS1SignatureDeformatter(rsa);
                        def.SetHashAlgorithm("SHA512");
                        bool verPart1 = def.VerifySignature(modhash, sigbytes);
                        bool verPart2 = true;
                        if (key.sig != null)
                        {
                            keyData.Exponent = Convert.FromBase64String(key.sig.by.e);
                            keyData.Modulus = Convert.FromBase64String(key.sig.by.n);
                            rsa.ImportParameters(keyData);
                            def = new RSAPKCS1SignatureDeformatter(rsa);
                            def.SetHashAlgorithm("SHA512");
                            var bee = Encoding.ASCII.GetBytes($"postkey:{key.e}-{key.n}");
                            modhash = sha.ComputeHash(bee);
                            verPart2 = def.VerifySignature(modhash, Convert.FromBase64String(key.sig.data));
                        }
                        if (verPart1 && verPart2)
                        {
                            Wood.WriteLine($"Mod sig verified: {this.name}, saving");
                            try
                            {
                                var tfi = new DirectoryInfo(TargetDirectory);
                                if (!tfi.Exists) { tfi.Create(); tfi.Refresh(); }
                                File.WriteAllBytes(Path.Combine(TargetDirectory, $"{this.name}.{this.fileExtension}"), mcts);
                                if (deps.Count > 0)
                                {
                                    Wood.WriteLine("");
                                    foreach (var dep in deps)
                                    {
                                        if (dep.TryDownload(TargetDirectory)) { }
                                    }
                                }
                            }
                            catch (IOException ioe) 
                            { Wood.WriteLine($"Can not write the downloaded mod {this.name}:"); Wood.WriteLine(ioe, 1); return false; }
                            return true;
                        }
                        else
                        {
                            Wood.WriteLine($"Mod sig incorrect: {this.name}, download aborted");
                            return false;
                        }
                    }
                    catch (WebException we)
                    {
                        Wood.WriteLine($"Error downloading data from AUDB entry {name}:");
                        Wood.WriteLine(we, 1);
                        
                    }
                    finally
                    {
                        
                    }
                }
                return false;
            }

            public bool Equals(AUDBEntryRelay other)
            {
                return (this.download == other.download);
            }
        }
    }
}
