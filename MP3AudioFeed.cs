using System;
using System.Collections.Generic;
using System.IO;

using OpenTK.Audio.OpenAL;

using Mp3Sharp;

namespace MD
{
    /// <summary>
    /// An audio feed from a mp3 file.
    /// </summary>
    public class MP3AudioFeed : AudioFeed
    {
        public MP3AudioFeed(string File)
        {
            this._Stream = new Mp3Stream(File);
            this._Init();
        }

        public MP3AudioFeed(Stream Source)
        {
            this._Stream = new Mp3Stream(Source);
            this._Init();
        }

        private void _Init()
        {
            // Read some samples to force initialization
            byte[] samp = new byte[100];
            this._Stream.Read(samp, 0, samp.Length);
            this._Stream.Seek(0, SeekOrigin.Begin);
        }

        public override int Read(int Amount, byte[] Output, int Offset)
        {
            int bps = this.BytesPerSample;
            if (this._Stream.Position < this._Stream.Length)
            {
                int amount = 0;
                try
                {
                    amount = this._Stream.Read(Output, Offset, Amount * bps);
                }
                catch (IndexOutOfRangeException)
                {

                }
                return amount / bps;
            }
            else
            {
                for (int t = 0; t < Amount * bps; t++)
                {
                    Output[Offset + t] = 0;
                }
                return Amount;
            }
        }

        public override int SampleRate
        {
            get
            {
                return this._Stream.Frequency;
            }
        }

        public override ALFormat Format
        {
            get
            {
                return this._Stream.Format == SoundFormat.Pcm16BitMono ? ALFormat.Mono16 : ALFormat.Stereo16;
            }
        }

        private Mp3Stream _Stream;
    }
}