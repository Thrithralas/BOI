using Mono.Cecil;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Security.Cryptography;


namespace Blep.Backend
{
    //
    //  CODE MODS
    //

    /// <summary>
    /// Unified relay class for <see cref="ModData"/> and its children.
    /// </summary>
    public class ModRelay
    {
        public ModRelay(string path)
        {
            ModPath = path;
            isValid = !ModData.AbsolutelyIgnore(ModPath);
            if (isValid)
            {
                if (BlepOut.AintThisPS(path))
                {
                    AssociatedModData = new InvalidModData(path);
                    MyType = EUModType.Invalid;
                    return;
                }
                EUModType mt = GetModType(ModPath);
                switch (mt)
                {
                    case EUModType.Unknown:
                        AssociatedModData = new ModData(path);
                        MyType = EUModType.Unknown;
                        break;
                    case EUModType.Patch:
                        AssociatedModData = new PtModData(path);
                        MyType = EUModType.Patch;
                        break;
                    case EUModType.Partmod:
                        AssociatedModData = new HkModData(path);
                        MyType = EUModType.Partmod;
                        break;
                    case EUModType.BepPlugin:
                        AssociatedModData = new BepPluginData(path);
                        MyType = EUModType.BepPlugin;
                        break;
                    case EUModType.Invalid:
                        AssociatedModData = new InvalidModData(path);
                        MyType = EUModType.Invalid;
                        break;
                }
            }
        }


        public static EUModType GetModType(ModuleDefinition md)
        {
            var tstate = default(ModTypeFlags);
            foreach (TypeDefinition t in md.Types)
            {
                CheckThisType(t, ref tstate);
            }
#warning redo and finish
            if (tstate.HasFlag(ModTypeFlags.MMpatch))
            {
                if (tstate != ModTypeFlags.MMpatch) return EUModType.Invalid;
                else return EUModType.Patch;
            }
            if (tstate.HasFlag(ModTypeFlags.BepPatcher))
            {
                if (tstate != ModTypeFlags.BepPatcher) return EUModType.Invalid;
                else return EUModType.BepPatcher;
            }
            if (tstate.HasFlag(ModTypeFlags.PartMod) ^ tstate.HasFlag(ModTypeFlags.BepPlugin))
            {
                return tstate.HasFlag(ModTypeFlags.PartMod) ? EUModType.Partmod : EUModType.BepPlugin; 
            }
            return EUModType.Unknown;
        }

        public static EUModType GetModType(string path)
        {
            
            try
            {
                using (ModuleDefinition md = ModuleDefinition.ReadModule(path))
                {

                    return GetModType(md);

                }
            }
            catch (IOException ioe)
            {
                Wood.WriteLine("ERROR CHECKING ASSEMBLY TYPE: IOException occured");
                Wood.Indent();
                Wood.WriteLine(ioe);
                Wood.Unindent();
                return EUModType.Unknown;
            }

        }

        public static void CheckThisType(TypeDefinition td, ref ModTypeFlags state)
        {
            
            if (td.BaseType != null && td.BaseType.Name == "PartialityMod") state |= ModTypeFlags.PartMod;
            var contract_M = false;
            var contract_P = false;
            foreach (var method in td.Methods)
            {
                if (method.IsStatic && method.Name == "Patch" && method.HasParameters && method.Parameters[0].Name == "assembly" && method.Parameters[0].ParameterType.Name == "AssemblyDefinition" ) { contract_M = true; break; }
            }
            foreach (var prop in td.Properties)
            {
                if (prop.Name == "TargetDLLs" && prop.GetMethod != null && prop.PropertyType.Name.Contains("IEnumerable")) { contract_P = true; break; }
            }
            if (contract_P & contract_M) state |= ModTypeFlags.BepPatcher;

            if (td.HasCustomAttributes)
            {
                foreach (CustomAttribute catr in td.CustomAttributes)
                {
                    if (catr.AttributeType.Name == "MonoModPatch") state |= ModTypeFlags.MMpatch;
                    if (catr.AttributeType.Namespace == "BepInEx") state |= ModTypeFlags.BepPlugin;
                }
            }
            if (td.HasNestedTypes)
            {
                foreach (TypeDefinition ntd in td.NestedTypes)
                {
                    ModTypeFlags nestate = 0;
                    CheckThisType(ntd, ref nestate);
                    state |= nestate;
                }
            }
        }

