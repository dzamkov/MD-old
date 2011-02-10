using System;
using System.Collections.Generic;
using System.Threading;

namespace MD
{
    /// <summary>
    /// Manages an amount of threads to complete a queue of tasks.
    /// </summary>
    public class ThreadPool
    {
        public ThreadPool(NextTaskHandler NextTask)
        {
            this._NextTask = NextTask;
            this._TargetThreadAmount = 5;
            this._WaitTime = 200;
        }

        /// <summary>
        /// Gets or sets the amount of threads running in the thread pool.
        /// </summary>
        public int ThreadAmount
        {
            get
            {
                return this._TargetThreadAmount;
            }
            set
            {
                this._TargetThreadAmount = value;
                this.Start();
            }
        }

        /// <summary>
        /// Creates all threads requested by the thread pool.
        /// </summary>
        public void Start()
        {
            lock (this)
            {
                while (this._ThreadAmount < this._TargetThreadAmount)
                {
                    this._ThreadAmount++;
                    Thread th = new Thread(this._ThreadLoop);
                    th.IsBackground = true;
                    th.Start();
                }
            }
        }

        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, a thread must wait if it didn't get a task.
        /// </summary>
        public int TaskWaitTime
        {
            get
            {
                return this._WaitTime;
            }
            set
            {
                this._WaitTime = value;
            }
        }

        private void _ThreadLoop()
        {
            while (true)
            {
                lock (this)
                {
                    if (this._ThreadAmount > this._TargetThreadAmount)
                    {
                        return;
                    }
                }

                Action task = this._NextTask();
                if (task == null)
                {
                    Thread.Sleep(this._WaitTime);
                }
                else
                {
                    task();
                }
            }
        }

        private NextTaskHandler _NextTask;
        private int _WaitTime;
        private int _ThreadAmount;
        private int _TargetThreadAmount;
    }

    /// <summary>
    /// A handler which gets the next task for a thread pool. Handlers of this type should be thread safe.
    /// </summary>
    public delegate Action NextTaskHandler();
}