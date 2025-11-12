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
        private Button _clearButton;

        public string PlaylistName { get; }

        public event EventHandler ConfigureClicked;
        public event EventHandler ClearClicked;

        private Color _defaultBackColor;
        private readonly Color _hoverBackColor = Color.FromArgb(229, 243, 255); // Light blue
        private readonly Color _downBackColor = Color.FromArgb(204, 232, 255); // Darker blue

        public PlaylistConfigControl(string playlistName, string displayName, OrdersConfig ordersConfig)
        {
            this.PlaylistName = playlistName;
            InitializeComponent();
            _playlistNameLabel.Text = displayName;
            UpdateDisplay(ordersConfig);
            _defaultBackColor = this.BackColor;

            // Hook events for the control and its labels to make the whole area clickable
            this.Click += Control_Click;
            this.MouseEnter += Control_MouseEnter;
            this.MouseLeave += Control_MouseLeave;
            this.MouseDown += Control_MouseDown;
            this.MouseUp += Control_MouseUp;

            foreach (Control control in this.Controls)
            {
                if (control != _clearButton) // Don't apply these handlers to the clear button
                {
                    control.Click += Control_Click;
                    control.MouseEnter += Control_MouseEnter;
                    control.MouseLeave += Control_MouseLeave;
                    control.MouseDown += Control_MouseDown;
                    control.MouseUp += Control_MouseUp;
                }
            }
        }

        private void InitializeComponent()
        {
            _playlistNameLabel = new Label();
            _orderDisplayLabel = new Label();
            _clearButton = new Button();
            this.SuspendLayout();

            // PlaylistConfigControl
            this.Size = new Size(750, 45);
            this.Margin = new Padding(3, 3, 3, 0);
            this.BackColor = Theme.FormBackColor;
            this.BorderStyle = BorderStyle.None;
            this.Cursor = Cursors.Hand;

            // _playlistNameLabel
            _playlistNameLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            _playlistNameLabel.Location = new Point(10, 12);
            _playlistNameLabel.Size = new Size(250, 20);
            _playlistNameLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _playlistNameLabel.TextAlign = ContentAlignment.MiddleLeft;
            _playlistNameLabel.AutoEllipsis = true;
            _playlistNameLabel.UseMnemonic = false;
            _playlistNameLabel.BackColor = Color.Transparent;

            // _clearButton
            _clearButton.Text = "✕";
            _clearButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _clearButton.ForeColor = Color.Red;
            _clearButton.Size = new Size(30, 30);
            _clearButton.Location = new Point(this.Width - 45, (this.Height - 30) / 2);
            _clearButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _clearButton.FlatStyle = FlatStyle.Flat;
            _clearButton.FlatAppearance.BorderSize = 0;
            _clearButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 230, 230);
            _clearButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(255, 200, 200);
            _clearButton.Click += (s, e) => ClearClicked?.Invoke(this, EventArgs.Empty);
            _clearButton.Cursor = Cursors.Default;

            // _orderDisplayLabel
            _orderDisplayLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            _orderDisplayLabel.ForeColor = SystemColors.ControlDarkDark;
            _orderDisplayLabel.Location = new Point(265, 13);
            _orderDisplayLabel.Size = new Size(this.Width - 265 - 55, 20); // Dynamic width
            _orderDisplayLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _orderDisplayLabel.TextAlign = ContentAlignment.MiddleLeft;
            _orderDisplayLabel.AutoEllipsis = true;
            _orderDisplayLabel.BackColor = Color.Transparent;

            this.Controls.Add(_playlistNameLabel);
            this.Controls.Add(_orderDisplayLabel);
            this.Controls.Add(_clearButton);

            this.ResumeLayout(false);
        }
        
        private void Control_MouseEnter(object sender, EventArgs e)
        {
            SetBackColor(this._hoverBackColor);
        }

        private void Control_MouseLeave(object sender, EventArgs e)
        {
            SetBackColor(this._defaultBackColor);
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                SetBackColor(this._downBackColor);
            }
        }

        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.ClientRectangle.Contains(this.PointToClient(Cursor.Position)))
            {
                SetBackColor(this._hoverBackColor);
            }
            else
            {
                SetBackColor(this._defaultBackColor);
            }
        }

        private void Control_Click(object sender, EventArgs e)
        {
            ConfigureClicked?.Invoke(this, EventArgs.Empty);
        }

        private void SetBackColor(Color color)
        {
            this.BackColor = color;
            _playlistNameLabel.BackColor = color;
            _orderDisplayLabel.BackColor = color;
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
                _orderDisplayLabel.Text = "";
                _clearButton.Visible = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Theme.ListItemSeparatorColor, 1))
            {
                e.Graphics.DrawLine(pen, 0, this.Height - 1, this.Width, this.Height - 1);
            }
        }
    }
}