        public struct mttup
        {
            public mttup(bool hk, bool pt, bool beppl)
            {
                isbeppl = beppl;
                ishk = hk;
                ispt = pt;
            }
            public bool ishk;
            public bool ispt;
            public bool isbeppl;
        }

        [Flags]
        public enum ModTypeFlags
        {
            PartMod = 1,
            MMpatch = 2,
            BepPlugin = 4,
            BepPatcher = 8,
        }
        public enum EUModType
        {
            Patch,
            Partmod,
            Invalid,
            BepPlugin,
            BepPatcher,
            Unknown
        }
        public EUModType MyType;



        public byte[] origchecksum
        {
            get
            {
                using (FileStream fs = File.OpenRead(ModPath))
                {
                    SHA256 sha = new SHA256Managed();
                    return sha.ComputeHash(fs);
                }
            }
        }
        public byte[] TarCheckSum
        {
            get
            {
                if (AssociatedModData is InvalidModData)
                {
                    return origchecksum;
                }
                using (FileStream fs = File.OpenRead(TarPath))
                {
                    SHA256 sha = new SHA256Managed();
                    return sha.ComputeHash(fs);
                }
            }
        }
        public string TarPath
        {
            get
            {
                return (Path.Combine(AssociatedModData.TarFolder, AssociatedModData.TarName));
            }
        }

        public string ModPath { get; set; }
        public ModData AssociatedModData { get; set; }
        public bool isValid { get; set; }


        public bool enabled
        {
            get { return AssociatedModData.Enabled; }
        }
        public void Enable()
        {
            if (enabled) return;
            File.Copy(AssociatedModData.OrigPath, TarPath);
        }
        public void Disable()
        {
            if (!enabled) return;
            File.Delete(TarPath);
        }
        public override string ToString()
        {
            return AssociatedModData.DisplayedName + " : " + MyType.ToString().ToUpper();
        }
    }

    /// <summary>
    /// Base representationn for RW code mods.
    /// </summary>
    public class ModData
    {
        public ModData(string path)
        {
            OrigPath = path;
            DisplayedName = new FileInfo(path).Name;
        }
        public virtual string TarName
        {
            get { return DisplayedName; }
        }

        public virtual string TarFolder => Path.Combine(BlepOut.RootPath, "BepInEx", "plugins");
        public string OrigPath;

        public virtual bool Enabled => File.Exists(Path.Combine(TarFolder, TarName));
        public virtual string DisplayedName { get; set; }
        public static bool AbsolutelyIgnore(string tpath)
        {
            return (new FileInfo(tpath).Extension != @".dll" || new FileInfo(tpath).Attributes.HasFlag(FileAttributes.ReparsePoint));
        }
        public override string ToString()
        {
            return DisplayedName + " : UNKNOWN";
        }
    }

    /// <summary>
    /// Implementation of <see cref="ModData"/> for Partiality mods.
    /// </summary>
    public class HkModData : ModData
    {
        public HkModData(string path) : base(path)
        {

        }

        public override string ToString()
        {
            return DisplayedName + " : HOOK";
        }
    }

    /// <summary>
    /// Implementation of <see cref="ModData"/> for Monomod patches.
    /// </summary>
    public class PtModData : ModData
    {
        public PtModData(string path) : base(path)
        {

        }

        public override string TarName => "Assembly-CSharp." + DisplayedName.Replace(".dll", string.Empty) + ".mm.dll";

        public override string TarFolder => Path.Combine(BlepOut.RootPath, "BepInEx", "Monomod");

        public static string GiveMeBackMyName(string partname)
        {
            string sl = partname;
            if (sl.StartsWith("Assembly-CSharp.") && sl.EndsWith(".mm.dll"))
            {
                sl = sl.Replace("Assembly-CSharp.", string.Empty);
                sl = sl.Replace(".mm.dll", ".dll");
            }
            return sl;
        }

        //public override bool Enabled => File.Exists(this.TarFolder + this.TarName);

        public override string ToString()
        {
            return DisplayedName + " : PATCH";
        }
    }

    /// <summary>
    /// Implementation of <see cref="ModData"/> for Bepinex plugins.
    /// </summary>
    public class BepPluginData : ModData
    {
        public BepPluginData(string path) : base(path)
        {

        }

        public override string ToString()
        {
            return DisplayedName + " : PLUGIN";
        }
    }

