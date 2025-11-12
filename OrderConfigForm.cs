using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static MusicBeePlugin.Plugin;

namespace MusicBeePlugin
{
    public partial class OrderConfigForm : Form
    {
        private List<(string Order, bool Descending)> orderConfig;
        private List<string> orderTypes;
        private DoubleBufferedFlowLayoutPanel orderPanel;
        private Button addButton;
        private Panel buttonContainer;
        private Button okButton;
        private Button cancelButton;

        public OrderConfigForm(List<(string Order, bool Descending)> config, string playlistName = null)
        {
            orderConfig = new List<(string Order, bool Descending)>(config); // Create a copy to work on
            InitializeComponent();
            this.Text = string.IsNullOrEmpty(playlistName) 
                ? "Configure Playlist Order" 
                : $"Configure Order for '{playlistName}'";
            PopulateOrderItems();
        }

        private void InitializeComponent()
        {
            this.orderPanel = new DoubleBufferedFlowLayoutPanel();
            this.addButton = new Button();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.SuspendLayout();
            // 
            // orderPanel
            // 
            this.orderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.orderPanel.AutoScroll = true;
            this.orderPanel.Location = new Point(12, 12);
            this.orderPanel.Name = "orderPanel";
            this.orderPanel.Size = new Size(576, 238);
            this.orderPanel.TabIndex = 0;
            this.orderPanel.FlowDirection = FlowDirection.TopDown;
            this.orderPanel.WrapContents = false;
            this.orderPanel.Resize += new EventHandler(this.OrderPanel_Resize);
            this.buttonContainer = new Panel();
            //
            // buttonContainer
            //
            this.buttonContainer.Margin = new Padding(0, 5, 0, 0);
            this.buttonContainer.Name = "buttonContainer";
            this.buttonContainer.Size = new Size(576, 40);
            // 
            // addButton
            // 
            this.addButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.addButton.Name = "addButton";
            this.addButton.Size = new Size(100, 30);
            this.addButton.TabIndex = 0;
            this.addButton.Text = "+ Add Rule";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new EventHandler(this.AddButton_Click);
            // Calculate location based on parent container size to avoid being pushed off-screen
            this.addButton.Location = new Point(this.buttonContainer.ClientSize.Width - this.addButton.Width - 3, 5);
            
            this.buttonContainer.Controls.Add(this.addButton);
            // 
            // okButton
            // 
            this.okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.okButton.Location = new Point(432, 265);
            this.okButton.Name = "okButton";
            this.okButton.Size = new Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new EventHandler(this.OkButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.cancelButton.DialogResult = DialogResult.Cancel;
            this.cancelButton.Location = new Point(513, 265);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new EventHandler(this.CancelButton_Click);
            // 
            // OrderConfigForm
            // 
            this.AcceptButton = this.okButton;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new Size(600, 300);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.orderPanel);
            this.KeyPreview = true;
            this.MinimumSize = new Size(500, 250);
            this.Name = "OrderConfigForm";
            this.Text = "Configure Playlist Order";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Load += new System.EventHandler(this.OrderConfigForm_Load);
            this.ResumeLayout(false);
        }

