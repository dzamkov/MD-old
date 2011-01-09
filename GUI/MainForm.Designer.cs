namespace MD.GUI
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
            this.MainSplitter = new System.Windows.Forms.SplitContainer();
            this._Workspace = new MD.GUI.Workspace();
            this.MainMenu = new System.Windows.Forms.MenuStrip();
            this.MainSplitter.Panel1.SuspendLayout();
            this.MainSplitter.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainSplitter
            // 
            this.MainSplitter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.MainSplitter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainSplitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.MainSplitter.Location = new System.Drawing.Point(0, 24);
            this.MainSplitter.Name = "MainSplitter";
            // 
            // MainSplitter.Panel1
            // 
            this.MainSplitter.Panel1.Controls.Add(this._Workspace);
            this.MainSplitter.Size = new System.Drawing.Size(609, 526);
            this.MainSplitter.SplitterDistance = 260;
            this.MainSplitter.TabIndex = 0;
            // 
            // _Workspace
            // 
            this._Workspace.BackColor = System.Drawing.Color.White;
            this._Workspace.Dock = System.Windows.Forms.DockStyle.Fill;
            this._Workspace.Location = new System.Drawing.Point(0, 0);
            this._Workspace.Name = "_Workspace";
            this._Workspace.Size = new System.Drawing.Size(258, 524);
            this._Workspace.TabIndex = 0;
            // 
            // MainMenu
            // 
            this.MainMenu.Location = new System.Drawing.Point(0, 0);
            this.MainMenu.Name = "MainMenu";
            this.MainMenu.Size = new System.Drawing.Size(609, 24);
            this.MainMenu.TabIndex = 1;
            this.MainMenu.Text = "menuStrip1";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(609, 550);
            this.Controls.Add(this.MainSplitter);
            this.Controls.Add(this.MainMenu);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.MainSplitter.Panel1.ResumeLayout(false);
            this.MainSplitter.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer MainSplitter;
        private System.Windows.Forms.MenuStrip MainMenu;
        private MD.GUI.Workspace _Workspace;


    }
}