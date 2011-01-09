using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace MD.GUI
{
    public partial class Workspace : UserControl
    {
        public Workspace()
        {
            InitializeComponent();
            this._WorkList = new LinkedList<WorkItem>();

            this._ScrollBar.ValueChanged += delegate
            {
                this._Update();
            };
            this.DoubleBuffered = true;
        }

        /// <summary>
        /// The amount of padding in pixels added to work items in the work space.
        /// </summary>
        public const int ItemPadding = 10;

        /// <summary>
        /// The amount of indentation in pixels, a line is produced that connects to child items.
        /// </summary>
        public const int ParentIndentation = 12;

        /// <summary>
        /// The additional amount of indentation in pixels, child items are indented from the line produced by the parent.
        /// </summary>
        public const int ChildIndentation = 6;

        /// <summary>
        /// The distance, in pixels, work items are seperated.
        /// </summary>
        public const int Seperation = 15;

        /// <summary>
        /// The thickness of connectors between parents and children.
        /// </summary>
        public const int ConnectorThickness = 2;


        /// <summary>
        /// Adds a new top-level work item to this workspace.
        /// </summary>
        public void AddItem(WorkItem Item)
        {
            this._WorkList.AddLast(Item);
            this._Update();
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            this._Update();
        }

        /// <summary>
        /// Updates the items in the workspace.
        /// </summary>
        private void _Update()
        {
            this.SuspendLayout();
            int sv = this._ScrollBar.Value;
            int hei = ItemPadding - sv;
            this._Update(ref hei, ItemPadding, this.ClientSize.Width - ItemPadding - this._ScrollBar.Width, this._WorkList);
            int max = hei + ItemPadding + sv - this.ClientSize.Height;
            if (max > 0)
            {
                this._ScrollBar.Maximum = max;
                this._ScrollBar.Enabled = true;
            }
            else
            {
                this._ScrollBar.Maximum = 2;
                this._ScrollBar.Enabled = false;
            }
            this.ResumeLayout(true);
        }

        /// <summary>
        /// Updates a portion of the workspace.
        /// </summary>
        private void _Update(ref int Height, int Left, int Right, LinkedList<WorkItem> List)
        {
            LinkedListNode<WorkItem> cur = List.First;
            while (cur != null)
            {
                WorkItem item = cur.Value;
                if (item.IsDisposed)
                {
                    LinkedListNode<WorkItem> todel = cur;
                    cur = cur.Next;
                    List.Remove(todel);
                }
                else
                {
                    if (item.Parent != this)
                    {
                        this.Controls.Add(item);
                    }

                    item.Location = new Point(Left, Height);
                    item.Size = new Size(Right - Left, WorkItem.ItemHeight);

                    Height += WorkItem.ItemHeight;
                    Height += Seperation;

                    this._Update(ref Height, Left + ParentIndentation + ChildIndentation, Right, item._Children);

                    cur = cur.Next;
                }
            }
        }

        /// <summary>
        /// Called when a work item is dragged into the workspace.
        /// </summary>
        internal WorkItem _DragUpdate(Point ClientPoint, WorkItem Item, WorkItem CurrentReceivingItem)
        {
            int hei = ClientPoint.Y + this._ScrollBar.Value - ItemPadding;
            _EnterPoint ep;
            WorkItem newrec = null;
            if (this._GetEnterPoint(ref hei, null, this._WorkList, out ep))
            {
                if (ep.Parent == null || ep.Parent._State == WorkItemState.Normal)
                {
                    LinkedList<WorkItem> list = ep.Parent == null ? this._WorkList : ep.Parent._Children;
                    if (ep.Before == null && (ep.After == null || ep.After.Value._State == WorkItemState.Normal))
                    {
                        newrec = WorkItem.CreateEntering();
                        list.AddFirst(newrec);
                    }
                    if (ep.Before != null && ep.Before.Value._State == WorkItemState.Normal && (ep.After == null || ep.After.Value._State == WorkItemState.Normal))
                    {
                        newrec = WorkItem.CreateEntering();
                        list.AddAfter(ep.Before, newrec);
                    }
                }
            }
            else
            {
                if (ep.Before != null)
                {
                    WorkItem before = ep.Before.Value;
                    if (before._State == WorkItemState.Normal)
                    {
                        newrec = WorkItem.CreateEntering();
                        this._WorkList.AddLast(newrec);
                    }
                }
                else
                {
                    newrec = WorkItem.CreateEntering();
                    this._WorkList.AddFirst(newrec);
                }
            }

            if (newrec != null)
            {
                if (CurrentReceivingItem != null)
                {
                    CurrentReceivingItem.Dispose();
                }
                this._Update();
                return newrec;
            }
            else
            {
                return CurrentReceivingItem;
            }
        }

        /// <summary>
        /// Called when a work item that was previously dragged into the workspace has left.
        /// </summary>
        internal void _DragLeave(WorkItem ReceivingItem)
        {
            ReceivingItem.Dispose();
            this._Update();
        }

        /// <summary>
        /// Gets a spot between items showing where a dragged item enters.
        /// </summary>
        private struct _EnterPoint
        {
            public LinkedListNode<WorkItem> Before;
            public LinkedListNode<WorkItem> After;
            public WorkItem Parent;
        }

        private bool _GetEnterPoint(ref int Y, WorkItem Parent, LinkedList<WorkItem> WorkList, out _EnterPoint EnterPoint)
        {
            LinkedListNode<WorkItem> before = null;
            LinkedListNode<WorkItem> cur = WorkList.First;
            while(cur != null)
            {
                LinkedListNode<WorkItem> after = cur.Next;
                WorkItem item = cur.Value;
                if (Y < 0)
                {
                    EnterPoint = new _EnterPoint()
                    {
                        Before = before,
                        After = after,
                        Parent = Parent,
                    };
                    return true;
                }
                if (Y < WorkItem.ItemHeight)
                {
                    EnterPoint = new _EnterPoint()
                    {
                        Before = null,
                        After = item._Children.First,
                        Parent = item,
                    };
                    return true;
                }
                Y -= WorkItem.ItemHeight;
                Y -= Seperation;
                if (this._GetEnterPoint(ref Y, item, item._Children, out EnterPoint))
                {
                    return true;
                }

                before = cur;
                cur = after;
            }
            EnterPoint = new _EnterPoint()
            {
                Before = before
            };
            return false;
        }

        private LinkedList<WorkItem> _WorkList;
    }
}
