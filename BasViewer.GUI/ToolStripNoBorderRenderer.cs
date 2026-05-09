using System;
using System.Collections.Generic;
using System.Text;

namespace BasViewer.GUI
{
    public class NoBorderRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Do nothing — suppress border
        }
    }
}
