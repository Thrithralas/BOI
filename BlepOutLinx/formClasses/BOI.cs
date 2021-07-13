using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;
using Blep.Backend;

namespace Blep
{
    /// <summary>
    /// Main window form and actual mod manager - all in one
    /// </summary>
    public partial class BlepOut : Form
    {
        /// <summary>
        /// The one and only ctor
        /// </summary>
        public BlepOut()
        {
#warning nuke all the old intertwined UI/modlist code
            InitializeComponent();            
            this.Text = this.Text.Replace("<VersionNumber>", VersionNumber);
            firstshow = true;
            MaskModeSelect.Items.AddRange(new object[] {Maskmode.Names, Maskmode.Tags, Maskmode.NamesAndTags});
            MaskModeSelect.SelectedItem = Maskmode.NamesAndTags;
            Wood.SetNewPathAndErase(Path.Combine(Directory.GetCurrentDirectory(), "BOILOG.txt"));
            Wood.WriteLine("BOI starting " + DateTime.Now);
            outrmixmods = new List<string>();
            TagManager.ReadTagsFromFile(tagfilePath);
            BoiConfigManager.ReadConfig();
            UpdateTargetPath(BoiConfigManager.TarPath);
            firstshow = false;
            if (File.Exists(Path.Combine(RootPath, "BepInEx", "LogOutput.log")))
            {
                string[] lans = File.ReadAllLines(Path.Combine(RootPath, "BepInEx", "LogOutput.log"));
                for (int cuwo = 0; cuwo < lans.Length; cuwo++)
                {
                    string scrpyr = lans[cuwo];
                    if (scrpyr.Contains("Here be dragons!"))
                    {

                        Wood.WriteLine("Dragon thoughts found. Saying hi.");
                        goto iolaa;
                    }
                }
            iolaa:
                {
                    Wood.WriteLine("...");
                    Wood.WriteLine("To you and your parent, greetings. May your work persist for as long as we do.");
                    Wood.WriteLine("Wish you all well. Bzz!");
                }
            }
            //if (VoiceOfBees.ModEntryList.Count > 0) { VoiceOfBees.ModEntryList[0].TryDownload(ModFolder); }
        }
        
        /// <summary>
        /// Ran every time there's a need to refresh mod manager's state, with new path or the same one as before.
        /// Runs regardless of whether selected path is valid or not.
        /// </summary>
        /// <param name="path">Path to check</param>
        public void UpdateTargetPath(string path)
        {
            //btnLaunch.Enabled = false;
            Modlist.Enabled = false;
            RootPath = path;
            BoiConfigManager.TarPath = path;
            if (IsMyPathCorrect) Setup();
            StatusUpdate();
        }

        /// <summary>
        /// Continuation of <see cref="UpdateTargetPath(string)"/>: only ran when selected path is valid. 
        /// <para>
        /// Loads file blacklists, does active folder cleanup / mod file retrieval and compiles modlist.
        /// </para>
        /// Also displays certain popup windows. See: <see cref="PubstuntInfoPopup"/>, <see cref="MixmodsPopup"/>
        /// </summary>
        private void Setup()
        {
            Wood.WriteLine("Path valid, starting setup " + DateTime.Now);
            Wood.Indent();
            PubstuntFound = false;
            MixmodsFound = false;
            metafiletracker = false;
            Modlist.Items.Clear();
            outrmixmods.Clear();

            Donkey.TryLoadCargo(new DirectoryInfo(ModFolder));
            Donkey.SetBepPatcherTarget(PatchersFolder);
            Donkey.SetMmpTarget(mmFolder);
            Donkey.SetPluginsTarget(PluginsFolder);
            Donkey.CriticalSweep();
            PrepareModsFolder();
            FillModList();
            Modlist.Enabled = true;
            //btnLaunch.Enabled = true;
            TargetSelect.SelectedPath = RootPath;
            if (PubstuntFound && firstshow)
            {
                PubstuntInfoPopup popup;
                popup = new PubstuntInfoPopup();
                AddOwnedForm(popup);
                popup.Show();
            }
            if (MixmodsFound)
            {
                MixmodsPopup mixmodsPopup = new Blep.MixmodsPopup(outrmixmods);
                AddOwnedForm(mixmodsPopup);
                mixmodsPopup.Show();
            }
            buttonClearMeta.Visible = metafiletracker;
            Wood.Unindent();
        }
        /// <summary>
        /// Creates mods folder if there isn't one; checks if there is leftover PL junk.
        /// </summary>
        private void PrepareModsFolder()
        {
            metafiletracker = false;
            if (!Directory.Exists(ModFolder))
            {
                Wood.WriteLine("Mods folder not found, creating.");
                Directory.CreateDirectory(ModFolder);
            }
            string[] modfldcontents = Directory.GetFiles(ModFolder);
            foreach (string path in modfldcontents)
            {
                var fi = new FileInfo(path);
                if (fi.Extension == ".modHash" || fi.Extension == ".modMeta")
                {
                    metafiletracker = true;
                }
            }
            Wood.WriteLineIf(metafiletracker, "Found modhash/modmeta files in mods folder.");
        }