    /// <summary>
    /// Implementation of <see cref="ModData"/> for invalid mods (mixing mm patches with elsewhat)
    /// </summary>
    public class InvalidModData : PtModData
    {
        public InvalidModData(string path) : base(path)
        {

        }

        public override string ToString()
        {
            return DisplayedName + ": INVALID";
        }

    }

    public class BepPatcherData : ModData
    {
        public BepPatcherData(string path) : base(path) { }
        public override string TarFolder => Path.Combine(BlepOut.RootPath, "BepInEx", "patchers");
    }

    //
    //  REGMODS
    //

    /// <summary>
    /// Represents a CRS regpack state.
    /// </summary>
    public class RegModData
    {
        public RegModData(string pth)
        {
            path = pth;
            hasBeenChanged = false;
            ReadRegInfo();
        }

        
        private JObject jo;
        private string path;
        public bool hasBeenChanged;
        public enum CfgState
        {
            RegInfo,
            PackInfo,
            None
        }
        //Config file type
        public CfgState CurrCfgState
        {
            get
            {
                if (File.Exists(Path.Combine(path, @"packInfo.json")))
                {
                    return CfgState.PackInfo;
                }
                else if (File.Exists(Path.Combine(path, @"regionInfo.json") ))
                {
                    return CfgState.RegInfo;
                }
                else return CfgState.None;
            }
        }
        public string pathToCfg
        {
            get
            {
                switch (CurrCfgState)
                {
                    case CfgState.PackInfo:
                        return Path.Combine(path, "packInfo.json");
                    case CfgState.RegInfo:
                        return Path.Combine(path + "regionInfo.json");
                    case CfgState.None:
                        return null;
                    default: return null;
                }
            }
        }
        public string regionName
        {
            get
            {
                if (jo == null || !jo.ContainsKey("regionName")) return new DirectoryInfo(path).Name;
                return (string)jo["regionName"];
            }
        }
        public string description
        {
            get
            {
                if (jo == null || !jo.ContainsKey("description")) return "Settings file could not have been loaded; description inaccessible.";
                return (string)jo["description"];
            }
        }
        public bool activated
        {
            get
            {
                if (jo == null || !jo.ContainsKey("activated")) return false;
                return (bool)jo["activated"];
            }

            set 
            {
                if (jo == null || !jo.ContainsKey("activated")) return;
                hasBeenChanged = true;
                jo["activated"] = value;
            }
        }
        public bool structureValid
        {
            get
            {
                return (Directory.Exists(Path.Combine(path, "World") ) || Directory.Exists(Path.Combine(path, "Levels")));
            }
        }
        public int? loadOrder
        {
            get
            {
                if (jo == null || !jo.ContainsKey("loadOrder")) return null;
                return (int)jo["loadOrder"];
            }
            set
            {
                if (jo == null || !jo.ContainsKey("loadOrder")) return;
                hasBeenChanged = true;
                jo["loadOrder"] = value;
            }
        }
        /// <summary>
        /// Reads regionInfo.json / packInfo.json
        /// </summary>
        public void ReadRegInfo()
        {
            if (pathToCfg != null)
            {

                try
                {
                    string jscts = File.ReadAllText(pathToCfg);
                    jo = JObject.Parse(jscts);
                }
                catch (JsonException ioe)
                {
                    Wood.WriteLine($"ERROR READING REGPACK CONFIG JSON FOR: {regionName}");
                    Wood.Indent();
                    Wood.WriteLine(ioe);
                    Wood.Unindent();
                }
                
            }
        }
        /// <summary>
        /// Writes regpack settings json
        /// </summary>
        public void WriteRegInfo()
        {
            if (jo == null)
            {
                Wood.WriteLine($"Region mod {regionName} does not have a config file; cannot apply any changes.");
                return;
            }
            Wood.WriteLine($"Writing changes to regpack config for: {regionName}, contents:");
            Wood.Indent();
            Wood.WriteLine(jo);
            Wood.Unindent();
            
            hasBeenChanged = false;
            File.WriteAllText(pathToCfg, jo.ToString());
        }
        public override string ToString()
        {
            return regionName;
        }
    }

    //
    // EDT CONFIG
    //

