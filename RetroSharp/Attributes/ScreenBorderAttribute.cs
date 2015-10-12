using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RetroSharp
{
    /// <summary>
    /// Defines the default border size of the screen. If used in conjunction with <see cref="AspectRatioAttribute"/>,
    /// these values defines the minimum border used. The attribute can also be used to define the border color.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ScreenBorderAttribute : RetroAttribute
    {
        private int leftMargin;
        private int rightMargin;
        private int topMargin;
        private int bottomMargin;
        private Color borderColor = Color.Empty;

        /// <summary>
        /// Defines the default border size of the screen. If used in conjunction with <see cref="AspectRatioAttribute"/>,
        /// these values defines the minimum border used. The attribute can also be used to define the border color.
        /// </summary>
        /// <param name="LeftRight">Margin to the left and right of the screen.</param>
        /// <param name="TopBottom">Margon above and below the screen.</param>
        public ScreenBorderAttribute(int LeftRight, int TopBottom)
        {
            this.leftMargin = this.rightMargin = AssertNonNegative(LeftRight);
            this.topMargin = this.bottomMargin = AssertNonNegative(TopBottom);
        }

        /// <summary>
        /// Defines the default border size of the screen. If used in conjunction with <see cref="AspectRatioAttribute"/>,
        /// these values defines the minimum border used. The attribute can also be used to define the border color.
        /// </summary>
        /// <param name="Left">Margin to the left of the screen.</param>
        /// <param name="Right">Margin to the right of the screen.</param>
        /// <param name="Top">Margon above the screen.</param>
        /// <param name="Bottom">Margon below the screen.</param>
        public ScreenBorderAttribute(int Left, int Right, int Top, int Bottom)
        {
            this.leftMargin = AssertNonNegative(Left);
            this.rightMargin = AssertNonNegative(Right);
            this.topMargin = AssertNonNegative(Top);
            this.bottomMargin = AssertNonNegative(Bottom);
        }

        /// <summary>
        /// Defines the default border size of the screen. If used in conjunction with <see cref="AspectRatioAttribute"/>,
        /// these values defines the minimum border used. The attribute can also be used to define the border color.
        /// </summary>
        /// <param name="LeftRight">Margin to the left and right of the screen.</param>
        /// <param name="TopBottom">Margon above and below the screen.</param>
        /// <param name="BorderColor">Border color.</param>
        public ScreenBorderAttribute(int LeftRight, int TopBottom, KnownColor BorderColor)
        {
            this.leftMargin = this.rightMargin = AssertNonNegative(LeftRight);
            this.topMargin = this.bottomMargin = AssertNonNegative(TopBottom);
            this.borderColor = Color.FromKnownColor(BorderColor);
        }

        /// <summary>
        /// Defines the default border size of the screen. If used in conjunction with <see cref="AspectRatioAttribute"/>,
        /// these values defines the minimum border used. The attribute can also be used to define the border color.
        /// </summary>
        /// <param name="Left">Margin to the left of the screen.</param>
        /// <param name="Right">Margin to the right of the screen.</param>
        /// <param name="Top">Margon above the screen.</param>
        /// <param name="Bottom">Margon below the screen.</param>
        /// <param name="BorderColor">Border color.</param>
        public ScreenBorderAttribute(int Left, int Right, int Top, int Bottom, KnownColor BorderColor)
        {
            this.leftMargin = AssertNonNegative(Left);
            this.rightMargin = AssertNonNegative(Right);
            this.topMargin = AssertNonNegative(Top);
            this.bottomMargin = AssertNonNegative(Bottom);
            this.borderColor = Color.FromKnownColor(BorderColor);
        }

        /// <summary>
        /// Left Margin
        /// </summary>
        public int LeftMargin
        {
            get { return this.leftMargin; }
        }

        /// <summary>
        /// Right Margin
        /// </summary>
        public int RightMargin
        {
            get { return this.rightMargin; }
        }

        /// <summary>
        /// Top Margin
        /// </summary>
        public int TopMargin
        {
            get { return this.topMargin; }
        }

        /// <summary>
        /// Bottom Margin
        /// </summary>
        public int BottomMargin
        {
            get { return this.bottomMargin; }
        }

        /// <summary>
        /// Border Color
        /// </summary>
        public Color BorderColor
        {
            get { return this.borderColor; }
        }
    }
}
