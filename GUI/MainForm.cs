using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MD.GUI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the main workspace for the form.
        /// </summary>
        public Workspace Workspace
        {
            get
            {
                return this._Workspace;
            }
        }
    }
}
