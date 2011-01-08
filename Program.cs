using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using Mp3Sharp;

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
            string file = "";
            using(OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Filter = "MP3 Files |*.mp3";
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    file = fd.FileName;
                }
                else
                {
                    return;
                }
            }

            Thread t = new Thread(delegate()
            {
                AudioFeed.Play(new MP3AudioFeed(file).Copy(4096, 65536 * 4).Play, 4096, 2);
            });
            t.IsBackground = true;
            t.Start();

            while (true)
            {
                Application.DoEvents();
            }
        }
    }
}