        /// <summary>
        /// Adds all mods from <see cref="Donkey.cargo" into current modlist/>
        /// </summary>
        private void FillModList()
        {
#warning unfinished

            Modlist.Items.Clear();
            Modlist.ItemCheck -= Modlist_ItemCheck;
            foreach (var mod in Donkey.cargo) { Modlist.Items.Add(mod); Modlist.SetItemChecked(Modlist.Items.Count - 1, mod.enabled); }

            Modlist.ItemCheck += Modlist_ItemCheck;
        }
        /// <summary>
        /// Applies search mask to visible modlist; depending on selected search mode, names and/or tags will be accounted for.
        /// </summary>
        /// <param name="mask">Mask contents.</param>
        private void ApplyMaskToModlist(string mask)
        {
            Modlist.Items.Clear();
            Modlist.ItemCheck -= Modlist_ItemCheck;
            foreach (var mod in Donkey.cargo) { if (ModSelectedByMask(mask, mod)) Modlist.Items.Add(mod); Modlist.SetItemChecked(Modlist.Items.Count - 1, mod.enabled); }
            Modlist.ItemCheck += Modlist_ItemCheck;
        }

        /// <summary>
        /// Returns if a <see cref="ModRelay"/> is selected by a given mask.
        /// </summary>
        /// <param name="mask">Mask text.</param>
        /// <param name="mr"><see cref="ModRelay"/> to be checked.</param>
        /// <returns></returns>
        private bool ModSelectedByMask(string mask, ModRelay mr)
        {
            if (mask == string.Empty) return true;
            string cmm = MaskModeSelect.Text;
            if (cmm == nameof(Maskmode.Names) || cmm == nameof(Maskmode.NamesAndTags)) if (mr.ToString().ToLower().Contains(mask.ToLower())) return true;
            if (cmm == nameof(Maskmode.NamesAndTags) || cmm == nameof(Maskmode.Tags))
            {
                string[] tags = TagManager.GetTagsArray(mr.AssociatedModData.DisplayedName);
                foreach (string tag in tags)
                {
                    if (tag.ToLower().Contains(mask.ToLower())) return true;
                }
            }
            
                
            return false;
        }

        [Flags]
        private enum Maskmode
        {
            Tags,
            Names,
            NamesAndTags
        }

        /// <summary>
        /// Checks if enabled mods are identical to their counterparts in active folders; if not, brings the needed side up to date.
        /// </summary>
        public static bool IsMyPathCorrect
        {
            get { return (currentStructureState.HasFlag(FolderStructureState.BlepFound | FolderStructureState.GameFound)); }
        }

        /// <summary>
        /// Gets <see cref="FolderStructureState"/> for currently selected path.
        /// </summary>
        private static FolderStructureState currentStructureState 
        {
            get
            {
                FolderStructureState res = 0;
                if (Directory.Exists(PluginsFolder) && Directory.Exists(mmFolder)) res |= FolderStructureState.BlepFound;
                if (Directory.Exists(Path.Combine(RootPath, "RainWorld_Data"))) res |= FolderStructureState.GameFound;
                return res;
            }
        }

        [Flags]
        private enum FolderStructureState
        {
            BlepFound = 1,
            GameFound = 2
        }

