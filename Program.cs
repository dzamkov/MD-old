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

            AudioContext ac = new AudioContext();
            MP3AudioFeed af = new MP3AudioFeed(file);
            MemoryAudioSource mas = af.Copy(65536 * 4, 65536 * 4);
            af.Reset();
            AudioOutput ao = new AudioOutput(af);
            ao.Play();


            MainForm mf = new MainForm();
            mf.Spectrogram.Source = mas;
            Application.Run(mf);
        }
    }
}