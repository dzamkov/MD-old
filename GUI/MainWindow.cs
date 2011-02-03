using System;
using System.Collections.Generic;

using OpenTK;
using OpenTKGUI;

namespace MD.GUI
{
    /// <summary>
    /// Main window for MD.
    /// </summary>
    public class MainWindow : HostWindow
    {
        public MainWindow()
            : base(_BuildControl, "MD", 640, 480)
        {
            this.WindowState = WindowState.Maximized;

        }

        /// <summary>
        /// Builds controls for the main window.
        /// </summary>
        private static Control _BuildControl()
        {
            // Menu items
            MenuItem[] menuitems = new MenuItem[]
            {
                MenuItem.Create("File", new MenuItem[]
                {
                    MenuItem.Create("Import", delegate
                    {
                        
                    })
                })
            };

            // Menu
            Menu menu = new Menu(menuitems);
            SplitContainer sc = new SplitContainer(Axis.Vertical, menu.WithBorder(0.0, 0.0, 0.0, 1.0), new Blank(Color.Transparent));
            sc.NearSize = 30.0;

            // Main layer container
            LayerContainer lc = new LayerContainer(sc);

            return lc;
        }
    }
}