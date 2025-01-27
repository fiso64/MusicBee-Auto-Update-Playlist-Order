using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public partial class ConfigForm : Form
    {
        private MusicBeeApiInterface mbApi;
        private Dictionary<string, List<(string Order, bool Descending)>> playlistConfig;
        private DataGridView playlistGrid;
        private Button okButton;
        private Button cancelButton;
        private bool gridHasFocus = false; // Track if grid has focus after first click

        public ConfigForm(MusicBeeApiInterface api, Dictionary<string, List<(string Order, bool Descending)>> config)
        {
            mbApi = api;
            playlistConfig = new Dictionary<string, List<(string Order, bool Descending)>>(config); // Create a copy to work on
            InitializeComponent();
            PopulateGrid();
            this.Activated += ConfigForm_Activated; // Ensure focus on form activation
            this.playlistGrid.MouseClick += PlaylistGrid_MouseClick; // Handle first click on grid
        }

        private void InitializeComponent()
        {
            this.playlistGrid = new DataGridView();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.SuspendLayout();
            //
            // playlistGrid
            //
            this.playlistGrid.AllowUserToAddRows = true;
            this.playlistGrid.AllowUserToDeleteRows = true;
            this.playlistGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.playlistGrid.Dock = DockStyle.Top;
            this.playlistGrid.Location = new Point(10, 10);
            this.playlistGrid.Name = "playlistGrid";
            this.playlistGrid.Size = new Size(780, 300);
            this.playlistGrid.TabIndex = 0;
            this.playlistGrid.CellContentClick += PlaylistGrid_CellContentClick;
            this.playlistGrid.CellBeginEdit += PlaylistGrid_CellBeginEdit;
            this.playlistGrid.CellEndEdit += PlaylistGrid_CellEndEdit;
            this.playlistGrid.UserDeletingRow += PlaylistGrid_UserDeletingRow;
            this.playlistGrid.DataError += PlaylistGrid_DataError;
            this.playlistGrid.EditMode = DataGridViewEditMode.EditOnEnter; // Try EditOnEnter mode
            //
            // okButton
            //
            this.okButton.Location = new Point(634, 320);
            this.okButton.Name = "okButton";
            this.okButton.Size = new Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new EventHandler(this.OkButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Location = new Point(715, 320);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new EventHandler(this.CancelButton_Click);
            //
            // ConfigForm
            //
            this.ClientSize = new Size(800, 350);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.playlistGrid);
            this.StartPosition = FormStartPosition.CenterParent; // Center form on parent
            this.KeyPreview = true; // Need to set KeyPreview to true to capture key events for the form
            this.Name = "ConfigForm";
            this.Text = "Auto Update Playlist Order Configuration";
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

        private void PopulateGrid()
        {
            playlistGrid.Columns.Clear();
            playlistGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "PlaylistName", HeaderText = "Playlist Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            playlistGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderDisplay", HeaderText = "Order Configuration", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true }); // New text column
            playlistGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Order", HeaderText = "Order", Text = "Configure", UseColumnTextForButtonValue = true, Width = 100 }); // Configure button, always "Configure" text
            playlistGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "Delete", UseColumnTextForButtonValue = true, Width = 50 });

            DataGridViewComboBoxColumn playlistColumn = (DataGridViewComboBoxColumn)playlistGrid.Columns["PlaylistName"];
            playlistColumn.DataSource = GetAllPlaylists().Select(p => p.Name).ToList();
            playlistColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            playlistColumn.DisplayStyleForCurrentCellOnly = false; // Ensure dropdown button is always visible

            playlistGrid.Rows.Clear();
            foreach (var playlistName in playlistConfig.Keys)
            {
                int rowIndex = playlistGrid.Rows.Add();
                playlistGrid.Rows[rowIndex].Cells["PlaylistName"].Value = playlistName;
                UpdateOrderDisplayCell(rowIndex, playlistName); // Populate the new text column
            }
            playlistGrid.AllowUserToAddRows = true; // Allow adding new rows after initial population
        }

        private void UpdateOrderDisplayCell(int rowIndex, string playlistName)
        {
            if (playlistConfig.TryGetValue(playlistName, out var orders) && orders.Count > 0)
            {
                playlistGrid.Rows[rowIndex].Cells["OrderDisplay"].Value = string.Join(", ", orders.Select(o => $"{o.Order}{(o.Descending ? " (desc)" : "")}"));
            }
            else
            {
                playlistGrid.Rows[rowIndex].Cells["OrderDisplay"].Value = "Not Configured";
            }
        }


        private void PlaylistGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == playlistGrid.Columns["Order"].Index && e.RowIndex >= 0 && e.RowIndex < playlistGrid.Rows.Count - 1)
            {
                string playlistName = playlistGrid.Rows[e.RowIndex].Cells["PlaylistName"].Value as string;
                if (playlistName == null) return;

                List<(string Order, bool Descending)> currentOrderConfig = playlistConfig.ContainsKey(playlistName) ? playlistConfig[playlistName] : new List<(string Order, bool Descending)>();
                using (var orderConfigForm = new OrderConfigForm(currentOrderConfig))
                {
                    if (orderConfigForm.ShowDialog() == DialogResult.OK)
                    {
                        playlistConfig[playlistName] = orderConfigForm.GetOrderConfig();
                        UpdateOrderDisplayCell(e.RowIndex, playlistName); // Update the text column after config changes
                    }
                }
            }
            else if (e.ColumnIndex == playlistGrid.Columns["Delete"].Index && e.RowIndex >= 0 && e.RowIndex < playlistGrid.Rows.Count - 1)
            {
                string playlistName = playlistGrid.Rows[e.RowIndex].Cells["PlaylistName"].Value as string;
                if (playlistName != null)
                {
                    playlistConfig.Remove(playlistName);
                    playlistGrid.Rows.RemoveAt(e.RowIndex);
                }
            }
        }

        public Dictionary<string, List<(string Order, bool Descending)>> GetPlaylistConfig()
        {
            var configToSave = new Dictionary<string, List<(string Order, bool Descending)>>();
            foreach (var pair in playlistConfig)
            {
                if (pair.Value != null && pair.Value.Count > 0) // Only include if configuration is not empty
                {
                    configToSave.Add(pair.Key, pair.Value);
                }
            }
            return configToSave;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void PlaylistGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.ColumnIndex == playlistGrid.Columns["PlaylistName"].Index)
            {
                DataGridViewComboBoxColumn playlistColumn = (DataGridViewComboBoxColumn)playlistGrid.Columns["PlaylistName"];
                playlistColumn.DataSource = GetAllPlaylists().Select(p => p.Name).ToList();
            }
        }

        private void PlaylistGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == playlistGrid.Columns["PlaylistName"].Index && e.RowIndex >= 0 && e.RowIndex < playlistGrid.Rows.Count - 1)
            {
                string newPlaylistName = playlistGrid.Rows[e.RowIndex].Cells["PlaylistName"].Value as string;
                if (string.IsNullOrEmpty(newPlaylistName)) return;

                // Check for duplicates (excluding the current row being edited)
                foreach (DataGridViewRow row in playlistGrid.Rows)
                {
                    if (row.Index != e.RowIndex && !row.IsNewRow && row.Cells["PlaylistName"].Value as string == newPlaylistName)
                    {
                        MessageBox.Show($"Playlist '{newPlaylistName}' is already configured.", "Duplicate Playlist", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        playlistGrid.Rows[e.RowIndex].Cells["PlaylistName"].Value = playlistGrid.Rows[e.RowIndex - 1].Cells["PlaylistName"].Value; // Revert to previous, or clear if new row
                        return; // Exit without adding/updating
                    }
                }


                if (newPlaylistName != null)
                {
                    if (!playlistConfig.ContainsKey(newPlaylistName))
                    {
                        playlistConfig[newPlaylistName] = new List<(string Order, bool Descending)>();
                    }
                    UpdateOrderDisplayCell(e.RowIndex, newPlaylistName); // Update the text column after playlist name change
                }
            }
        }

        private void PlaylistGrid_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (e.Row.Cells["PlaylistName"].Value is string playlistName)
            {
                playlistConfig.Remove(playlistName);
            }
        }


        private void PlaylistGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Exception != null)
            {
                // Handle or ignore the error, prevents crash on combobox invalid value.
                e.ThrowException = false; // Prevent exception from being thrown.
                e.Cancel = false; // Do not cancel the cell edit.
            }
        }

        private void ConfigForm_Activated(object sender, EventArgs e)
        {
            if (!gridHasFocus)
            {
                this.playlistGrid.Focus();
                gridHasFocus = true;
            }
        }

        private void PlaylistGrid_MouseClick(object sender, MouseEventArgs e)
        {
            if (!gridHasFocus)
            {
                this.playlistGrid.Focus();
                gridHasFocus = true;
            }
        }
    }
}