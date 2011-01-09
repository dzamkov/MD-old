namespace MD.GUI
{
    partial class WorkItem
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._ToolStrip = new System.Windows.Forms.ToolStrip();
            this._Label = new System.Windows.Forms.ToolStripLabel();
            this._ToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // _ToolStrip
            // 
            this._ToolStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            this._ToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._Label});
            this._ToolStrip.Location = new System.Drawing.Point(0, 0);
            this._ToolStrip.Name = "_ToolStrip";
            this._ToolStrip.Size = new System.Drawing.Size(486, ItemHeight);
            this._ToolStrip.TabIndex = 0;
            // 
            // _Label
            // 
            this._Label.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._Label.Name = "_Label";
            this._Label.Size = new System.Drawing.Size(78, 22);
            this._Label.Text = "WorkItem";
            // 
            // WorkItem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this._ToolStrip);
            this.MaximumSize = new System.Drawing.Size(65536, ItemHeight);
            this.MinimumSize = new System.Drawing.Size(ItemHeight, ItemHeight);
            this.Name = "WorkItem";
            this.Size = new System.Drawing.Size(486, ItemHeight);
            this._ToolStrip.ResumeLayout(false);
            this._ToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip _ToolStrip;
        private System.Windows.Forms.ToolStripLabel _Label;

    }
}
