﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MusicBeePlugin;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public partial class OrderConfigForm : Form
    {
        private List<(string Order, bool Descending)> orderConfig;
        private DataGridView orderGrid;
        private Button okButton;
        private Button cancelButton;
        private bool gridHasFocus = false; // Track if grid has focus after first click

        public OrderConfigForm(List<(string Order, bool Descending)> config, string playlistName = null)
        {
            orderConfig = new List<(string Order, bool Descending)>(config); // Create a copy to work on
            InitializeComponent();
            PopulateGrid();
            this.Activated += OrderConfigForm_Activated; // Ensure focus on form activation
            this.orderGrid.MouseClick += OrderGrid_MouseClick; // Handle first click on grid
        }

        private void InitializeComponent()
        {
            this.orderGrid = new DataGridView();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.SuspendLayout();
            //
            // orderGrid
            //
            this.orderGrid.AllowUserToAddRows = true;
            this.orderGrid.AllowUserToDeleteRows = true;
            this.orderGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.orderGrid.Dock = DockStyle.Top;
            this.orderGrid.Location = new Point(10, 10);
            this.orderGrid.Name = "orderGrid";
            this.orderGrid.Size = new Size(580, 250);
            this.orderGrid.TabIndex = 0;
            this.orderGrid.CellContentClick += OrderGrid_CellContentClick;
            this.orderGrid.EditMode = DataGridViewEditMode.EditOnEnter; // Try EditOnEnter mode
            //
            // okButton
            //

            this.okButton.Location = new Point(434, 270);
            this.okButton.Name = "okButton";
            this.okButton.Size = new Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new EventHandler(this.OkButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Location = new Point(515, 270);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new EventHandler(this.CancelButton_Click);
            //
            // OrderConfigForm
            //
            this.ClientSize = new Size(600, 300);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.orderGrid);
            this.StartPosition = FormStartPosition.CenterParent; // Center form on parent
            this.KeyPreview = true; // Need to set KeyPreview to true to capture key events for the form
            this.Name = "OrderConfigForm";
            this.Text = "Configure Playlist Order";
            this.ResumeLayout(false);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                OkButton_Click(this, EventArgs.Empty); // Treat Escape as OK
                return true; // Indicate that we handled this key
            }
            return base.ProcessCmdKey(ref msg, keyData); // Let base class handle other keys
        }


        private void PopulateGrid()
        {
            orderGrid.Columns.Clear();
            orderGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "OrderType", HeaderText = "Order By", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            orderGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Descending", HeaderText = "Descending", Width = 100 });
            orderGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "Delete", UseColumnTextForButtonValue = true, Width = 50 });

            DataGridViewComboBoxColumn orderTypeColumn = (DataGridViewComboBoxColumn)orderGrid.Columns["OrderType"];
            var orderTypes = new List<string> { "ManualOrder" }
                .Concat(Enum.GetNames(typeof(FilePropertyType)))
                .Concat(Enum.GetNames(typeof(MetaDataType)))
                .ToList();
            orderTypeColumn.DataSource = orderTypes;
            orderTypeColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            orderTypeColumn.DisplayStyleForCurrentCellOnly = false; // Ensure dropdown button is always visible

            orderGrid.Rows.Clear();
            foreach (var order in orderConfig)
            {
                int rowIndex = orderGrid.Rows.Add();
                orderGrid.Rows[rowIndex].Cells["OrderType"].Value = order.Order;
                orderGrid.Rows[rowIndex].Cells["Descending"].Value = order.Descending;
            }
            orderGrid.AllowUserToAddRows = true; // Allow adding new rows after initial population
        }


        private void OrderGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == orderGrid.Columns["Delete"].Index && e.RowIndex >= 0 && e.RowIndex < orderGrid.Rows.Count - 1)
            {
                orderGrid.Rows.RemoveAt(e.RowIndex);
            }
        }

        public List<(string Order, bool Descending)> GetOrderConfig()
        {
            orderConfig.Clear();
            foreach (DataGridViewRow row in orderGrid.Rows)
            {
                if (row.IsNewRow) continue;

                string orderType = row.Cells["OrderType"].Value as string;
                bool descending = row.Cells["Descending"].Value as bool? ?? false; // default to false if null

                if (!string.IsNullOrEmpty(orderType))
                {
                    orderConfig.Add((orderType, descending));
                }
            }
            return orderConfig;
        }


        private bool ValidateOrderConfig()
        {
            var orders = GetOrderConfig();
            
            int manualOrderCount = orders.Count(o => o.Order == "ManualOrder");
            bool hasOtherOrders = orders.Any(o => o.Order != "ManualOrder");
            
            if (manualOrderCount > 1)
            {
                MessageBox.Show("Multiple ManualOrder entries are not allowed",
                    "Invalid Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            
            if (manualOrderCount == 1 && hasOtherOrders)
            {
                MessageBox.Show("ManualOrder cannot be combined with other sort orders", 
                    "Invalid Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            
            return true;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (!ValidateOrderConfig())
                return;
            
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OrderConfigForm_Activated(object sender, EventArgs e)
        {
            if (!gridHasFocus)
            {
                this.orderGrid.Focus(); // Set focus to grid on form activation
                gridHasFocus = true;
            }
        }

        private void OrderGrid_MouseClick(object sender, MouseEventArgs e)
        {
            if (!gridHasFocus)
            {
                this.orderGrid.Focus(); // Ensure grid gets focus on first click within grid area
                gridHasFocus = true;
            }
        }

    }
}