        /// <summary>
        /// Path to the game's root folder.
        /// </summary>
        public static string RootPath = string.Empty;
        /// <summary>
        /// Indicates whether modhash/modmeta files were found during setup.
        /// </summary>
        private static bool metafiletracker;
        /// <summary>
        /// State tracker for path select dialog and button; janky, definitely subject to change.
        /// </summary>
        private static bool TSbtnMode = true;
        /// <summary>
        /// <see cref="Options"/> form instance.
        /// </summary>
        private Options opwin;
        /// <summary>
        /// <see cref="InvalidModPopup"/> form instance.
        /// </summary>
        private InvalidModPopup inp;
        /// <summary>
        /// <see cref="InfoWindow"/> form instance.
        /// </summary>
        private InfoWindow iw;
        /// <summary>
        /// Returns BOI folder path.
        /// </summary>
        public static string BOIpath => Directory.GetCurrentDirectory();
        /// <summary>
        /// Returns path to BOI config file.
        /// </summary>
        public static string cfgpath => Path.Combine(BOIpath, "cfg.json");
        /// <summary>
        /// Returns path to tag data file.
        /// </summary>
        public static string tagfilePath => Path.Combine(BOIpath, "MODTAGS.txt");
        /// <summary>
        /// List of mods that have been erased during rootout.
        /// </summary>
        private List<string> outrmixmods;
        /// <summary>
        /// Indicates whether the form is being viewed for the first time in the session.
        /// </summary>
        private bool firstshow;
        /// <summary>
        /// Setup tracker for pubstunt.
        /// </summary>
        private bool PubstuntFound;
        /// <summary>
        /// Setup tracker for invalid mods.
        /// </summary>
        private bool MixmodsFound;        
        public static string ModFolder => Path.Combine(RootPath, "Mods");
        public static string PluginsFolder => Path.Combine(RootPath, "BepInEx", "plugins");
        public static string mmFolder => Path.Combine(RootPath, "BepInEx", "monomod");
        public static string PatchersFolder => Path.Combine(RootPath, "BepInEx", "patchers");

