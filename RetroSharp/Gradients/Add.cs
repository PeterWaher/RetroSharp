using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace RetroSharp.Gradients
{
    /// <summary>
    /// Adds a color to the destination background.
    /// </summary>
    public class Add : ColorAlgorithm
    {
		private double p;
        private byte r;
        private byte g;
        private byte b;

        /// <summary>
        /// Adds a color to the destination background.
        /// </summary>
        /// <param name="Color">Color to add to the destination.</param>
        public Add(Color Color)
        {
            this.r = Color.R;
            this.g = Color.G;
            this.b = Color.B;
            this.p = Color.A / 255.0;
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
            int R = (int)(this.r * this.p + DestinationColor.R + 0.5);
            int G = (int)(this.g * this.p + DestinationColor.G + 0.5);
            int B = (int)(this.b * this.p + DestinationColor.B + 0.5);

            return Color.FromArgb(
				DestinationColor.A,
                R > 255 ? 255 : R,
                G > 255 ? 255 : G,
                B > 255 ? 255 : B);
        }
    }
}