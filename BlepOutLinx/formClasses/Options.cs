﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Blep.Backend;

namespace Blep
{
    public partial class Options : Form
    {
        public Options(BlepOut mainForm)
        {
            InitializeComponent();
            mf = mainForm;
            Wood.WriteLine("Options window opened " + DateTime.Now);
            labelREGIONNAME.Text = string.Empty;
            labelREGIONDESC.Text = string.Empty;
            labelSTRUCTURESTATUS.Text = string.Empty;
            regpacklist = new List<RegModData>();
            FetchStuff();
        }
        public Blep.BlepOut mf;
        private bool readytoapply = true;
        private RegModData curRmd;
        private List<RegModData> regpacklist;

        private void FetchStuff()
        {
            Wood.WriteLine("Fetching jsons and stuff.");
            CRSlist.Items.Clear();
            regpacklist.Clear();
            if (Directory.Exists(CRSpath))
            {
                string[] CRcts = Directory.GetDirectories(CRSpath);
                foreach (string path in CRcts)
                {
                    regpacklist.Add(new RegModData(path));
                }
                foreach (RegModData rmd in regpacklist)
                {
                    CRSlist.Items.Add(rmd);
                }

            }
            labelCRSCTR.Text = CRSlist.Items.Count.ToString();
            EDTCFGDATA.loadJo();

            if (Directory.Exists(langinplugins))
            {
                labelCOMMODSTATUS.Text = "Everything seems fine.";
                labelCOMMODDETAILS.Text = "Language folder is in the correct spot.";
                buttonMM2P.Visible = false;
            }
            else if (Directory.Exists(langinmods))
            {
                labelCOMMODSTATUS.Text = "Language folder is in the wrong spot!";
                labelCOMMODDETAILS.Text = @"Translation patch files have been found inside Mods but not Plugins. Press the button below to order BOI to move translation patch to RainWorld\BepInEx\plugins, or move it manually if you want it enabled.";
                buttonMM2P.Visible = true;
            }
            else
            {
                labelCOMMODSTATUS.Text = "Nothing found.";
                labelCOMMODDETAILS.Text = string.Empty;
                
                buttonMM2P.Visible = false;
            }
        }
        private void ApplyStuff()
        {
            foreach (RegModData rmd in regpacklist)
            {
                if (rmd.hasBeenChanged) rmd.WriteRegInfo();
            }
            if (EDTCFGDATA.hasBeenChanged)
            {
                EDTCFGDATA.SaveJo();
            }
            BackupManager.SaveSettingsForAll();
        }
        private void DrawCRSpage()
        {
            readytoapply = false;
            RegModData rmd = (CRSlist.SelectedIndex != -1) ? CRSlist.Items[CRSlist.SelectedIndex] as RegModData : null;
            curRmd = rmd;
            if (rmd == null)
            {
                labelREGIONNAME.Text = string.Empty;
                labelREGIONDESC.Text = string.Empty;
                labelSTRUCTURESTATUS.Text = string.Empty;
                checkBoxCR.Enabled = false;
                checkBoxCR.Checked = false;
                tbLOADORDER.Enabled = false;
                tbLOADORDER.Clear();
            }
            else
            {
                labelREGIONNAME.Text = rmd.regionName;
                labelREGIONDESC.Text = rmd.description;
                labelSTRUCTURESTATUS.Text = (rmd.structureValid) ? "VALID" : "INVALID";
                checkBoxCR.Enabled = rmd.CurrCfgState != RegModData.CfgState.None;
                tbLOADORDER.Enabled = rmd.CurrCfgState != RegModData.CfgState.None;
                tbLOADORDER.Text = (rmd.loadOrder != null) ? rmd.loadOrder.ToString() : string.Empty;
                checkBoxCR.Checked = rmd.activated;
            }
            readytoapply = true;
            
        }
        private void DrawEDTpage()
        {
            readytoapply = false;
            EDTCFGDATA.loadJo();
            if (EDTCFGDATA.jo == null)
            {
                tableLayoutPanel10.Enabled = false;
            }
            else
            {
                tableLayoutPanel10.Enabled = true;
                textBoxEDT_STARTMAP.Text = EDTCFGDATA.startmap;
                checkBoxEDT_QUICKSTART.Checked = (bool)EDTCFGDATA.skiptitle;
                textBoxEDT_CHARSELECT.Text = (EDTCFGDATA.forcechar == null) ? string.Empty : EDTCFGDATA.forcechar.ToString();
                checkBoxEDT_DISABLERAIN.Checked = (bool)EDTCFGDATA.norain;
                checkBoxEDT_EDT.Checked = (bool)EDTCFGDATA.devtools;
                TextBoxEDT_CHEATKARMA.Text = (EDTCFGDATA.cheatkarma == null) ? string.Empty : EDTCFGDATA.cheatkarma.ToString();
                checkBoxEDT_MAPREVEAL.Checked = (bool)EDTCFGDATA.revealmap;
                checkBoxEDT_FORCEGLOW.Checked = (bool)EDTCFGDATA.forcelight;
                checkBoxEDT_BAKE.Checked = (bool)EDTCFGDATA.bake;
                checkBoxEDT_ENCRYPT.Checked = (bool)EDTCFGDATA.encrypt;
            }
            readytoapply = true;
        }
        private void DrawBUMPage()
        {
            BackupManager.LoadBackupList();
            CompileBackupList(BackupSearchBar.Text);
            FillBars();
        }
        private static string CRSpath => Path.Combine(BlepOut.ModFolder, @"CustomResources");
        private void Options_Activated(object sender, EventArgs e)
        {
            //this.Enabled = mf.Enabled;
            if (BlepOut.IsMyPathCorrect)
            {
                //StatusUpdate();
                FetchStuff();
            }
            DrawCRSpage();
            DrawEDTpage();
            DrawBUMPage();
        }
        private void Options_Deactivate(object sender, EventArgs e)
        {
            ApplyStuff();
        }
        private void CRSlist_SelectedIndexChanged(object sender, EventArgs e)
        {
            DrawCRSpage();
        }
        private void checkBoxCR_EnabledChanged(object sender, EventArgs e)
        {
            if (curRmd == null) return;
            curRmd.activated = checkBoxCR.Checked;
        }
        private void Options_FormClosing(object sender, FormClosingEventArgs e)
        {
            ApplyStuff();
        }
        private void tbLOADORDER_Leave(object sender, EventArgs e)
        {
            if (curRmd == null || !readytoapply) return;
            if (int.TryParse(tbLOADORDER.Text, out int i))
            {
                curRmd.loadOrder = i;
            }
            
        }
        private void EDT_PROPERTY_CHANGED(object sender, EventArgs e)
        {
            if (!readytoapply) return;
            if (sender == textBoxEDT_STARTMAP)
            {
                EDTCFGDATA.startmap = textBoxEDT_STARTMAP.Text;
            }
            else if (sender == checkBoxEDT_QUICKSTART)
            {
                EDTCFGDATA.skiptitle = checkBoxEDT_QUICKSTART.Checked;
            }
            else if (sender == textBoxEDT_CHARSELECT)
            {
                if (int.TryParse(textBoxEDT_CHARSELECT.Text, out int res))
                {
                    EDTCFGDATA.forcechar = res;
                }

            }
            else if (sender == checkBoxEDT_DISABLERAIN)
            {
                EDTCFGDATA.norain = checkBoxEDT_DISABLERAIN.Checked;
            }
            else if (sender == checkBoxEDT_EDT)
            {
                EDTCFGDATA.devtools = checkBoxEDT_EDT.Checked;
            }
            else if (sender == TextBoxEDT_CHEATKARMA)
            {
                if (int.TryParse(TextBoxEDT_CHEATKARMA.Text, out int res))
                {
                    EDTCFGDATA.cheatkarma = res;
                }
            }
            else if (sender == checkBoxEDT_MAPREVEAL)
            {
                EDTCFGDATA.revealmap = checkBoxEDT_MAPREVEAL.Checked;
            }
            else if (sender == checkBoxEDT_FORCEGLOW)
            {
                EDTCFGDATA.forcelight = checkBoxEDT_FORCEGLOW.Checked;
            }
            else if (sender == checkBoxEDT_BAKE)
            {
                EDTCFGDATA.bake = checkBoxEDT_FORCEGLOW.Checked;
            }
            else if (sender == checkBoxEDT_ENCRYPT)
            {
                EDTCFGDATA.encrypt = checkBoxEDT_ENCRYPT.Checked;
            }
        }
        private void buttonclickCOMMOD(object sender, EventArgs e)
        {
            try
            {
                Directory.Move(langinmods, langinplugins);

            }
            catch (IOException ioe)
            {
                Wood.WriteLine("ERROR MOVING LANGUAGE FOLDER:");
                Wood.Indent();
                Wood.WriteLine(ioe);
                Wood.Unindent();
            }
            ApplyStuff();
            FetchStuff();
        }

