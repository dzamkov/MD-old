using System;
using System.Collections.Generic;


using OpenTKGUI;

namespace MD.GUI
{
    /// <summary>
    /// A control that shows a graphic hierarchy of work items.
    /// </summary>
    public class WorkSpace : Control
    {
        public WorkSpace()
        {
            Skin workitemskin = new Skin(Res.WorkItem);
            this._WorkItem = workitemskin.GetStretchableSurface(new SkinArea(0, 0, 16, 16));
            this._Mask = workitemskin.GetStretchableSurface(new SkinArea(16, 0, 16, 16));
            this._Shadow = ShadowStyle.Default.Image;
            this._Sample = Font.Default.CreateSample("Test Sample.mp3");
        }

        /// <summary>
        /// Creates and returns a scrollable workspace.
        /// </summary>
        public static Control CreateScrollable(out WorkSpace WorkSpace)
        {
            WorkSpace = new WorkSpace();
            WindowContainer window = new WindowContainer(WorkSpace);
            ScrollContainer scroll = new ScrollContainer(window, new SunkenContainer(window).WithBorder(1.0, 1.0, 0.0, 1.0));
            scroll.ClientHeight = 1000;
            return scroll;
        }

        public override void Render(GUIRenderContext Context)
        {
            Rectangle itemrect = new Rectangle(10.0, 10.0, this.Size.X - 20.0, 35.0);
            Context.DrawSurface(this._Shadow, new Rectangle(itemrect.Location - new Point(5.0, 5.0), itemrect.Size + new Point(10.0, 10.0)));
            Context.DrawSurface(this._WorkItem, itemrect);
            Context.DrawSurface(this._Mask, itemrect);
            Context.DrawText(Color.RGB(0.0, 0.0, 0.0), this._Sample, itemrect, TextAlign.Center, TextAlign.Center);
        }

        private TextSample _Sample;
        private Surface _WorkItem;
        private Surface _Mask;
        private Surface _Shadow;
    }
}