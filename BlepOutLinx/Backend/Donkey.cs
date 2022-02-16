using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mono.Cecil;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;

namespace Blep.Backend
{
    public static class Donkey
    {
        /// <summary>
        /// Clears modlist, attempts to load it again from a specified directory
        /// </summary>
        /// <param name="target">Target directory</param>
        /// <returns>Number of errors encountered during the operation</returns>
        [Obsolete]
        public static int TryLoadCargo(DirectoryInfo target)
        {
            var start = DateTime.UtcNow;
            currentSourceDir = target;
            cargo.Clear();
            if (!target.Exists) return -1;
            var errcount = 0;
            foreach (var file in target.GetFiles("*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var mr = new ModRelay(file);
                    if (mr.AssociatedModData == null) throw new ArgumentNullException("NULL MOD DATA! something went wrong in ModRelay ctor");
                    cargo.Add(mr);
                }
                catch (Exception e) { errcount++; Wood.WriteLine("Error checking mod entry:"); Wood.WriteLine(e); }
            }
            Wood.WriteLine($"Sync loading complete. Time elapsed: {DateTime.UtcNow - start}");
            return errcount;
        }
        public static int TryLoadCargoAsync(DirectoryInfo target)
        {
            Wood.WriteLine($"Attempting to load cargo from {target}.");
            currentSourceDir = target;
            cargo.Clear();
            if (!target.Exists) return -1;
            Wood.WriteLine("Path valid. ");
            var start = DateTime.UtcNow;
            var tasklist = new List<Task<ModRelay>>();
            foreach (var file in target.GetFiles("*.dll", SearchOption.TopDirectoryOnly))
            {
                var nt = new Task<ModRelay>(() => (ModRelay)Activator.CreateInstance(typeof(ModRelay), file));
                nt.Start();
                tasklist.Add(nt);
            }
            Task.WaitAll(tasklist.ToArray());
            var errc = 0;
            foreach (var t in tasklist)
            {
                if (t.Exception != null)
                {
                    Wood.WriteLine($"Unhandled exception during creation of a ModRelay: {t.Exception}");
                    errc++;
                    continue;
                }
                if (t.Result.AssociatedModData == null)
                {
                    Wood.WriteLine($"Empty mod data: something went wrong in ModRelay ctor for {t.Result.AssociatedModData.OrigLocation}");
                    errc++;
                    continue;
                }
                cargo.Add((ModRelay)t.Result);
            }
            Wood.WriteLine($"Loading complete. Time elapsed {DateTime.UtcNow - start}");
            return errc;
        }


        /// <summary>
        /// modlist
        /// </summary>
        public static List<ModRelay> cargo = new();
        public static DirectoryInfo currentSourceDir;
        public static bool FullyFunctional => (pluginsTargetPath?.Exists ?? false) && (currentSourceDir?.Exists ?? false) && (bepPatcherTargetPath?.Exists ?? false);

        #region targets and blacklists
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
        public static List<string> pluginsBlacklist = new();

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
        public static List<string> mmpBlacklist = new();

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
                    var sampleBl = new[] { "BepInEx.MonoMod.Loader.dll", "Dragons.dll", "Dragons.HookGenCompatibility.dll", "Dragons.PublicDragon.dll", "Dragons.Core.dll" };
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
        public static List<string> bepPatcherBlacklist = new();

#warning revise blacklist system, bringback ignores it atm
#warning blacklist templates to ER?
        #endregion

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
        /// Async version of <see cref="TryDeliver(int)"/>
        /// </summary>
        /// <param name="tIndex"></param>
        public static void DeliverAsync(int tIndex)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(x => { TryDeliver(tIndex); }));
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
        /// Async version of <see cref="TryDeliverRange(IEnumerable{int})"/>
        /// </summary>
        /// <param name="range"></param>
        public static void DeliverRangeAsync(IEnumerable<int> range)
        {
            foreach (var tIndex in range)
            {
                DeliverAsync(tIndex);
            }
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
        /// Async version of <see cref="TryRetract(int)"/>
        /// </summary>
        /// <param name="tIndex"></param>
        public static void RetractAsync(int tIndex)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(x => { TryRetract(tIndex); }));
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
        /// Async version of <see cref="TryretractRange(IEnumerable{int})"/>
        /// </summary>
        /// <param name="range"></param>
        public static void RetractRangeAsync(IEnumerable<int> range)
        {
            foreach (var tIndex in range)
            {
                RetractAsync(tIndex);
            }
        }

        //public static List<Task<bool>> movetasks = new List<Task<bool>>();
        public static int RetrieveLost()
        {
            int errc = 0;
            foreach (var afld in new[] { pluginsTargetPath, bepPatcherTargetPath, mmpTargetPath })
            {
                if (afld == null) continue;
                var blfn = new FileInfo(Path.Combine(afld.FullName, afld.Name + "_blacklist.txt"));
                string[] bl = blfn.Exists ? File.ReadAllLines(blfn.FullName) : new string[] { };
                
                foreach (var mf in afld.GetFiles("*.dll", SearchOption.TopDirectoryOnly))
                {
                    if (bl.Contains(mf.Name)) continue;
                    var expectedSource = Path.Combine(currentSourceDir.FullName, 
                        afld == mmpTargetPath ? mmPatchData.GiveMeBackMyName(mf.Name) : mf.Name); // why do mmpatches even need these stupid ass names wtf
                    if (!File.Exists(expectedSource)) mf.CopyTo(expectedSource);
                }
            }
            return errc;
        }

        /// <summary>
        /// Deletes files from active folders that are extremely likely to crash the game instantly and absolutely should not be there
        /// </summary>
        /// <returns>Number of errors encountered during the operation</returns>
        public static int CriticalSweep()
        {
            if (!FullyFunctional) return -1;
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
                    Wood.WriteLine($"Error encountered during critsweep; current file: {fin.Name}");
                    Wood.WriteLine(e);
                    errctr++;
                }
            }
            Wood.Unindent();
            Wood.WriteLine("Critsweep over.");
            return errctr;
        }
        /// <summary>
        /// Brings all mods up to date if needed.
        /// </summary>
        /// <returns>Number of errors encountered during the operation.</returns>
        public static int BringUpToDate()
        {
            if (!FullyFunctional) return -1;
            Wood.WriteLine("Syncing mod versions.");
            Wood.Indent();
            var errc = 0;
            foreach (var mod in cargo) if (mod.Enabled && !BoiCustom.BOIC_Bytearr_Compare(mod.OrigChecksum, mod.TarCheckSum))
                {
                    Wood.WriteLine($"{mod} checksums not matching; bringing up to date.");
                    var orig = mod.AssociatedModData.OrigLocation;
                    var tar = new FileInfo(mod.TarPath);
                    string from;
                    string to;
                    if (orig.LastWriteTime > tar.LastWriteTime) { from = orig.FullName; to = tar.FullName; }
                    else { from = tar.FullName; to = orig.FullName; }
                    try
                    {
                        Wood.WriteLine($"Copying [ {from} ] to [ {to} ].");
                        File.Copy(from, to, true);
                        Wood.WriteLine($"{mod} updated.");
                    }
                    catch (Exception e)
                    {
                        Wood.WriteLine($"Error while updating {mod}:");
                        Wood.WriteLine(e);
                        errc++;
                    }
                }
            Wood.Unindent();
            Wood.WriteLine($"Sync complete, {errc} errors encountered.");
            return errc;
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
                using ModuleDefinition md = ModuleDefinition.ReadModule(path);
                return (md.Assembly.FullName.Contains("PublicityStunt"));
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
