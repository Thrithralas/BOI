using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mono.Cecil;
using System.Runtime.InteropServices;


namespace Blep.Backend
{
    public static class Donkey
    {
        /// <summary>
        /// Clears modlist, attempts to load it again from a specified directory
        /// </summary>
        /// <param name="target">Target directory</param>
        /// <returns>Number of errors encountered during the operation</returns>
        public static int TryLoadCargo(DirectoryInfo target)
        {
            currentSourceDir = target;
            if (!target.Exists) return -1;
            cargo.Clear();
            var errcount = 0;
            foreach (var file in target.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                if (file.Extension != ".dll") continue;
                try
                {
                    var mr = new ModRelay(file);
                    cargo.Add(mr);
                }
                catch (Exception e) { errcount++; Wood.WriteLine("Error checking mod entry:"); Wood.WriteLine(e); }
            }
            return errcount;
        }
        /// <summary>
        /// modlist
        /// </summary>
        public static List<ModRelay> cargo = new List<ModRelay>();
        public static DirectoryInfo currentSourceDir;
        public static bool FullyFunctional => ((bool)pluginsTargetPath?.Exists && (bool)mmpTargetPath?.Exists && (bool)currentSourceDir?.Exists && (bool)bepPatcherTargetPath?.Exists);

        public static void SetPluginsTarget(string path) { SetPluginsTarget(new DirectoryInfo(path)); }
        public static void SetPluginsTarget (DirectoryInfo target)
        {
            pluginsTargetPath = target;
            pluginsBlacklist.Clear();
            if (!target.Exists) return;
            var blp = target.GetFiles("plugins_blacklist.txt", SearchOption.TopDirectoryOnly);
            try
            {
                if (blp.Length > 0) { pluginsBlacklist.AddRange(File.ReadAllLines(blp[0].FullName)); }
                else { var sampleBl = new[] { "LogFix.dll" }; pluginsBlacklist.AddRange(sampleBl); File.WriteAllLines(Path.Combine(target.FullName, "plugins_blacklist.txt"), sampleBl); }
            }
            catch (Exception e)
            {
                Wood.WriteLine("Error while setting plugins tar folder:");
                Wood.WriteLine(e);
            }
        }
        public static DirectoryInfo pluginsTargetPath;
        public static List<string> pluginsBlacklist = new List<string>();

        public static void SetMmpTarget(string path) { SetMmpTarget(new DirectoryInfo(path)); }
        public static void SetMmpTarget(DirectoryInfo target) 
        {
            mmpTargetPath = target;
            mmpBlacklist.Clear();
            if (!target.Exists) return;
            var blp = target.GetFiles("patches_blacklist.txt", SearchOption.TopDirectoryOnly);
            try
            {
                if (blp.Length > 0) { pluginsBlacklist.AddRange(File.ReadAllLines(blp[0].FullName)); }
                else { var sampleBl = new[] { "Assembly-CSharp.PatchNothing.mm.dll" }; pluginsBlacklist.AddRange(sampleBl); File.WriteAllLines(Path.Combine(target.FullName, "patches_blacklist.txt"), sampleBl); }
            }
            catch (Exception e)
            {
                Wood.WriteLine("Error while setting mm patches tar folder:");
                Wood.WriteLine(e);
            }
        }
        public static DirectoryInfo mmpTargetPath;
        public static List<string> mmpBlacklist = new List<string>();

        public static void SetBepPatcherTarget(string path) { SetBepPatcherTarget(new DirectoryInfo(path)); }
        public static void SetBepPatcherTarget(DirectoryInfo target)
        {
            bepPatcherTargetPath = target;
            bepPatcherBlacklist.Clear();
            if (!target.Exists) return;
            var blp = target.GetFiles("patchers_blacklist.txt", SearchOption.TopDirectoryOnly);
            try
            {
                if (blp.Length > 0) { pluginsBlacklist.AddRange(File.ReadAllLines(blp[0].FullName)); }
                else {
                    var sampleBl = new[] { "BepInEx.MonoMod.Loader.dll", "Dragons.dll", "Dragons.HookGenCompatibility.dll", "Dragons.PublicDragon.dll" };
                    bepPatcherBlacklist.AddRange(sampleBl);
                    File.WriteAllLines(Path.Combine(target.FullName, "patchers_blacklist.txt"), sampleBl); }
            }
            catch (Exception e)
            {
                Wood.WriteLine("Error while setting plugins tar folder:");
                Wood.WriteLine(e);
            }
        }
        public static DirectoryInfo bepPatcherTargetPath;
        public static List<string> bepPatcherBlacklist = new List<string>();