        /// <summary>
        /// Ran when the user clicks path select button. <see cref="TSbtnMode"/> is used here.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="e">Unused.</param>
        private void buttonSelectPath_Click(object sender, EventArgs e)
        {
            if (TSbtnMode)
            {
                TargetSelect.ShowDialog();
                btnSelectPath.Text = "Press again to load modlist";
                TSbtnMode = false;
            }
            else
            {
                UpdateTargetPath(TargetSelect.SelectedPath);
                btnSelectPath.Text = "Select path";
                TSbtnMode = true;
            }
        }
        /// <summary>
        /// Ran when the window is brought into focus.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="e">Unused.</param>
        private void BlepOut_Activated(object sender, EventArgs e)
        {
            UpdateTargetPath(RootPath);
            StatusUpdate();
            ApplyMaskToModlist(textBox_MaskInput.Text);
            buttonUprootPart.Visible = Directory.Exists(Path.Combine(RootPath, "RainWorld_Data", "Managed_backup"));
        }
        private void BlepOut_Deactivate(object sender, EventArgs e)
        {
            if (IsMyPathCorrect && Directory.Exists(ModFolder))
            {
#warning empty action block? might have wanted to add something
            }
            TagManager.SaveToFile(tagfilePath);
        }
        /// <summary>
        /// Updates the status bars.
        /// </summary>
        private void StatusUpdate()
        {
            var cf = currentStructureState;
            if (cf.HasFlag(FolderStructureState.BlepFound | FolderStructureState.GameFound))
            {
                lblPathStatus.Text = "Path valid";
                lblPathStatus.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightGreen);
            }
            else if (cf.HasFlag(FolderStructureState.GameFound))
            {
                lblPathStatus.Text = "No bepinex";
                lblPathStatus.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightYellow);
            }
            else
            {
                lblPathStatus.Text = "Path invalid";
                lblPathStatus.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Salmon);
            }

            //lblProcessStatus.Visible = IsMyPathCorrect;
            //lblProcessStatus.Text = (rw != null && !rw.HasExited) ? "Running" : "Not running";
            //lblProcessStatus.BackColor = (rw != null && !rw.HasExited) ? System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Orange) : System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Gray);
            //if (rw != null && !rw.HasExited)
            //{
            //    Modlist.Enabled = false;

            //}
            //else Modlist.Enabled = true;
            
            lblProcessStatus.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.PaleTurquoise);
            Modlist.Enabled = IsMyPathCorrect;
            //btnLaunch.Enabled = Modlist.Enabled;
            btnSelectPath.Enabled = true;

        }
        
        /// <summary>
        /// Brings up the help window.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="e">Unused.</param>
        private void btn_Help_Click(object sender, EventArgs e)
        {
            if (iw == null || iw.IsDisposed) iw = new Blep.InfoWindow(this);
            iw.Show();
        }
        /// <summary>
        /// Shows PL uproot dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonUprootPart_Click(object sender, EventArgs e)
        {
            Blep.PartYeet py = new Blep.PartYeet(this);
            AddOwnedForm(py);
            py.ShowDialog();
        }
        /// <summary>
        /// Brings up PL junk cleanup dialog.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="e">Unused.</param>
        private void buttonClearMeta_Click(object sender, EventArgs e)
        {
            Blep.MetafilePurgeSuggestion psg = new Blep.MetafilePurgeSuggestion(this);
            AddOwnedForm(psg);
            psg.ShowDialog();
        }
        /// <summary>
        /// Ran every time a modlist item is checked.
        /// </summary>
        /// <param name="sender">Unused.</param>
        /// <param name="e"></param>
        private void Modlist_ItemCheck(object sender, ItemCheckEventArgs e)
        {
#warning readd invalid mod warnings, additional pubstunt check
            var tm = Modlist.Items[e.Index] as ModRelay;
            if (e.NewValue == CheckState.Checked) tm.Enable();
            else tm.Disable();
        }

        /// <summary>
        /// Ran when the form is closing, as the name suggests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlepOut_FormClosing(object sender, FormClosingEventArgs e)
        {
            Wood.WriteLine("BOI shutting down. " + DateTime.Now);
            BoiConfigManager.WriteConfig();
            List<string> mns = new List<string>();
            foreach (ModRelay mr in Donkey.cargo) mns.Add(mr.AssociatedModData.DisplayedName);
            TagManager.TagCleanup(mns.ToArray());
            TagManager.SaveToFile(tagfilePath);
        }
        /// <summary>
        /// Brings up the options + tools dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOption_Click(object sender, EventArgs e)
        {
            if (opwin == null || opwin.IsDisposed) opwin = new Options(this);
            opwin.Show();
        }
        /// <summary>
        /// Used to handle data drop events for modlist.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Modlist_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Wood.WriteLine($"Drag&Drop: {files.Length} files were dropped in the mod list.");
            Wood.Indent();
            foreach (string file in files)
            {
                // get the file info for easier operations
                FileInfo ModFileInfo = new FileInfo(file);
                // check if we are dealing with a dll file
                if (!String.Equals(ModFileInfo.Extension, ".dll", StringComparison.CurrentCultureIgnoreCase))
                {
                    Wood.WriteLine($"Error: {ModFileInfo.Name} was ignored, as it is not a dll file.");
                    continue;
                }
                // move the dll file to the Mods folder
                string ModFilePath = Path.Combine(RootPath, "Mods", ModFileInfo.Name);
                if(File.Exists(ModFilePath))
                {
                    Wood.WriteLine($"Error: {ModFileInfo.Name} was ignored, as it already exists.");
                    continue;
                }
                // move the dll file to the Mods folder
                File.Copy(ModFileInfo.FullName, ModFilePath);
                // get mod data
                var mr = new ModRelay(ModFilePath);
                // add the mod to the mod list
                Donkey.cargo.Add(mr);
                Wood.WriteLine($"{ModFileInfo.Name} successfully added.");
                // since it's a new mod just added to the folder, it shouldn't be checked as active, nothing else to do here
            }
            Wood.Unindent();
            Wood.WriteLine("Drag&Drop operation ended.");
        }
        /// <summary>
        /// Used to handle data drop events for modlist.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Modlist_DragEnter(object sender, DragEventArgs e)
        {
            // if we're about to drop a file, indicate a copy to allow the drop
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
        private void textBoxMaskInput_TextChanged(object sender, EventArgs e)
        {
            ApplyMaskToModlist(textBox_MaskInput.Text);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Modlist_SelectionChanged(object sender, EventArgs e)
        {
            if (Modlist.SelectedItem == null) return;
            TagInputBox.Text = TagManager.GetTagString(((ModRelay)Modlist.SelectedItem)?.AssociatedModData.DisplayedName);
        }
        private void TagTextChanged(object sender, EventArgs e)
        {
            if (Modlist.SelectedItem == null) return;
            TagManager.SetTagData(((ModRelay)Modlist.SelectedItem).AssociatedModData.DisplayedName, TagInputBox.Text);
        }
        private void MaskModeSelect_TextChanged(object sender, EventArgs e)
        {
            ApplyMaskToModlist(textBox_MaskInput.Text);
        }
        /// <summary>
        /// Brings up AUDBrowser.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenAudbBrowser(object sender, EventArgs e)
        {
            if (browser == null || browser.IsDisposed) { browser = new AUDBBrowser(); browser.Show(); }
        }
        private AUDBBrowser browser;


        public static string VersionNumber => "0.1.5+";
    }
}
