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

            var okButton = new Button();
            okButton.Text = "OK";
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(310, 110);
            okButton.Click += OkButton_Click;

            var cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(390, 110);

            this.ClientSize = new Size(500, 150);
            this.Controls.Add(infoLabel);
            this.Controls.Add(pathLabel);
            this.Controls.Add(this.listenerCheckBox);
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

        private void OkButton_Click(object sender, EventArgs e)
        {
            config.M3uFileListenerEnabled = listenerCheckBox.Checked;
            this.Close();
        }
    }
}