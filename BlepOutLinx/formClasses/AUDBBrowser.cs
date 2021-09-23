﻿using System;
using System.Windows.Forms;
using Blep.Backend;

namespace Blep
{
    public partial class AUDBBrowser : Form
    {
        public AUDBBrowser()
        {
            InitializeComponent();
            FetchAndRefresh();
        }

        private void RefreshTriggered(object sender, EventArgs e)
        {
            FetchAndRefresh();
        }
        
        public void FetchAndRefresh()
        {
            bool fl = VoiceOfBees.FetchList();
            listAUDBEntries.Items.Clear();
            foreach (var rel in VoiceOfBees.ModEntryList)
            {
                listAUDBEntries.Items.Add(rel);
            }
            DrawBoxes();
            if (!fl) labelOperationStatus.Text = "Retrieving modlist failed, check BOILOG.txt for details";
        }

        public void DrawBoxes()
        {
            buttonDownload.Enabled = BlepOut.IsMyPathCorrect;
            var currEntry = listAUDBEntries.SelectedItem as VoiceOfBees.AUDBEntryRelay;

            labelEntryAuthors.Text = currEntry?.author ?? string.Empty;
            labelEntryDescription.Text = currEntry?.description ?? string.Empty;
            labelEntryName.Text = currEntry?.name ?? string.Empty;
            listDeps.Items.Clear();
            if (currEntry?.deps != null) foreach (var dep in currEntry.deps) listDeps.Items.Add(currEntry);
            labelOperationStatus.Text = "[Idle]";
        }

        private void listAUDBEntries_SelectedIndexChanged(object sender, EventArgs e)
        {
            DrawBoxes();
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            
            var currEntry = listAUDBEntries.SelectedItem as VoiceOfBees.AUDBEntryRelay;
            if (currEntry != null)
            {
                labelOperationStatus.Text = $"Downloading {currEntry.name} and dependencies...";
                labelOperationStatus.Text = (currEntry.TryDownload(BlepOut.ModFolder))? $"Downloaded {currEntry.name}." : $"Could not download {currEntry.name}! Check BOILOG.txt for details";
            }
        }
    }
}
