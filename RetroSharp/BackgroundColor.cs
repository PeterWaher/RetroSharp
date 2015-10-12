using System;
using System.Collections.Generic;
using System.Drawing;

namespace RetroSharp
{
    /// <summary>
    /// Access to the background color buffer of the main screen.
    /// </summary>
    public class BackgroundColor
    {
        /// <summary>
        /// Access to the background color buffer of the main screen.
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <returns>Color</returns>
        public Color this[int x, int y]
        {
            get
            {
                int w;

                if (x < 0 || x >= (w = RetroApplication.ConsoleWidth))
                    return Color.Empty;

                if (y < 0 || y >= RetroApplication.ConsoleHeight)
                    return Color.Empty;

                return RetroApplication.BackgroundColorBuffer[y * w + x];
            }

            set
            {
                int w;

                if (x >= 0 && y >= 0 && x < (w = RetroApplication.ConsoleWidth) && y < RetroApplication.ConsoleHeight)
                    RetroApplication.BackgroundColorBuffer[y * w + x] = value;
            }
        }
    }
}
