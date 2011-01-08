﻿using System;
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
            return -1;
        }
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