        private void OrderConfigForm_Load(object sender, EventArgs e)
        {
            // This prevents horizontal resizing while allowing vertical resizing.
            this.MinimumSize = new Size(this.Width, this.MinimumSize.Height);
            this.MaximumSize = new Size(this.Width, Screen.PrimaryScreen.WorkingArea.Height);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.CancelButton_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void PopulateOrderItems()
        {
            orderPanel.SuspendLayout();
            orderPanel.Controls.Clear();

            if (orderTypes == null)
            {
                orderTypes = new List<string> { "ManualOrder" }
                    .Concat(Enum.GetNames(typeof(FilePropertyType)))
                    .Concat(Enum.GetNames(typeof(MetaDataType)))
                    .OrderBy(x => x)
                    .ToList();
            }

            foreach (var order in orderConfig)
            {
                orderPanel.Controls.Add(CreateAndWireUpNewControl(order));
            }

            buttonContainer.Width = orderPanel.ClientSize.Width - buttonContainer.Margin.Horizontal;
            orderPanel.Controls.Add(buttonContainer);

            UpdateMoveButtonsState();
            orderPanel.ResumeLayout();
        }
        
        private OrderItemControl CreateAndWireUpNewControl((string Order, bool Descending) orderItem)
        {
            var control = new OrderItemControl(orderTypes)
            {
                OrderItem = orderItem
            };
            control.Width = orderPanel.ClientSize.Width - control.Margin.Horizontal;
            control.RemoveClicked += OrderItem_RemoveClicked;
            control.MoveUpClicked += OrderItem_MoveUpClicked;
            control.MoveDownClicked += OrderItem_MoveDownClicked;
            return control;
        }

        private List<(string Order, bool Descending)> BuildOrderConfigFromUi()
        {
            return orderPanel.Controls.OfType<OrderItemControl>().Select(c => c.OrderItem).ToList();
        }

        public List<(string Order, bool Descending)> GetOrderConfig() => orderConfig;

        private void AddButton_Click(object sender, EventArgs e)
        {
            orderPanel.SuspendLayout();

            var newControl = CreateAndWireUpNewControl(("DateAdded", true));
            int insertIndex = orderPanel.Controls.GetChildIndex(buttonContainer);

            orderPanel.Controls.Add(newControl);
            orderPanel.Controls.SetChildIndex(newControl, insertIndex);

            orderPanel.ResumeLayout(true);

            UpdateMoveButtonsState();
            orderPanel.ScrollControlIntoView(newControl);
        }

        private void OrderItem_RemoveClicked(object sender, EventArgs e)
        {
            var control = sender as OrderItemControl;
            if (control != null)
            {
                orderPanel.SuspendLayout();
                orderPanel.Controls.Remove(control);
                control.Dispose();
                orderPanel.ResumeLayout(true);
                UpdateMoveButtonsState();
            }
        }

        private void OrderItem_MoveUpClicked(object sender, EventArgs e)
        {
            var control = sender as OrderItemControl;
            if (control != null)
            {
                int index = orderPanel.Controls.GetChildIndex(control);
                if (index > 0)
                {
                    orderPanel.Controls.SetChildIndex(control, index - 1);
                    UpdateMoveButtonsState();
                }
            }
        }

        private void OrderItem_MoveDownClicked(object sender, EventArgs e)
        {
            var control = sender as OrderItemControl;
            if (control != null)
            {
                int index = orderPanel.Controls.GetChildIndex(control);
                if (index < orderPanel.Controls.Count - 1)
                {
                    orderPanel.Controls.SetChildIndex(control, index + 1);
                    UpdateMoveButtonsState();
                }
            }
        }

        private void UpdateMoveButtonsState()
        {
            var controls = orderPanel.Controls.OfType<OrderItemControl>().ToList();
            for (int i = 0; i < controls.Count; i++)
            {
                controls[i].SetMoveButtonsEnabled(i > 0, i < controls.Count - 1);
            }
        }

        private void OrderPanel_Resize(object sender, EventArgs e)
        {
            orderPanel.SuspendLayout();
            foreach (Control control in orderPanel.Controls)
            {
                control.Width = orderPanel.ClientSize.Width - control.Margin.Horizontal;
            }
            orderPanel.ResumeLayout();
        }

        private bool ValidateOrderConfig()
        {
            int manualOrderCount = orderConfig.Count(o => o.Order == "ManualOrder");
            bool hasOtherOrders = orderConfig.Any(o => o.Order != "ManualOrder");

            if (manualOrderCount > 1)
            {
                MessageBox.Show("Multiple ManualOrder entries are not allowed.", "Invalid Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (manualOrderCount == 1 && hasOtherOrders)
            {
                MessageBox.Show("ManualOrder cannot be combined with other sort orders.", "Invalid Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            orderConfig = BuildOrderConfigFromUi();
            if (!ValidateOrderConfig())
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}