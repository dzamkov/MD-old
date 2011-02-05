using System;
using System.Collections.Generic;
using System.IO;

using OpenTK.Audio.OpenAL;

namespace MD
{
    /// <summary>
    /// A static one-dimensional sampling of audio.
    /// </summary>
    public abstract class AudioSource
    {
        /// <summary>
        /// Reads some data into the specified output array.
        /// </summary>
        /// <param name="Position">The position in samples to start reading at.</param>
        /// <param name="Amount">The amount of samples to read.</param>
        /// <param name="Output">The byte array to read to.</param>
        /// <param name="Offset">The place in the byte array to start reading to.</param>
        public abstract void Read(int Position, int Amount, byte[] Output, int Offset);

        /// <summary>
        /// Reads some data (as double samples) into the specified output array. One double is used for each channel of a sample. Data ranges from -1.0 to 1.0.
        /// </summary>
        public virtual void ReadDouble(int Position, int Amount, double[] Output, int Offset)
        {
            switch (this.Format)
            {
                case ALFormat.Mono16:
                    byte[] data = new byte[Amount * 2];
                    this.Read(Position, Amount, data, 0);
                    for (int t = 0; t < Amount; t++)
                    {
                        Output[t + Offset] = (double)BitConverter.ToInt16(data, t * 2) / 32768.0;
                    }
                    return;
                case ALFormat.Stereo16:
                    data = new byte[Amount * 4];
                    this.Read(Position, Amount, data, 0);
                    for (int t = 0; t < Amount * 2; t++)
                    {
                        Output[t + Offset] = (double)BitConverter.ToInt16(data, t * 2) / 32768.0;
                    }
                    return;
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the size of the audio source in samples.
        /// </summary>
        public abstract int Size { get; }

        /// <summary>
        /// Gets the amount of samples in a second.
        /// </summary>
        public abstract int SampleRate { get; }

        /// <summary>
        /// Gets the format of the audio in the audio source.
        /// </summary>
        public abstract ALFormat Format { get; }

        /// <summary>
        /// Gets the amount of channels this audio source has.
        /// </summary>
        public int Channels
        {
            get
            {
                return GetChannels(this.Format);
            }
        }

        /// <summary>
        /// Gets the amount of bytes in a sample of this audio source.
        /// </summary>
        public int BytesPerSample
        {
            get
            {
                return GetBytesPerSample(this.Format);
            }
        }

        /// <summary>
        /// Gets an audio feed that plays this source.
        /// </summary>
        public AudioFeed Play
        {
            get
            {
                return new AudioSourcePlayer(this);
            }
        }

        /// <summary>
        /// Gets the amount of channels in a sample of the specified format.
        /// </summary>
        public static int GetChannels(ALFormat Format)
        {
            switch (Format)
            {
                case ALFormat.Stereo16: return 2;
                case ALFormat.Stereo8: return 2;
                case ALFormat.Mono16: return 1;
                case ALFormat.Mono8: return 1;
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads silence to the specified byte array.
        /// </summary>
        public static void ReadSilence(ALFormat Format, int Amount, byte[] Data, int Offset)
        {
            switch (Format)
            {
                case ALFormat.Stereo16:
                    for (int t = 0; t < Amount * 4; t++)
                    {
                        Data[t + Offset] = 0;
                    }
                    break;
                case ALFormat.Mono16:
                    for (int t = 0; t < Amount * 2; t++)
                    {
                        Data[t + Offset] = 0;
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets the amount of bytes in a sample of the specified format.
        /// </summary>
        public static int GetBytesPerSample(ALFormat Format)
        {
            switch (Format)
            {
                case ALFormat.Stereo16: return 4;
                case ALFormat.Stereo8: return 2;
                case ALFormat.Mono16: return 2;
                case ALFormat.Mono8: return 1;
            }
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// An audio source stored in RAM in chunks.
    /// </summary>
    public class MemoryAudioSource : AudioSource
    {
        public MemoryAudioSource(int SampleRate, ALFormat Format, List<byte[]> Chunks, int ChunkSize, int Size)
        {
            this._SampleRate = SampleRate;
            this._Format = Format;
            this._ChunkSize = ChunkSize;
            this._Chunks = Chunks;
            this._Size = Size;
        }

        public override void Read(int Position, int Amount, byte[] Output, int Offset)
        {
            int bps = this.BytesPerSample;
            int cpos = Position % this._ChunkSize;
            int chunk = Position / this._ChunkSize;
            while (Amount > 0)
            {
                byte[] chunkdata = this._Chunks[chunk];
                if (Amount + cpos > this._ChunkSize)
                {
                    int dif = this._ChunkSize - cpos;
                    for (int t = 0; t < dif * bps; t++)
                    {
                        Output[Offset + t] = chunkdata[cpos * bps + t];
                    }
                    Amount -= dif;
                    Offset += dif * bps;
                    chunk++;
                    cpos = 0;
                }
                else
                {
                    for (int t = 0; t < Amount * bps; t++)
                    {
                        Output[Offset + t] = chunkdata[cpos * bps + t];
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the amount of samples stored in a chunk.
        /// </summary>
        public int ChunkSize
        {
            get
            {
                return this._ChunkSize;
            }
        }

        public override int Size
        {
            get
            {
                return this._Size;
            }
        }

        public override int SampleRate
        {
            get
            {
                return this._SampleRate;
            }
        }

        public override ALFormat Format
        {
            get
            {
                return this._Format;
            }
        }

        private int _SampleRate;
        private ALFormat _Format;
        private int _Size;
        private int _ChunkSize;
        private List<byte[]> _Chunks;
    }

    /// <summary>
    /// Pads (or truncates) another source to a certain length.
    /// </summary>
    public class PaddedSource : AudioSource
    {
        public PaddedSource(AudioSource Source, int PaddedSize)
        {
            this._Source = Source;
            this._PaddedSize = PaddedSize;
        }

        public override void Read(int Position, int Amount, byte[] Output, int Offset)
        {
            int pad = (Position + Amount) - this._Source.Size;
            if (pad > 0)
            {
                if (pad < Amount)
                {
                    int amountread = Amount - pad;
                    this._Source.Read(Position, amountread, Output, Offset);
                    Offset += amountread;
                }
                AudioSource.ReadSilence(this._Source.Format, pad, Output, Offset);
            }
            else
            {
                this._Source.Read(Position, Amount, Output, Offset);
            }
        }

        public override int SampleRate
        {
            get
            {
                return this._Source.SampleRate;
            }
        }

        public override int Size
        {
            get
            {
                return this._PaddedSize;
            }
        }

        public override ALFormat Format
        {
            get
            {
                return this._Source.Format;
            }
        }

        private AudioSource _Source;
        private int _PaddedSize;
    }

    /// <summary>
    /// An audio feed that takes data from an audio source. The source will be looped.
    /// </summary>
    public class AudioSourcePlayer : AudioFeed
    {
        public AudioSourcePlayer(AudioSource Source)
        {
            this._Source = Source;
            this._Location = 0;
        }

        public override int Read(int Amount, byte[] Output, int Offset)
        {
            int dif = this._Source.Size - this._Location;
            if (dif < Amount)
            {
                this._Source.Read(this._Location, dif, Output, Offset);
                this._Source.Read(0, Amount - dif, Output, Offset + dif);
                this._Location = Amount - dif;
            }
            else
            {
                this._Source.Read(this._Location, Amount, Output, Offset);
                this._Location += Amount;
            }
            return Amount;
        }

        public override int SampleRate
        {
            get
            {
                return this._Source.SampleRate;
            }
        }

        public override ALFormat Format
        {
            get
            {
                return this._Source.Format;
            }
        }

        /// <summary>
        /// Gets the audio source that is being played.
        /// </summary>
        public AudioSource Source
        {
            get
            {
                return this._Source;
            }
        }

        private int _Location;
        private AudioSource _Source;
    }
}