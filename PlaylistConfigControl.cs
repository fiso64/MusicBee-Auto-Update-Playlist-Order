using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public class PlaylistConfigControl : UserControl
    {
        private Label _playlistNameLabel;
        private Label _orderDisplayLabel;
        private Button _configureButton;
        private Button _clearButton;

        public string PlaylistName { get; }

        public event EventHandler ConfigureClicked;
        public event EventHandler ClearClicked;

        public PlaylistConfigControl(string playlistName, string displayName, OrdersConfig ordersConfig)
        {
            this.PlaylistName = playlistName;
            InitializeComponent();
            _playlistNameLabel.Text = displayName;
            UpdateDisplay(ordersConfig);
        }

        private void InitializeComponent()
        {
            _playlistNameLabel = new Label();
            _orderDisplayLabel = new Label();
            _configureButton = new Button();
            _clearButton = new Button();
            this.SuspendLayout();

            // PlaylistConfigControl
            this.Size = new Size(750, 45);
            this.Margin = new Padding(3, 3, 3, 0);
            this.BackColor = SystemColors.Control;
            this.BorderStyle = BorderStyle.FixedSingle;

            // _playlistNameLabel
            _playlistNameLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            _playlistNameLabel.Location = new Point(10, 12);
            _playlistNameLabel.Size = new Size(250, 20);
            _playlistNameLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _playlistNameLabel.TextAlign = ContentAlignment.MiddleLeft;
            _playlistNameLabel.AutoEllipsis = true;
            _playlistNameLabel.UseMnemonic = false;

            // _configureButton
            _configureButton.Text = "Configure";
            _configureButton.Location = new Point(this.Width - 180, 10);
            _configureButton.Size = new Size(80, 25);
            _configureButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _configureButton.Click += (s, e) => ConfigureClicked?.Invoke(this, EventArgs.Empty);
            _configureButton.FlatStyle = FlatStyle.System;

            // _clearButton
            _clearButton.Text = "Clear";
            _clearButton.Location = new Point(this.Width - 90, 10);
            _clearButton.Size = new Size(80, 25);
            _clearButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _clearButton.Click += (s, e) => ClearClicked?.Invoke(this, EventArgs.Empty);
            _clearButton.FlatStyle = FlatStyle.System;

            // _orderDisplayLabel
            _orderDisplayLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _orderDisplayLabel.ForeColor = SystemColors.ControlDarkDark;
            _orderDisplayLabel.Location = new Point(265, 13);
            _orderDisplayLabel.Size = new Size(this.Width - 265 - 185, 20); // Dynamic width
            _orderDisplayLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _orderDisplayLabel.TextAlign = ContentAlignment.MiddleLeft;
            _orderDisplayLabel.AutoEllipsis = true;

            this.Controls.Add(_playlistNameLabel);
            this.Controls.Add(_orderDisplayLabel);
            this.Controls.Add(_configureButton);
            this.Controls.Add(_clearButton);

            this.ResumeLayout(false);
        }

        public void UpdateDisplay(OrdersConfig ordersConfig)
        {
            bool isConfigured = ordersConfig != null && ordersConfig.Orders.Any();
            if (isConfigured)
            {
                _orderDisplayLabel.Text = ordersConfig.ToString();
                _clearButton.Visible = true;
            }
            else
            {
                _orderDisplayLabel.Text = "Using default sort order";
                _clearButton.Visible = false;
            }
        }
    }
}