using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public partial class ExcludePlaylistsForm : Form
    {
        private CheckedListBox playlistList;
        private Button okButton;
        private Button cancelButton;
        private HashSet<string> excludedPlaylists;

        public ExcludePlaylistsForm(HashSet<string> currentExclusions)
        {
            excludedPlaylists = new HashSet<string>(currentExclusions);
            InitializeComponent();
            PopulateList();
        }

        private void InitializeComponent()
        {
            this.playlistList = new CheckedListBox();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.SuspendLayout();

            // playlistList
            this.playlistList.Dock = DockStyle.Top;
            this.playlistList.Location = new Point(10, 10);
            this.playlistList.Name = "playlistList";
            this.playlistList.Size = new Size(380, 250);
            this.playlistList.TabIndex = 0;
            this.playlistList.CheckOnClick = true;

            // okButton
            this.okButton.Location = new Point(234, 270);
            this.okButton.Name = "okButton";
            this.okButton.Size = new Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "Ok";
            this.okButton.Click += new EventHandler(this.OkButton_Click);

            // cancelButton
            this.cancelButton.Location = new Point(315, 270);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.Click += new EventHandler(this.CancelButton_Click);

            // ExcludePlaylistsForm
            this.ClientSize = new Size(400, 300);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.playlistList);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Exclude Playlists";
            this.ResumeLayout(false);
        }

        private void PopulateList()
        {
            var playlists = GetAllPlaylists().Select(p => p.Name).ToList();
            playlistList.Items.AddRange(playlists.ToArray());
            
            // Check items that are in the exclusion list
            for (int i = 0; i < playlistList.Items.Count; i++)
            {
                if (excludedPlaylists.Contains(playlistList.Items[i].ToString()))
                {
                    playlistList.SetItemChecked(i, true);
                }
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            excludedPlaylists.Clear();
            foreach (var item in playlistList.CheckedItems)
            {
                excludedPlaylists.Add(item.ToString());
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                CancelButton_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public HashSet<string> GetExcludedPlaylists()
        {
            return excludedPlaylists;
        }
    }
}
