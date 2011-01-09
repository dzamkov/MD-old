namespace MD.GUI
{
    partial class Workspace
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
            this._ScrollBar = new System.Windows.Forms.VScrollBar();
            this.SuspendLayout();
            // 
            // _ScrollBar
            // 
            this._ScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
            this._ScrollBar.Location = new System.Drawing.Point(133, 0);
            this._ScrollBar.Name = "_ScrollBar";
            this._ScrollBar.Size = new System.Drawing.Size(17, 150);
            this._ScrollBar.TabIndex = 0;
            // 
            // Workspace
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this._ScrollBar);
            this.Name = "Workspace";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.VScrollBar _ScrollBar;


    }
}
