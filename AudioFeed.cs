using System;
using System.Collections.Generic;
using System.Threading;

using OpenTK.Audio.OpenAL;
using OpenTK.Audio;

namespace MD
{
    /// <summary>
    /// A continous, infinitely large, collection of samples that may only be accessed at a certain location and in certain conditions.
    /// </summary>
    public abstract class AudioFeed
    {
        /// <summary>
        /// Reads to the specified output array.
        /// </summary>
        /// <param name="Amount">The amount of samples to read.</param>
        /// <param name="Output">The array to read to.</param>
        /// <param name="Offset">The place in the array to begin reading to.</param>
        /// <returns>The amount of samples actually read.</returns>
        public abstract int Read(int Amount, byte[] Output, int Offset);

        /// <summary>
        /// Gets the amount of samples in a second.
        /// </summary>
        public abstract int SampleRate { get; }

        /// <summary>
        /// Gets the format of the audio in the audio source.
        /// </summary>
        public abstract ALFormat Format { get; }

        /// <summary>
        /// Gets the amount of channels this audio feed has.
        /// </summary>
        public int Channels
        {
            get
            {
                return AudioSource.GetChannels(this.Format);
            }
        }

        /// <summary>
        /// Gets the amount of bytes in a sample of this audio feed.
        /// </summary>
        public int BytesPerSample
        {
            get
            {
                return AudioSource.GetBytesPerSample(this.Format);
            }
        }
    }

    /// <summary>
    /// Continously plays an audio feed.
    /// </summary>
    public class AudioOutput
    {
        public AudioOutput(AudioFeed Source, int BufferSize, int BufferAmount)
        {
            this._Feed = Source;
            this._BufferSize = BufferSize;
            this._BufferAmount = BufferAmount;
            this._Init();
        }

        public AudioOutput(AudioFeed Source)
            : this(Source, 4096, 2)
        {

        }

        private void _Init()
        {
            this._Source = AL.GenSource();
            int bps = this._Feed.BytesPerSample;
            this._Data = new byte[this._BufferSize * bps];
            this._BuffersPlaying = new LinkedList<int>();
            this._BuffersAvailable = new Stack<int>();

            for (int t = 0; t < this._BufferAmount; t++)
            {
                this._BuffersAvailable.Push(AL.GenBuffer());
            }

            // Approximate wait time needed in the loop
            this._WaitTime = (int)((double)this._Feed.SampleRate / (double)this._BufferSize / (double)this._BufferAmount * 4.0);
        }

        /// <summary>
        /// Causes the output to begin playing, if it hasn't already.
        /// </summary>
        public void Play()
        {
            if (!this._Playing)
            {
                if (this._BuffersPlaying.Count > 0 && AL.GetSourceState(this._Source) != ALSourceState.Playing)
                {
                    AL.SourcePlay(this._Source);
                }
                this._Playing = true;
                this._Thread = new Thread(this._PlayLoop);
                this._Thread.IsBackground = true;
                this._Thread.Start();
            }
        }

        /// <summary>
        /// Causes the output to immediately stop playing.
        /// </summary>
        public void Stop()
        {
            if (this._Playing)
            {
                this._Playing = false;
                if (AL.GetSourceState(this._Source) != ALSourceState.Paused)
                {
                    AL.SourcePause(this._Source);
                }
                this._Thread.Join();
                this._Thread = null;
            }
        }

        private void _PlayLoop()
        {
            int bps = this._Feed.BytesPerSample;
            while (this._Playing)
            {
                // Buffer data
                while (this._BuffersAvailable.Count > 0)
                {
                    int amount = this._Feed.Read(this._BufferSize, this._Data, 0);
                    int buf = this._BuffersAvailable.Pop();
                    AL.BufferData<byte>(buf, this._Feed.Format, this._Data, amount * bps, this._Feed.SampleRate);
                    AL.SourceQueueBuffer(this._Source, buf);
                    this._BuffersPlaying.AddLast(buf);

                    if (this._Playing && AL.GetSourceState(this._Source) != ALSourceState.Playing)
                    {
                        AL.SourcePlay(this._Source);
                    }
                }

                // Wait a little
                Thread.Sleep(this._WaitTime);

                // Remove played buffers
                int processed;
                AL.GetSource(this._Source, ALGetSourcei.BuffersProcessed, out processed);
                if (processed > 0)
                {
                    AL.SourceUnqueueBuffers(this._Source, processed);
                }
                while (processed > 0)
                {
                    processed--;
                    LinkedListNode<int> node = this._BuffersPlaying.First;
                    this._BuffersAvailable.Push(node.Value);
                    this._BuffersPlaying.Remove(node);
                }
            }
        }

        private bool _Playing;
        private byte[] _Data;
        private int _Source;
        private int _WaitTime;
        LinkedList<int> _BuffersPlaying;
        Stack<int> _BuffersAvailable;
        private int _BufferSize;
        private int _BufferAmount;
        private AudioFeed _Feed;
        private Thread _Thread;
    }
}