using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public partial class ConfigForm : Form
    {
        public event Action<Config> UpdateAllPlaylists;
        private MusicBeeApiInterface mbApi;
        private Config config;
        private FlowLayoutPanel playlistPanel;
        private Button okButton;
        private Button cancelButton;
        private Button updateButton;

        public ConfigForm(MusicBeeApiInterface api, Config config)
        {
            mbApi = api;
            this.config = new Config(config);
            InitializeComponent();
            PopulatePlaylists();
        }

        private void InitializeComponent()
        {
            this.playlistPanel = new FlowLayoutPanel();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.updateButton = new Button();
            this.SuspendLayout();
            // 
            // playlistPanel
            // 
            this.playlistPanel.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.playlistPanel.AutoScroll = true;
            this.playlistPanel.Location = new Point(12, 12);
            this.playlistPanel.Name = "playlistPanel";
            this.playlistPanel.Size = new Size(776, 387);
            this.playlistPanel.TabIndex = 0;
            this.playlistPanel.FlowDirection = FlowDirection.TopDown;
            this.playlistPanel.WrapContents = false;
            // 
            // okButton
            // 
            this.okButton.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.okButton.Location = new Point(632, 415);
            this.okButton.Name = "okButton";
            this.okButton.Size = new Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new EventHandler(this.OkButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.cancelButton.Location = new Point(713, 415);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new EventHandler(this.CancelButton_Click);
            // 
            // updateButton
            // 
            this.updateButton.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.updateButton.Location = new Point(551, 415);
            this.updateButton.Name = "updateButton";
            this.updateButton.Size = new Size(75, 23);
            this.updateButton.TabIndex = 3;
            this.updateButton.Text = "Update All";
            this.updateButton.UseVisualStyleBackColor = true;
            this.updateButton.Click += new EventHandler(this.UpdateButton_Click);
            // 
            // ConfigForm
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(800, 450);
            this.Controls.Add(this.updateButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.playlistPanel);
            this.MinimumSize = new Size(600, 400);
            this.Name = "ConfigForm";
            this.Text = "Auto-Update Playlist Order Configuration";
            this.StartPosition = FormStartPosition.CenterParent;
            this.KeyPreview = true;
            this.ResumeLayout(false);

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                CancelButton_Click(this, EventArgs.Empty); // Treat Escape as Cancel
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData); // Let base class handle other keys
        }

        private void PopulatePlaylists()
        {
            playlistPanel.SuspendLayout();
            playlistPanel.Controls.Clear();

            // "AllPlaylists" as "Default Playlist Sort"
            AddPlaylistControl("AllPlaylists", "Default Playlist Sort");

            // Configured playlists
            var configuredPlaylists = config.PlaylistConfig.Keys
                .Where(p => p != "AllPlaylists")
                .OrderBy(p => p)
                .ToList();

            if (configuredPlaylists.Any())
            {
                AddSectionHeader("Custom Playlist Sort Orders");
                foreach (var playlistName in configuredPlaylists)
                {
                    AddPlaylistControl(playlistName, playlistName);
                }
            }

            // Non-configured playlists
            var allPlaylistNames = GetAllPlaylists().Select(p => p.Name).ToList();
            var nonConfiguredPlaylists = allPlaylistNames
                .Except(config.PlaylistConfig.Keys)
                .OrderBy(p => p)
                .ToList();

            if (nonConfiguredPlaylists.Any())
            {
                AddSectionHeader("No Custom Order (using default)");
                foreach (var playlistName in nonConfiguredPlaylists)
                {
                    AddPlaylistControl(playlistName, playlistName);
                }
            }

            playlistPanel.ResumeLayout();
        }

        private void AddPlaylistControl(string playlistName, string displayName)
        {
            config.PlaylistConfig.TryGetValue(playlistName, out var ordersConfig);
            var control = new PlaylistConfigControl(playlistName, displayName, ordersConfig)
            {
                Width = playlistPanel.ClientSize.Width - 10
            };
            control.ConfigureClicked += OnConfigureClicked;
            control.ClearClicked += OnClearClicked;
            playlistPanel.Controls.Add(control);
        }

        private void AddSectionHeader(string text)
        {
            var label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0),
                ForeColor = SystemColors.HotTrack,
                AutoSize = false,
                Size = new Size(playlistPanel.ClientSize.Width - 10, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(3, 15, 3, 5)
            };
            playlistPanel.Controls.Add(label);
        }

        public Config GetConfig()
        {
           return config;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            UpdateAllPlaylists?.Invoke(GetConfig());
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnConfigureClicked(object sender, EventArgs e)
        {
            var control = sender as PlaylistConfigControl;
            if (control == null) return;

            var playlistName = control.PlaylistName;

            var currentOrderConfig = config.GetOrderConfigForPlaylist(playlistName) ?? new OrdersConfig();

            using (var orderConfigForm = new OrderConfigForm(currentOrderConfig.Orders.Select(o => (o.Order, o.Descending)).ToList(), playlistName))
            {
                if (orderConfigForm.ShowDialog(this) == DialogResult.OK)
                {
                    var newOrders = orderConfigForm.GetOrderConfig()
                        .Select(o => new OrderItem(o.Order, o.Descending))
                        .ToList();

                    config.SetOrderConfigForPlaylist(playlistName, new OrdersConfig { Orders = newOrders });
                    PopulatePlaylists();
                }
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            var control = sender as PlaylistConfigControl;
            if (control == null) return;

            config.SetOrderConfigForPlaylist(control.PlaylistName, null);
            PopulatePlaylists();
        }
    }
}