    /// <summary>
    /// it's bad
    /// </summary>
    public static class EDTCFGDATA
    {
        public static JObject jo;
        public static bool hasBeenChanged = false;
        public static string edtConfigPath => Path.Combine(BlepOut.RootPath, "edtSetup.json");
        public static bool edtConfigExists => File.Exists(edtConfigPath);
        public static void loadJo()
        {
            jo = null;
            if (!edtConfigExists) return;
            try
            {
                string jsf = File.ReadAllText(edtConfigPath);
                jo = JObject.Parse(jsf);
            }
            catch (IOException ioe)
            {
                Wood.WriteLine("Error reading EDT config file:");
                Wood.Indent();
                Wood.WriteLine(ioe);
                Wood.Unindent();
            }
            catch (JsonReaderException jre)
            {
                Wood.WriteLine("Error parsing EDT config:");
                Wood.Indent();
                Wood.WriteLine(jre);
                Wood.Unindent();
            }
            hasBeenChanged = false;
            
        }
        public static void SaveJo()
        {
            if (!hasBeenChanged) return;
            try
            {
                File.WriteAllText(edtConfigPath, jo.ToString());
                Wood.WriteLine("Saving config. Contents:");
                Wood.Indent();
                Wood.WriteLine(jo.ToString());
                Wood.Unindent();
            }
            catch (IOException ioe)
            {
                Wood.WriteLine("Error writing EDT config file:");
                Wood.Indent();
                Wood.WriteLine(ioe);
                Wood.Unindent();
            }
            catch (System.ArgumentNullException)
            {
                Wood.WriteLine("JO is null; nothing to write.");
            }
        }
        public static string startmap
        {
            get
            {
                if (jo == null || !jo.ContainsKey("start_map")) return null;
                return (string)jo["start_map"];
            }
            set
            {
                if (jo == null) return;
                hasBeenChanged = true;
                jo["start_map"] = value;
            }
        }
        public static bool? skiptitle
        {
            get
            {
                if (jo == null || !jo.ContainsKey("skip_title")) return null;
                return (bool)jo["skip_title"];
            }
            set
            {
                if (jo == null) return;
                hasBeenChanged = true;
                jo["skip_title"] = value;
            }
        }
        public static int? forcechar
        {
            get
            {
                if (jo == null || !jo.ContainsKey("force_selected_character")) return null;
                return (int)jo["force_selected_character"];
            }
            set
            {
                if (jo == null) return;
                hasBeenChanged = true;
                jo["force_selected_character"] = value;
            }
        }
        public static bool? norain
        {
            get
            {
                if (jo == null || !jo.ContainsKey("no_rain")) return null;
                return (bool)jo["no_rain"];
            }
            set
            {
                if (jo == null) return;
                hasBeenChanged = true;
                jo["no_rain"] = value;
            }
        }
        public static bool? devtools
        {
            get
            {
                if (jo == null || !jo.ContainsKey("devtools")) return null;
                return (bool)jo["devtools"];
            }
            set
            {
                if (jo == null) return;
                hasBeenChanged = true;
                jo["devtools"] = value;
            }
        }
        public static int? cheatkarma
        {
            get
            {
                if (jo == null || !jo.ContainsKey("cheat_karma")) return null;
                return (int)jo["cheat_karma"];
            }
            set
            {
                if (jo == null) return;
                hasBeenChanged = true;
                jo["cheat_karma"] = value;
            }
        }
        public static bool? revealmap
        {
            get
            {
                if (jo == null || !jo.ContainsKey("reveal_map")) return null;
                return (bool)jo["reveal_map"];
            }
            set
            {
                if (jo == null) return;
                hasBeenChanged = true;
                jo["reveal_map"] = value;
            }
        }
        public static bool? forcelight
        {
            get
            {
                if (jo == null || !jo.ContainsKey("force_light")) return null;
                return (bool)jo["force_light"];
            }
            set
            {
                if (jo == null) return;
                hasBeenChanged = true;
                jo["force_light"] = value;
            }
        }
        public static bool? bake
        {
            get
            {
                if (jo == null || !jo.ContainsKey("bake")) return null;
                return (bool)jo["bake"];
            }
            set
            {
                if (jo == null) return;
                hasBeenChanged = true;
                jo["bake"] = value;
            }
        }
        public static bool? encrypt
        {
            get
            {
                if (jo == null || !jo.ContainsKey("encrypt")) return null;
                return (bool)jo["encrypt"];
            }
            set
            {
                if (jo == null) return;
                hasBeenChanged = true;
                jo["devtools"] = value;
            }
        }
    }
}
