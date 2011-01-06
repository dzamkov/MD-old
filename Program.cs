using System;
using System.Collections.Generic;
using System.Windows.Forms;

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


            Mp3Stream mstr = new Mp3Stream(file);

            AudioContext AC = new AudioContext();

            int s = AL.GenSource();

            ALFormat format = mstr.Format == SoundFormat.Pcm16BitMono ? ALFormat.Mono16 : ALFormat.Stereo16;
            bool playing = false;
            byte[] data = new byte[50000];
            while (true)
            {

                int amount = mstr.Read(data, 0, data.Length);
                Console.WriteLine("WROTE: " + amount.ToString());
                if (mstr.Position == mstr.Length)
                {
                    break;
                }

                int buf = AL.GenBuffer();
                AL.BufferData<byte>(buf, format, data, 50000, mstr.Frequency);
                AL.SourceQueueBuffer(s, buf);

                if (!playing)
                {
                    playing = true;
                    AL.SourcePlay(s);
                }
            }

            while (AL.GetSourceState(s) == ALSourceState.Playing)
            {
                System.Threading.Thread.Sleep(1000);
            }  

            AC.Dispose();
        }
    }
}