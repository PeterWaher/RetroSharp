using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace RetroSharp.Gradients
{
    /// <summary>
    /// Performs an exclusive or operation on a color with the destination background.
    /// </summary>
    public class Xor : ColorAlgorithm
    {
        private int argb;

        /// <summary>
        /// Performs an exclusive or operation on a color with the destination background.
        /// </summary>
        /// <param name="Color">Color to blend with the destination.</param>
        public Xor(Color Color)
        {
            this.argb = Color.ToArgb();
        }

        /// <summary>
        /// Gets the color for a given coordinate given the color of the pixel before coloration.
        /// </summary>
        /// <param name="X">X-coordinate.</param>
        /// <param name="Y">Y-coordinate.</param>
        /// <param name="DestinationColor">Color before coloration.</param>
        /// <returns>Color to use for the corresponding pixel.</returns>
        public override Color GetColor(int X, int Y, Color DestinationColor)
        {
            return Color.FromArgb(this.argb ^ DestinationColor.ToArgb());
        }
    }
}