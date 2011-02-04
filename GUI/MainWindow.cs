using System;
using System.Collections.Generic;

using WinForms = System.Windows.Forms;

using OpenTK;
using OpenTK.Audio;
using OpenTKGUI;

namespace MD.GUI
{
    /// <summary>
    /// Main window for MD.
    /// </summary>
    public class MainWindow : HostWindow
    {
        public MainWindow()
            : base("MD", 640, 480)
        {
            this.WindowState = WindowState.Maximized;

            // Menu items
            MenuItem[] menuitems = new MenuItem[]
            {
                MenuItem.Create("File", new MenuItem[]
                {
                    MenuItem.Create("Import", delegate
                    {
                        using(var fd = new WinForms.OpenFileDialog())
                        {
                            fd.Filter = "MP3 Files |*.mp3";
                            if (fd.ShowDialog() == WinForms.DialogResult.OK)
                            {
                                string file = fd.FileName;
                                AudioContext ac = new AudioContext();
                                AudioOutput ao = new AudioOutput(new MP3AudioFeed(file));
                                ao.Play();
                            }
                            else
                            {
                                return;
                            }
                        }
                    }),
                    MenuItem.Create("Exit", delegate
                    {
                        this.Close();
                    })
                })
            };

            // Workspace and main splitter
            WorkSpace wks;
            SplitContainer msc = new SplitContainer(Axis.Horizontal, WorkSpace.CreateScrollable(out wks), new Blank(Color.RGB(0.8, 0.8, 0.8)).WithBorder(0.0, 1.0, 1.0, 1.0));
            msc.NearSize = 300.0;

            // Menu and splitter
            Menu menu = new Menu(menuitems);
            SplitContainer sc = new SplitContainer(Axis.Vertical, menu, msc);
            sc.NearSize = 30.0;

            // Main layer container
            LayerContainer lc = new LayerContainer(sc);

            this.Control = lc;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            WinForms.Application.DoEvents();
            base.OnUpdateFrame(e);
        }
    }
}