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

        /// <summary>
        /// Brings the feed back to its begining.
        /// </summary>
        public void Reset()
        {
            this._Stream.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        /// Copies this mp3 stream to an audio source with the specified chunk size.
        /// </summary>
        public MemoryAudioSource Copy(int ChunkSize, int MaxSize)
        {
            this.Reset();
            List<byte[]> chunks = new List<byte[]>();
            int len = 0;
            int bps = this.BytesPerSample;
            while (this._Stream.Position < this._Stream.Length && len < MaxSize)
            {
                byte[] chunk = new byte[ChunkSize * bps];
                len += this._Stream.Read(chunk, 0, ChunkSize * bps) / bps;
                chunks.Add(chunk);
            }
            return new MemoryAudioSource(this.SampleRate, this.Format, chunks, ChunkSize, len > MaxSize ? MaxSize : len);
        }

        /// <summary>
        /// Copies all mp3 data to an audio source.
        /// </summary>
        public MemoryAudioSource Copy(int ChunkSize)
        {
            return this.Copy(ChunkSize, int.MaxValue);
        }

        public override int Read(int Amount, byte[] Output, int Offset)
        {
            int bps = this.BytesPerSample;
            if (this._Stream.Position < this._Stream.Length)
            {
                int amount = 0;
                amount = this._Stream.Read(Output, Offset, Amount * bps);
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