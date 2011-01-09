using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace MD.GUI
{
    /// <summary>
    /// A graphical representation of an object that can be manipulated by the program.
    /// </summary>
    public partial class WorkItem : UserControl
    {
        private WorkItem(WorkItemState State)
        {
            this._State = State;
            this._Children = new LinkedList<WorkItem>();
        }

        public WorkItem()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this._Label.ForeColor = _LabelNormalColor;
            this._ToolStrip.BackColor = this.Color.Desaturate(0.6, 1.0);


            this._ToolStrip.MouseDown += new MouseEventHandler(this._OnMouseDown);
            this._Label.MouseDown += new MouseEventHandler(this._OnMouseDown);
            this._ToolStrip.MouseUp += new MouseEventHandler(this._OnMouseUp);
            this._Label.MouseUp += new MouseEventHandler(this._OnMouseUp);
            this._ToolStrip.MouseLeave += new EventHandler(this._OnMouseLeave);
            this._Label.MouseLeave += new EventHandler(this._OnMouseLeave);

            this._Children = new LinkedList<WorkItem>();
            this._State = WorkItemState.Normal;
        }

        /// <summary>
        /// Creates a work item in the entering state.
        /// </summary>
        public static WorkItem CreateEntering()
        {
            return new WorkItem(WorkItemState.Entering);
        }

        /// <summary>
        /// The height of a workitem.
        /// </summary>
        public const int ItemHeight = 25;

        /// <summary>
        /// Gets the primary color of this work item.
        /// </summary>
        public Color Color
        {
            get
            {
                return Color.RGB(0.0, 0.5, 1.0);
            }
        }

        /// <summary>
        /// Gets the text shown on the work item.
        /// </summary>
        public string ItemText
        {
            get
            {
                return "Test";
            }
        }

        private static readonly FontFamily _Font = GetFontByName("Verdana");

        /// <summary>
        /// Gets a font family with the specified name.
        /// </summary>
        public static FontFamily GetFontByName(string Name)
        {
            foreach (FontFamily ff in FontFamily.Families)
            {
                if (ff.Name == Name)
                {
                    return ff;
                }
            }
            return null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            switch (this._State)
            {
                case WorkItemState.Normal:
                    base.OnPaint(e);
                    break;
                case WorkItemState.Leaving:
                    using (Pen p = new Pen(this.Color, 4.0f))
                    {
                        e.Graphics.DrawRectangle(p, this.ClientRectangle);
                    }
                    break;
                case WorkItemState.Entering:
                    using (Pen p = new Pen(Color.RGB(0.0, 0.0, 0.0), 4.0f))
                    {
                        e.Graphics.DrawRectangle(p, this.ClientRectangle);
                    }
                    break;
            }
        }

        private static Color _LabelNormalColor = Color.RGB(0.0, 0.0, 0.0);
        private static Color _LabelActiveColor = Color.RGB(0.2, 0.2, 0.2);

        private void _UpdateState()
        {
            if (this._State == WorkItemState.Normal)
            {
                this._ToolStrip.Visible = true;
            }
            else
            {
                this._ToolStrip.Visible = false;
            }
        }

        /// <summary>
        /// Cancels this work item from leaving.
        /// </summary>
        internal void _CancelLeave()
        {
            this._State = WorkItemState.Normal;
            this._UpdateState();
        }

        private void _OnMouseDown(object Sender, MouseEventArgs E)
        {
            if (this._State == WorkItemState.Normal)
            {
                this._ReadyDrag = Sender;
                this._DragOffset = this.PointToClient(System.Windows.Forms.Cursor.Position);
                this._Label.ForeColor = _LabelActiveColor;
            }
        }

        private void _OnMouseUp(object Sender, MouseEventArgs E)
        {
            this._ReadyDrag = null;
            this._Label.ForeColor = _LabelNormalColor;
        }

        private void _OnMouseLeave(object Sender, EventArgs E)
        {
            if (this._ReadyDrag == Sender)
            {
                // Drag item
                new DraggedWorkItem(this, this._DragOffset, false, this.FindForm()).Show();
                this._State = WorkItemState.Leaving;
                this._UpdateState();

                this._Label.ForeColor = _LabelNormalColor;
                this._ReadyDrag = null;
            }
        }

        internal LinkedList<WorkItem> _Children;
        internal WorkItemState _State;
        private Point _DragOffset;
        private object _ReadyDrag;
    }

    public enum WorkItemState
    {
        /// <summary>
        /// The work item is in a normal state.
        /// </summary>
        Normal,

        /// <summary>
        /// The work item is leaving the work space (by dragging).
        /// </summary>
        Leaving,

        /// <summary>
        /// The work item may enter the work space.
        /// </summary>
        Entering
    }
}