        private string langinplugins => Path.Combine(BlepOut.PluginsFolder, "Language");
        private string langinmods => Path.Combine(BlepOut.ModFolder + "Language");

        //backups
        private bool DoBUelmUpdates = true;
        private void BackupRelatedBtnClick(object sender, EventArgs e)
        {
            if (!BlepOut.IsMyPathCorrect) return;
            if (ReferenceEquals(sender, btnNukeActiveSave))
            {
                BackupManager.TryDeleteSave(BackupManager.ActiveSave);
            }
            else if (ReferenceEquals(sender, btnMakeBackup))
            {
                BackupManager.StashActiveSave();
            }
            if (listBackups.SelectedItem == null) return;
            else if (ReferenceEquals(sender, btnDeletBackup))
            {
                if (listBackups.SelectedItem != null) BackupManager.TryDeleteSave((BackupManager.UserDataStateRelay)listBackups.SelectedItem);
            }
            else if (ReferenceEquals(sender, btnRestoreBackup))
            {
                BackupManager.TryDeleteSave(BackupManager.ActiveSave);
                BackupManager.ActiveSave = ((BackupManager.UserDataStateRelay)listBackups.SelectedItem).CloneTo(BackupManager.UserDataFolder);
            }
            BackupManager.SaveSettingsForAll();
            DrawBUMPage();
        }

