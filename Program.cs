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

            bool playing = false;
            byte[] data = new byte[65536];
            Complex[] cdata = new Complex[data.Length / 4];
            Complex[] ndata = new Complex[data.Length / 4];
            while (true)
            {

                int amount = mstr.Read(data, 0, data.Length);
                Console.WriteLine("WROTE: " + amount.ToString());

                for(int t = 0; t < amount / 4; t++)
                {
                    cdata[t] = (double)data[t * 4 + 1] / 256.0;
                }

                // Fourier transform
                if (amount == data.Length)
                {
                    FFT.OnArray(false, cdata, ndata, cdata.Length);
                    FFT.OnArray(true, ndata, cdata, cdata.Length);
                }

                // Data fix
                for (int t = 0; t < cdata.Length; t++)
                {
                    byte h = (byte)(cdata[t].Real * 255.0);
                    data[t * 2 + 1] = h;
                }

                if (mstr.Position == mstr.Length)
                {
                    break;
                }

                int buf = AL.GenBuffer();
                AL.BufferData<byte>(buf, ALFormat.Mono16, data, data.Length / 2, mstr.Frequency);
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