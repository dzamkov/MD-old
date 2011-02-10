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

            // Client area
            VariableContainer clientarea = new VariableContainer();

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
                                MemoryAudioSource mas = new MP3AudioFeed(file).Copy(4096, 4096 * 100);

                                SpectrogramView sp = new SpectrogramView(mas);
                                clientarea.Client = sp;

                                AudioOutput ao = new AudioOutput(mas.Play);
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

            // Menu and splitter
            Menu menu = new Menu(menuitems);
            SplitContainer sc = new SplitContainer(Axis.Vertical, menu.WithBorder(0.0, 0.0, 0.0, 1.0), clientarea);
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