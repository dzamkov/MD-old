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
            byte[] idata = new byte[65536];
            byte[] odata = new byte[idata.Length / 2];
            Complex[] cdata = new Complex[idata.Length / 4];
            Complex[] ndata = new Complex[idata.Length / 4];
            while (true)
            {

                int amount = mstr.Read(idata, 0, idata.Length);
                Console.WriteLine("WROTE: " + amount.ToString()); 

                for(int t = 0; t < amount / 4; t++)
                {
                    short samp = BitConverter.ToInt16(idata, t * 4);
                    cdata[t].Real = (double)samp / 65536.0;
                }

                // Fourier transform
                if (amount == idata.Length)
                {
                    FFT.OnArray(false, cdata, ndata, cdata.Length);
                    double samplefreq = mstr.Frequency;
                    for (int t = 0; t < ndata.Length; t++)
                    {
                        double actfreq = FFT.AbsoluteFrequency(t, ndata.Length, samplefreq);
                        if (actfreq < 1000.0 || actfreq > 3000.0)
                        {
                            ndata[t] = 0.0;
                        }
                    }
                    FFT.OnArray(true, ndata, cdata, cdata.Length);
                }

                // Data fix
                for (int t = 0; t < cdata.Length; t++)
                {
                    double dat = cdata[t].Real;
                    byte[] bytes = BitConverter.GetBytes((short)(dat * 65536.0));
                    odata[t * 2 + 0] = bytes[0];
                    odata[t * 2 + 1] = bytes[1];
                }

                if (mstr.Position == mstr.Length)
                {
                    break;
                }

                int buf = AL.GenBuffer();
                AL.BufferData<byte>(buf, ALFormat.Mono16, odata, odata.Length, mstr.Frequency);
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