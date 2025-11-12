using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public class OrderItemControl : UserControl
    {
        private ComboBox _orderTypeComboBox;
        private CheckBox _descendingCheckBox;
        private Button _removeButton;
        private Button _moveUpButton;
        private Button _moveDownButton;

        public event EventHandler RemoveClicked;
        public event EventHandler MoveUpClicked;
        public event EventHandler MoveDownClicked;

        public (string Order, bool Descending) OrderItem
        {
            get => (_orderTypeComboBox.SelectedItem?.ToString() ?? "", _descendingCheckBox.Checked);
            set
            {
                _orderTypeComboBox.SelectedItem = value.Order;
                _descendingCheckBox.Checked = value.Descending;
            }
        }

        public OrderItemControl(List<string> orderTypes)
        {
            InitializeComponent();
            _orderTypeComboBox.DataSource = new List<string>(orderTypes); // Use a copy
        }

        public void SetMoveButtonsEnabled(bool canMoveUp, bool canMoveDown)
        {
            _moveUpButton.Enabled = canMoveUp;
            _moveDownButton.Enabled = canMoveDown;
        }

        private void InitializeComponent()
        {
            this._orderTypeComboBox = new ComboBox();
            this._descendingCheckBox = new CheckBox();
            this._removeButton = new Button();
            this._moveUpButton = new Button();
            this._moveDownButton = new Button();
            this.SuspendLayout();
            // 
            // OrderItemControl
            // 
            this.Size = new Size(560, 45);
            this.Margin = new Padding(3, 3, 3, 0);
            this.BackColor = Theme.FormBackColor;
            this.BorderStyle = BorderStyle.None;
            // 
            // _orderTypeComboBox
            // 
            this._orderTypeComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this._orderTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this._orderTypeComboBox.FormattingEnabled = true;
            this._orderTypeComboBox.Location = new Point(10, 12);
            this._orderTypeComboBox.Name = "_orderTypeComboBox";
            this._orderTypeComboBox.Size = new Size(300, 21);
            this._orderTypeComboBox.TabIndex = 0;
            // 
            // _descendingCheckBox
            // 
            this._descendingCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this._descendingCheckBox.AutoSize = true;
            this._descendingCheckBox.Location = new Point(328, 14);
            this._descendingCheckBox.Name = "_descendingCheckBox";
            this._descendingCheckBox.Size = new Size(83, 17);
            this._descendingCheckBox.TabIndex = 1;
            this._descendingCheckBox.Text = "Descending";
            this._descendingCheckBox.UseVisualStyleBackColor = true;
            // 
            // _removeButton
            // 
            this._removeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this._removeButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this._removeButton.ForeColor = Color.Red;
            this._removeButton.Location = new Point(518, 7);
            this._removeButton.Name = "_removeButton";
            this._removeButton.Size = new Size(30, 30);
            this._removeButton.TabIndex = 4;
            this._removeButton.Text = "✕";
            this._removeButton.UseVisualStyleBackColor = true;
            this._removeButton.FlatStyle = FlatStyle.Flat;
            this._removeButton.FlatAppearance.BorderSize = 0;
            this._removeButton.Click += (s, e) => RemoveClicked?.Invoke(this, EventArgs.Empty);
            // 
            // _moveDownButton
            // 
            this._moveDownButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this._moveDownButton.Location = new Point(482, 7);
            this._moveDownButton.Name = "_moveDownButton";
            this._moveDownButton.Size = new Size(30, 30);
            this._moveDownButton.TabIndex = 3;
            this._moveDownButton.Text = "▼";
            this._moveDownButton.UseVisualStyleBackColor = true;
            this._moveDownButton.Click += (s, e) => MoveDownClicked?.Invoke(this, EventArgs.Empty);
            // 
            // _moveUpButton
            // 
            this._moveUpButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this._moveUpButton.Location = new Point(446, 7);
            this._moveUpButton.Name = "_moveUpButton";
            this._moveUpButton.Size = new Size(30, 30);
            this._moveUpButton.TabIndex = 2;
            this._moveUpButton.Text = "▲";
            this._moveUpButton.UseVisualStyleBackColor = true;
            this._moveUpButton.Click += (s, e) => MoveUpClicked?.Invoke(this, EventArgs.Empty);
            //
            // Add controls
            //
            this.Controls.Add(this._moveUpButton);
            this.Controls.Add(this._moveDownButton);
            this.Controls.Add(this._removeButton);
            this.Controls.Add(this._descendingCheckBox);
            this.Controls.Add(this._orderTypeComboBox);
            this.ResumeLayout(false);
            this.PerformLayout();
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