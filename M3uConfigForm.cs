using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class M3uConfigForm : Form
    {
        private string playlistRootPath;
        private Config config;
        private CheckBox listenerCheckBox;
        private CheckBox relPathsCheckBox;
        private CheckBox slashCheckBox;

        public M3uConfigForm(string rootPath, Config config)
        {
            this.playlistRootPath = rootPath;
            this.config = config;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            var infoLabel = new Label();
            infoLabel.AutoSize = true;
            infoLabel.Location = new Point(20, 20);
            infoLabel.Text = "Common M3U Playlist Root Path:";
            infoLabel.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 10f, FontStyle.Bold);
            
            var pathLabel = new Label();
            pathLabel.AutoSize = true;
            pathLabel.Location = new Point(20, 50);
            pathLabel.Text = string.IsNullOrEmpty(playlistRootPath) ? "(None detected)" : playlistRootPath;
            pathLabel.ForeColor = SystemColors.GrayText;

            this.listenerCheckBox = new CheckBox();
            this.listenerCheckBox.AutoSize = true;
            this.listenerCheckBox.Location = new Point(23, 80);
            this.listenerCheckBox.Text = "Enable File Listener (disables standard event updates for M3Us)";
            this.listenerCheckBox.Checked = config.M3uFileListenerEnabled;
            this.listenerCheckBox.Enabled = !string.IsNullOrEmpty(playlistRootPath);
            this.listenerCheckBox.Width = 450;
            this.listenerCheckBox.CheckedChanged += ListenerCheckBox_CheckedChanged;

            this.relPathsCheckBox = new CheckBox();
            this.relPathsCheckBox.AutoSize = true;
            this.relPathsCheckBox.Location = new Point(23, 110);
            this.relPathsCheckBox.Text = "Use relative paths";
            this.relPathsCheckBox.Checked = config.M3uUseRelativePaths;
            this.relPathsCheckBox.Enabled = listenerCheckBox.Checked && listenerCheckBox.Enabled;
            this.relPathsCheckBox.Width = 450;

            this.slashCheckBox = new CheckBox();
            this.slashCheckBox.AutoSize = true;
            this.slashCheckBox.Location = new Point(23, 140);
            this.slashCheckBox.Text = "Enforce forward slash for all M3U playlists";
            this.slashCheckBox.Checked = config.M3uEnforceForwardSlash;
            this.slashCheckBox.Enabled = listenerCheckBox.Checked && listenerCheckBox.Enabled;
            this.slashCheckBox.Width = 450;

            var okButton = new Button();
            okButton.Text = "OK";
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(310, 170);
            okButton.Click += OkButton_Click;

            var cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(390, 170);

            this.ClientSize = new Size(500, 210);
            this.Controls.Add(infoLabel);
            this.Controls.Add(pathLabel);
            this.Controls.Add(this.listenerCheckBox);
            this.Controls.Add(this.relPathsCheckBox);
            this.Controls.Add(this.slashCheckBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
            
            this.Name = "M3uConfigForm";
            this.Text = "M3U Configuration";
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.FormBackColor;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void ListenerCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            relPathsCheckBox.Enabled = listenerCheckBox.Checked;
            slashCheckBox.Enabled = listenerCheckBox.Checked;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            config.M3uFileListenerEnabled = listenerCheckBox.Checked;
            config.M3uUseRelativePaths = relPathsCheckBox.Checked;
            config.M3uEnforceForwardSlash = slashCheckBox.Checked;
            this.Close();
        }
    }
}