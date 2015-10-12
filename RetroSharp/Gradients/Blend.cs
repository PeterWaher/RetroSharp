using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace RetroSharp.Gradients
{
    /// <summary>
    /// Blends a color with the destination background.
    /// </summary>
    public class Blend : ColorAlgorithm
    {
        private double p;
        private double u;
        private byte r;
        private byte g;
        private byte b;
        private byte a;

        /// <summary>
        /// Blends a color with the destination background.
        /// </summary>
        /// <param name="Color">Color to blend with the destination.</param>
        /// <param name="p">Blending coefficient. 0 = No blending. 1 = Opaque.</param>
        public Blend(Color Color, double p)
        {
            this.p = p;
            this.u = 1 - p;
            this.r = Color.R;
            this.g = Color.G;
            this.b = Color.B;
            this.a = Color.A;
        }

        /// <summary>
        /// Gets the color for a given coordinate and the color of the pixel before coloration.
        /// </summary>
        /// <param name="X">X-coordinate.</param>
        /// <param name="Y">Y-coordinate.</param>
        /// <param name="DestinationColor">Color before coloration.</param>
        /// <returns>Color to use for the corresponding pixel.</returns>
        public override Color GetColor(int X, int Y, Color DestinationColor)
        {
            int R = (int)(this.r * this.p + DestinationColor.R * this.u + 0.5);
            int G = (int)(this.g * this.p + DestinationColor.G * this.u + 0.5);
            int B = (int)(this.b * this.p + DestinationColor.B * this.u + 0.5);
            int A = (int)(this.a * this.p + DestinationColor.A * this.u + 0.5);

            return Color.FromArgb(
                A < 0 ? 0 : A > 255 ? 255 : A,
                R < 0 ? 0 : R > 255 ? 255 : R,
                G < 0 ? 0 : G > 255 ? 255 : G,
                B < 0 ? 0 : B > 255 ? 255 : B);
        }
    }
}