        public void CompileBackupList(string mask)
        {
            listBackups.Items.Clear();
            if (!BlepOut.IsMyPathCorrect) return;
            listBackups.Items.Add(BackupManager.ActiveSave);
            foreach (BackupManager.UserDataStateRelay udsr in BackupManager.AllBackups)
            {
                if (UDSRSelectedByMask(udsr, mask)) listBackups.Items.Add(udsr);
            }
        }
        private bool UDSRSelectedByMask(BackupManager.UserDataStateRelay udsr, string mask)
        {
            if (mask == string.Empty || udsr.MyName.ToLower().Contains(mask.ToLower()) || udsr.UserNotes.ToLower().Contains(mask.ToLower()) || udsr.DateTimeString.ToLower().Contains(mask.ToLower())) return true;
            return false;
        }

        private void listBackups_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillBars();
        }
        private void FillBars()
        {
            DoBUelmUpdates = false;
            BackupManager.UserDataStateRelay udsr = (BackupManager.UserDataStateRelay)listBackups.SelectedItem;
            NameBox.Text = udsr?.UserDefinedName ?? string.Empty;
            NoteBox.Text = udsr?.UserNotes ?? string.Empty;
            labelBUcreationTime.Text = udsr?.DateTimeString;
            DoBUelmUpdates = true;
        }

        private void BackupSearchBar_TextChanged(object sender, EventArgs e)
        {
            
            CompileBackupList(BackupSearchBar.Text);
        }

        private void NameBox_TextChanged(object sender, EventArgs e)
        {
            if (listBackups.SelectedItem == null) return;
            BackupManager.UserDataStateRelay sudsr = (BackupManager.UserDataStateRelay)listBackups.SelectedItem;
            sudsr.UserDefinedName = NameBox.Text;
            
        }

        private void NoteBox_TextChanged(object sender, EventArgs e)
        {
            if (listBackups.SelectedItem == null) return;
            BackupManager.UserDataStateRelay sudsr = (BackupManager.UserDataStateRelay)listBackups.SelectedItem;
            sudsr.UserNotes = NoteBox.Text;
        }
    }

    

}