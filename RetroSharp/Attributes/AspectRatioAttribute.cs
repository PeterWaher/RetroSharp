using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RetroSharp
{
    /// <summary>
    /// Defines the desired aspect ratio of the screen. If the current screen does not have this aspect ratio, the borders may be changed to make
    /// the visible screen appear using the provided aspect ratio.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AspectRatioAttribute : RetroAttribute
    {
        private int width;
        private int height;

        /// <summary>
        /// Defines the desired aspect ratio of the screen. If the current screen does not have this aspect ratio, the borders may be changed to make
        /// the visible screen appear using the provided aspect ratio.
        /// </summary>
        /// <param name="Width">Relative width of default screen.</param>
        /// <param name="Height">Relative height of default screen.</param>
        public AspectRatioAttribute(int Width, int Height)
        {
            this.width = AssertPositive(Width);
            this.height = AssertPositive(Height);
        }

        /// <summary>
        /// Relative width of default screen.
        /// </summary>
        public int Width
        {
            get { return this.width; }
        }

        /// <summary>
        /// Relative height of default screen.
        /// </summary>
        public int Height
        {
            get { return this.height; }
        }
    }
}
