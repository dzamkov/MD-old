using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using Mp3Sharp;

using MD.GUI;

namespace MD
{
    /// <summary>
    /// Main program interface.
    /// </summary>
    public static class Program
    {

        /// <summary>
        /// Program main entry point.
        /// </summary>
        [STAThread]
        public static void Main(string[] Args)
        {
            MainWindow mw = new MainWindow();
            mw.Run(60.0);
        }
    }
}