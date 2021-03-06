﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using VisualLocalizer.Library;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Adds "Visual Localizer" output window pane.
    /// </summary>
    internal sealed class VLOutputWindow : OutputWindow {

        static VLOutputWindow() {
            cache.Add(typeof(Guids.VisualLocalizerWindowPane).GUID, GetStandardPane(typeof(Guids.VisualLocalizerWindowPane).GUID));            
        }

        /// <summary>
        /// Represents "Visual Localizer" window pane (among "Debug", "Build", "General" etc.)
        /// </summary>
        public static OutputWindowPane VisualLocalizerPane {
            get {
                return GetPaneOrBlackHole(typeof(Guids.VisualLocalizerWindowPane).GUID);                
            }
        }
    }
}
