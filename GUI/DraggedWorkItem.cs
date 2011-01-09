using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MD.GUI
{
    public partial class DraggedWorkItem : Form
    {
        public DraggedWorkItem(WorkItem Item, Point Offset, bool ReleaseOnClick, Form TargetForm)
        {
            InitializeComponent();
            this.Capture = true;

            Bitmap bm = new Bitmap(Item.Width, Item.Height);
            Item.DrawToBitmap(bm, new Rectangle(0, 0, bm.Width, bm.Height));
            this.BackgroundImageLayout = ImageLayout.Center;
            this.BackgroundImage = bm;
            this.Size = Item.Size;

            this._Item = Item;
            this._TargetForm = TargetForm;
            this._Offset = Offset;
            this._ReleaseOnClick = ReleaseOnClick;

            this._Reposition();
        }

        private void _Reposition()
        {
            Point mousepos = System.Windows.Forms.Cursor.Position;
            this.Location = new Point(mousepos.X - this._Offset.X, mousepos.Y - this._Offset.Y);

            Control on = this._TargetForm;
            Workspace wk = null;
            Point clicoord = on.PointToClient(mousepos);
            while ((wk = on as Workspace) == null)
            {
                on = on.GetChildAtPoint(clicoord);
                if (on == null)
                {
                    break;
                }
                else
                {
                    clicoord = on.PointToClient(mousepos);
                }
            }

            if (wk != this._ReceivingWorkspace && this._ReceivingItem != null)
            {
                this._ReceivingWorkspace._DragLeave(this._ReceivingItem);
                this._ReceivingItem = null;
            }
            this._ReceivingWorkspace = wk;
            if (wk != null)
            {
                this._ReceivingItem = wk._DragUpdate(clicoord, this._Item, this._ReceivingItem);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            this._Reposition();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!this._ReleaseOnClick)
            {
                this._Release();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (this._ReleaseOnClick)
            {
                this._ReleaseOnClick = false;
            }
        }

        private void _Release()
        {
            this._Item._CancelLeave();
            this.BackgroundImage.Dispose();
            this.Close();
        }

        private Form _TargetForm;
        private bool _ReleaseOnClick;
        private Point _Offset;
        private Workspace _ReceivingWorkspace;
        private WorkItem _ReceivingItem;
        private WorkItem _Item;
    }
}
