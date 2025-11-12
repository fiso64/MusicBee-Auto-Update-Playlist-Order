using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public class PlaylistConfigControl : UserControl
    {
        private readonly ComboBox _playlistComboBox;
        private readonly Label _orderDisplayLabel;
        private readonly Button _configureButton;
        private readonly Button _removeButton;

        public event EventHandler RemoveClicked;
        public event EventHandler PlaylistChanged;
        public event EventHandler ConfigureClicked;

        public string SelectedPlaylist
        {
            get => _playlistComboBox.SelectedItem as string;
            set => _playlistComboBox.SelectedItem = value;
        }

        public string OldPlaylistName { get; set; }

        public string OrderDisplay
        {
            set => _orderDisplayLabel.Text = value;
        }

        public PlaylistConfigControl(string selectedPlaylist, string orderDisplay)
        {
            // UserControl properties
            this.Margin = new Padding(3);
            this.Padding = new Padding(5);
            this.Size = new Size(750, 45);
            this.BorderStyle = BorderStyle.FixedSingle;

            // Playlist ComboBox
            _playlistComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(10, 10),
                Size = new Size(200, 21),
                TabIndex = 0
            };
            _playlistComboBox.SelectionChangeCommitted += (s, e) => PlaylistChanged?.Invoke(this, EventArgs.Empty);
            this.Controls.Add(_playlistComboBox);

            OldPlaylistName = selectedPlaylist;

            // Order Display Label
            _orderDisplayLabel = new Label
            {
                Text = orderDisplay,
                Location = new Point(220, 10),
                Size = new Size(390, 23),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = SystemColors.ControlLightLight,
                TabIndex = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(_orderDisplayLabel);

            // Configure Button
            _configureButton = new Button
            {
                Text = "Configure",
                Location = new Point(620, 9),
                Size = new Size(75, 23),
                TabIndex = 2,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _configureButton.Click += (s, e) => ConfigureClicked?.Invoke(this, EventArgs.Empty);
            this.Controls.Add(_configureButton);

            // Remove Button
            _removeButton = new Button
            {
                Text = "X",
                Location = new Point(705, 9),
                Size = new Size(23, 23),
                ForeColor = Color.Red,
                Font = new Font(this.Font, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                TabIndex = 3,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _removeButton.FlatAppearance.BorderSize = 0;
            _removeButton.Click += (s, e) => RemoveClicked?.Invoke(this, EventArgs.Empty);
            this.Controls.Add(_removeButton);
        }

        public void SetPlaylistSource(List<string> playlistDataSource)
        {
            string selectedPlaylist = this.OldPlaylistName;
            var currentPlaylists = new List<string>(playlistDataSource);
            
            if (!currentPlaylists.Contains(selectedPlaylist))
            {
                currentPlaylists.Add(selectedPlaylist);
            }

            _playlistComboBox.DataSource = currentPlaylists;
            _playlistComboBox.SelectedItem = selectedPlaylist;
        }
    }
}