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
        public ThreadPool()
        {
            this._Tasks = new LinkedList<Action>();
            this._TargetThreadAmount = 5;
        }

        /// <summary>
        /// Gets the current amount of threads running in the thread pool.
        /// </summary>
        public int ThreadAmount
        {
            get
            {
                return this._ThreadAmount;
            }
        }

        /// <summary>
        /// Gets or sets the maximum amount of threads that can run in the thread pool at a time.
        /// </summary>
        public int TargetThreadAmount
        {
            get
            {
                return this._TargetThreadAmount;
            }
            set
            {
                this._TargetThreadAmount = value;
            }
        }

        /// <summary>
        /// Adds a task to be complete after all others.
        /// </summary>
        public void AppendTask(Action Task)
        {
            lock (this)
            {
                this._Tasks.AddLast(Task);
                this._TrySpawn();
            }
        }

        /// <summary>
        /// Adds a task to be started with the next available thread.
        /// </summary>
        public void PushTask(Action Task)
        {
            lock (this)
            {
                this._Tasks.AddFirst(Task);
                this._TrySpawn();
            }
        }

        private void _TrySpawn()
        {
            // May only be called in a lock
            if (this._ThreadAmount < this._TargetThreadAmount)
            {
                this._ThreadAmount++;
                Thread th = new Thread(this._ThreadLoop);
                th.IsBackground = true;
                th.Start();
            }
        }

        private void _ThreadLoop()
        {
            while (true)
            {
                Action task;
                lock (this)
                {
                    if (this._Tasks.Count > 0)
                    {
                        LinkedListNode<Action> first = this._Tasks.First;
                        task = first.Value;
                        this._Tasks.Remove(first);
                    }
                    else
                    {
                        this._ThreadAmount--;
                        return;
                    }
                }
                task();
            }
        }

        private LinkedList<Action> _Tasks;
        private int _ThreadAmount;
        private int _TargetThreadAmount;
    }
}