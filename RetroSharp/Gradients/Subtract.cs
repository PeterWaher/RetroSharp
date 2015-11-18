using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace RetroSharp.Gradients
{
    /// <summary>
    /// Subtracts a color from the destination background.
    /// </summary>
    public class Subtract : ColorAlgorithm
    {
		private double p;
        private byte r;
        private byte g;
        private byte b;

        /// <summary>
        /// Subtracts a color from the destination background.
        /// </summary>
        /// <param name="Color">Color to add to the destination.</param>
        public Subtract(Color Color)
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
			int R = (int)(DestinationColor.R - this.r * this.p + 0.5);
			int G = (int)(DestinationColor.G - this.g * this.p + 0.5);
			int B = (int)(DestinationColor.B - this.b * this.p + 0.5);

            return Color.FromArgb(
				DestinationColor.A,
                R < 0 ? 0 : R,
                G < 0 ? 0 : G,
                B < 0 ? 0 : B);
        }
    }
}