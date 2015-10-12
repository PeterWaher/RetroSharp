using System;
using System.Collections.Generic;
using System.Text;

namespace RetroSharp
{
    /// <summary>
    /// Access to the characters of the main screen.
    /// </summary>
    public class Screen 
    {
        /// <summary>
        /// Access to the characters of the main screen.
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <returns>Character value</returns>
        public char this[int x, int y]
        {
            get
            {
                int w;

                if (x < 0 || x >= (w = RetroApplication.ConsoleWidth))
                    return (char)0;

                if (y < 0 || y >= RetroApplication.ConsoleHeight)
                    return (char)0;

                return (char)RetroApplication.ScreenBuffer[y * w + x];
            }

            set
            {
                int w;

                if (x >= 0 && y >= 0 && x < (w = RetroApplication.ConsoleWidth) && y < RetroApplication.ConsoleHeight)
                    RetroApplication.ScreenBuffer[y * w + x] = value;
            }
        }
    }
}
