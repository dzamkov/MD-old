namespace MD
{
    partial class MainForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.spectrum1 = new MD.Spectrum();
            this.SuspendLayout();
            // 
            // spectrum1
            // 
            this.spectrum1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spectrum1.Location = new System.Drawing.Point(0, 0);
            this.spectrum1.Name = "spectrum1";
            this.spectrum1.Size = new System.Drawing.Size(609, 550);
            this.spectrum1.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(609, 550);
            this.Controls.Add(this.spectrum1);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.ResumeLayout(false);

        }

        #endregion

        private Spectrum spectrum1;
    }
}