        /// <summary>
        /// Enables a mod under selected index in <see cref="cargo"/>
        /// </summary>
        /// <param name="tIndex"></param>
        /// <returns>true if successful, false otherwise</returns>
        public static bool TryDeliver(int tIndex)
        {
            if (tIndex < 0 || tIndex >= cargo.Count) return false;
            Wood.WriteLine($"Donkey: delivering {cargo[tIndex]}...");
            try
            {
                cargo[tIndex].Enable();
                Wood.WriteLine($"Donkey: {cargo[tIndex]} delivered.");
                return true;
            }
            catch (Exception e)
            { 
                Wood.WriteLine($"Error while delivering {cargo[tIndex]}:"); 
                Wood.WriteLine(e);
                return false; 
            }
        }
        /// <summary>
        /// Enables mods by a range of indices
        /// </summary>
        /// <param name="range"></param>
        /// <returns>number of failed operations</returns>
        public static int TryDeliverRange(IEnumerable<int> range)
        {
            var errcount = 0;
            foreach (var tIndex in range)
            {
                if (!TryDeliver(tIndex)) errcount++;
            }
            return errcount;
        }
        /// <summary>
        /// Disables a mod under selected index in <see cref="cargo"/>
        /// </summary>
        /// <param name="tIndex"></param>
        /// <returns>true if successful, false otherwise</returns>
        public static bool TryRetract(int tIndex)
        {
            if (tIndex < 0 || tIndex >= cargo.Count) return false;
            Wood.WriteLine($"Donkey: retracting {cargo[tIndex]}...");
            try
            {
                cargo[tIndex].Disable();
                Wood.WriteLine($"Donkey: retracted {cargo[tIndex]}");
                return true;
            }
            catch (Exception e)
            {
                Wood.WriteLine($"Error while retracting {cargo[tIndex]}:");
                Wood.WriteLine(e);
                return false;
            }
        }
        /// <summary>
        /// Disables mods by a range of indices
        /// </summary>
        /// <param name="range"></param>
        /// <returns>number of failed operations</returns>
        public static int TryretractRange (IEnumerable<int> range)
        {
            var errcount = 0;
            foreach (var tIndex in range)
            {
                if (!TryRetract(tIndex)) errcount++;
            }
            return errcount;
        }

        /// <summary>
        /// Deletes files from active folders that are extremely likely to crash the game instantly and absolutely should not be there
        /// </summary>
        /// <returns>Number of errors encountered during the operation</returns>
        public static int CriticalSweep()
        {
            if (!FullyFunctional) return 0;
            int errctr = 0;
            Wood.WriteLine("Starting critsweep");
            Wood.Indent();
            var toCheck = new List<FileInfo>();
            toCheck.AddRange(pluginsTargetPath.GetFiles("*.dll", SearchOption.TopDirectoryOnly));
            toCheck.AddRange(bepPatcherTargetPath.GetFiles("*.dll", SearchOption.TopDirectoryOnly));
            toCheck.AddRange(mmpTargetPath.GetFiles("*.dll", SearchOption.TopDirectoryOnly));
            var ProhibitedTypes = new Dictionary<string, List<ModRelay.EUModType>>
            {
                {   pluginsTargetPath.FullName, 
                    new List<ModRelay.EUModType> { ModRelay.EUModType.Invalid} },
                {   mmpTargetPath.FullName, 
                    new List<ModRelay.EUModType> 
                        {ModRelay.EUModType.BepPlugin, 
                        ModRelay.EUModType.Partmod, 
                        ModRelay.EUModType.BepPatcher} },
                {   bepPatcherTargetPath.FullName, 
                    new List<ModRelay.EUModType> 
                        {ModRelay.EUModType.Invalid, 
                        ModRelay.EUModType.Partmod, 
                        ModRelay.EUModType.BepPlugin,
                        ModRelay.EUModType.mmPatch} }
            };
            foreach (var fin in toCheck)
            {
                if (pluginsBlacklist.Contains(fin.Name) || bepPatcherBlacklist.Contains(fin.Name) || mmpBlacklist.Contains(fin.Name)) continue;
                try
                {
                    var mt = ModRelay.GetModType(ModuleDefinition.ReadModule(fin.FullName));
                    if (ProhibitedTypes[fin.DirectoryName].Contains(mt)) 
                    {
                        Wood.WriteLine($"Removing {fin.Name} of type {mt} from {fin.DirectoryName}");
                        fin.Delete();
                    }
                }
                catch (Exception e)
                {
                    Wood.WriteLine($"error encountered during critsweep; current file: {fin.Name}");
                    Wood.WriteLine(e);
                    errctr++;
                }
            }
            Wood.Unindent();
            Wood.WriteLine("Critsweep over.");
            return errctr;
        }
        /// <summary>
        /// Checks if the file under a given path is Pubstunt.
        /// </summary>
        /// <param name="path">Path to be checked.</param>
        /// <returns></returns>
        public static bool AintThisPS(string path)
        {
            var fi = new FileInfo(path);
            if (fi.Extension != ".dll" || fi.Attributes.HasFlag(FileAttributes.ReparsePoint)) return false;
            try
            {
                using (ModuleDefinition md = ModuleDefinition.ReadModule(path))
                {
                    return (md.Assembly.FullName.Contains("PublicityStunt"));
                }
            }
            catch (IOException ioe)
            {
                Wood.WriteLine("ATPS: ERROR CHECKING MOD ASSEMBLY :");
                Wood.Indent();
                Wood.WriteLine(ioe);
                Wood.Unindent();
                Wood.WriteLine("Well, it's probably not PS.");
                return false;
            }

        }
    }
}
