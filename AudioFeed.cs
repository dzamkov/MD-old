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
        /// Gets the amount of bytes in a sample of this audio source.
        /// </summary>
        public int BytesPerSample
        {
            get
            {
                return AudioSource.GetBytesPerSample(this.Format);
            }
        }

        /// <summary>
        /// Synchronously plays the specified audio feed. This function does not return.
        /// </summary>
        public static void Play(AudioFeed Feed, int BufferSize, int BufferAmount)
        {
            AudioContext AC = new AudioContext();
            int s = AL.GenSource();
            int bps = Feed.BytesPerSample;
            byte[] data = new byte[BufferSize * bps];


            LinkedList<int> buffersplaying = new LinkedList<int>();
            Stack<int> buffersavailable = new Stack<int>();

            for (int t = 0; t < BufferAmount; t++)
            {
                buffersavailable.Push(AL.GenBuffer());
            }

            // Approximate wait time needed in the loop
            int waittime = (int)((double)Feed.SampleRate / (double)BufferSize / (double)BufferAmount * 4.0);


            while (true)
            {
                // Buffer data
                while (buffersavailable.Count > 0)
                {
                    int amount = Feed.Read(BufferSize, data, 0);
                    int buf = buffersavailable.Pop();
                    AL.BufferData<byte>(buf, Feed.Format, data, amount * bps, Feed.SampleRate);
                    AL.SourceQueueBuffer(s, buf);
                    buffersplaying.AddLast(buf);

                    // Play if not already
                    if (AL.GetSourceState(s) != ALSourceState.Playing)
                    {
                        AL.SourcePlay(s);
                    }
                }

                // Wait a little
                Thread.Sleep(waittime);

                // Remove played buffers
                int processed;
                AL.GetSource(s, ALGetSourcei.BuffersProcessed, out processed);
                if (processed > 0)
                {
                    AL.SourceUnqueueBuffers(s, processed);
                }
                while (processed > 0)
                {
                    processed--;
                    LinkedListNode<int> node = buffersplaying.First;
                    buffersavailable.Push(node.Value);
                    buffersplaying.Remove(node);
                }
            }
        }
    }


}