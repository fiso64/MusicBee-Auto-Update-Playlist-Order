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
        private DoubleBufferedFlowLayoutPanel playlistPanel;
        private TextBox searchTextBox;
        private Button okButton;
        private Button cancelButton;
        private Button updateButton;

        public ConfigForm(MusicBeeApiInterface api, Config config)
        {
            mbApi = api;
            this.config = new Config(config);
            InitializeComponent();
            PopulatePlaylists();
            this.Load += ConfigForm_Load;
            this.KeyDown += new KeyEventHandler(this.ConfigForm_KeyDown);
        }

        private void InitializeComponent()
        {
            this.playlistPanel = new DoubleBufferedFlowLayoutPanel();
            this.searchTextBox = new TextBox();
            var searchLabel = new Label();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.updateButton = new Button();
            this.SuspendLayout();
            // 
            // searchLabel
            // 
            searchLabel.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            searchLabel.AutoSize = true;
            searchLabel.Location = new Point(545, 15);
            searchLabel.Name = "searchLabel";
            searchLabel.Text = "Search:";
            // 
            // searchTextBox
            // 
            this.searchTextBox.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.searchTextBox.Location = new Point(595, 12);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new Size(193, 20);
            this.searchTextBox.TabIndex = 4;
            this.searchTextBox.TextChanged += new EventHandler(this.SearchTextBox_TextChanged);
            this.searchTextBox.KeyDown += new KeyEventHandler(this.SearchTextBox_KeyDown);
            // 
            // playlistPanel
            // 
            this.playlistPanel.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.playlistPanel.AutoScroll = true;
            this.playlistPanel.Location = new Point(12, 40);
            this.playlistPanel.Name = "playlistPanel";
            this.playlistPanel.Size = new Size(776, 359);
            this.playlistPanel.TabIndex = 0;
            this.playlistPanel.FlowDirection = FlowDirection.TopDown;
            this.playlistPanel.WrapContents = false;
            this.playlistPanel.Padding = new Padding(0, 0, 0, 0);
            this.playlistPanel.Resize += new System.EventHandler(this.PlaylistPanel_Resize);
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
            this.updateButton.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Left)));
            this.updateButton.Location = new Point(12, 11);
            this.updateButton.Name = "updateButton";
            this.updateButton.Size = new Size(85, 23);
            this.updateButton.TabIndex = 3;
            this.updateButton.Text = "Update All";
            this.updateButton.UseVisualStyleBackColor = true;
            this.updateButton.Click += new EventHandler(this.UpdateButton_Click);
            // 
            // ConfigForm
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(800, 800);
            this.BackColor = Theme.FormBackColor;
            this.Controls.Add(searchLabel);
            this.Controls.Add(this.searchTextBox);
            this.Controls.Add(this.updateButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.playlistPanel);
            this.MinimumSize = new Size(600, 400);
            this.Name = "ConfigForm";
            this.Text = "Auto-Update Playlist Order Configuration";
            this.StartPosition = FormStartPosition.CenterScreen;
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

            var filterText = searchTextBox?.Text ?? "";

            // "AllPlaylists" as "Default Playlist Sort" is only visible when not searching
            if (string.IsNullOrEmpty(filterText))
            {
                AddPlaylistControl("AllPlaylists", "Default Playlist Sort");
            }

            // Configured playlists
            var configuredPlaylists = config.PlaylistConfig.Keys
                .Where(p => p != "AllPlaylists")
                .ToList();

            if (!string.IsNullOrEmpty(filterText))
            {
                var searchTerms = filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                configuredPlaylists = configuredPlaylists
                    .Where(p => searchTerms.All(term => p.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
            }
            configuredPlaylists = configuredPlaylists.OrderBy(p => p).ToList();

            if (configuredPlaylists.Any())
            {
                AddSectionHeader("Sorted Playlists");
                foreach (var playlistName in configuredPlaylists)
                {
                    AddPlaylistControl(playlistName, playlistName);
                }
            }

            // Non-configured playlists
            var allPlaylistNames = GetAllPlaylists().Select(p => p.Name).ToList();
            var nonConfiguredPlaylists = allPlaylistNames
                .Except(config.PlaylistConfig.Keys)
                .ToList();
            
            if (!string.IsNullOrEmpty(filterText))
            {
                var searchTerms = filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                nonConfiguredPlaylists = nonConfiguredPlaylists
                    .Where(p => searchTerms.All(term => p.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
            }
            nonConfiguredPlaylists = nonConfiguredPlaylists.OrderBy(p => p).ToList();

            if (nonConfiguredPlaylists.Any())
            {
                AddSectionHeader("Unsorted Playlists (using default)");
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
            var control = new PlaylistConfigControl(playlistName, displayName, ordersConfig);
            control.Width = playlistPanel.ClientSize.Width - control.Margin.Horizontal;
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
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(3, 15, 3, 5)
            };
            label.Size = new Size(playlistPanel.ClientSize.Width - label.Margin.Horizontal, 30);
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

            using (var orderConfigForm = new OrderConfigForm(currentOrderConfig, playlistName, this.config))
            {
                if (orderConfigForm.ShowDialog(this) == DialogResult.OK)
                {
                    var newOrders = orderConfigForm.GetOrderConfig().Orders;

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

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            PopulatePlaylists();
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            // On initial load with a custom ClientSize, the Anchor properties are not yet applied.
            // We must manually calculate and set the final positions and sizes of anchored controls.

            // --- 1. Reposition Bottom-Anchored Buttons ---
            // In the designer, the form was 450px high and the buttons had Top=415.
            // The distance from the TOP of the buttons to the BOTTOM of the form is 450 - 415 = 35px.
            // We preserve this distance for our bottom-anchored (but not top-anchored) buttons.
            const int buttonTopToBottomMargin = 35;
            int newButtonTop = this.ClientSize.Height - buttonTopToBottomMargin;
            okButton.Top = newButtonTop;
            cancelButton.Top = newButtonTop;

            // --- 2. Resize the Main Panel ---
            // The panel is anchored to all four sides. Its height must stretch to fill the space.
            // In the designer, panel Top=40 and its Bottom=399 (40+359).
            // The distance from the panel's BOTTOM to the form's BOTTOM is 450 - 399 = 51px.
            // We preserve this margin.
            const int panelBottomMargin = 51;
            this.playlistPanel.Height = this.ClientSize.Height - this.playlistPanel.Top - panelBottomMargin;

            // --- 3. Lock Form Width ---
            // This prevents horizontal resizing while allowing vertical resizing.
            this.MinimumSize = new Size(this.Width, this.MinimumSize.Height);
            this.MaximumSize = new Size(this.Width, Screen.PrimaryScreen.WorkingArea.Height);
        }

        private void PlaylistPanel_Resize(object sender, EventArgs e)
        {
            playlistPanel.SuspendLayout();
            foreach (Control control in playlistPanel.Controls)
            {
                control.Width = playlistPanel.ClientSize.Width - control.Margin.Horizontal;
            }
            playlistPanel.ResumeLayout();
        }

        private void ConfigForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.ActiveControl is TextBox && this.ActiveControl != searchTextBox)
                return;

            if ((e.Control && e.KeyCode == Keys.F) || (e.Alt && e.KeyCode == Keys.D))
            {
                searchTextBox.Focus();
                searchTextBox.SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var firstResult = playlistPanel.Controls.OfType<PlaylistConfigControl>().FirstOrDefault();
                if (firstResult != null)
                {
                    OnConfigureClicked(firstResult, EventArgs.Empty);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }
    }
}
