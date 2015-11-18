using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace RetroSharp.Gradients
{
    /// <summary>
    /// Colors the destination by repeating a bitmapped texture along the x and y axes.
    /// </summary>
    public class TextureFill : ColorAlgorithm
    {
        private int width;
        private int height;
        private int xOffset;
        private int yOffset;
        private int stride;
        private int size;
        private byte[] rgba;

        /// <summary>
        /// Colors the destination by repeating a bitmapped texture along the x and y axes.
        /// </summary>
        /// <param name="Texture">Texture</param>
        public TextureFill(Bitmap Texture)
            : this(Texture, 0, 0)
        {
        }

        /// <summary>
        /// Colors the destination by repeating a bitmapped texture along the x and y axes.
        /// </summary>
        /// <param name="Texture">Texture</param>
        /// <param name="OffsetX">Offset along the X-axis.</param>
        /// <param name="OffsetY">Offset along the Y-axis.</param>
        public TextureFill(Bitmap Texture, int OffsetX, int OffsetY)
        {
            this.width = Texture.Width;
            this.height = Texture.Height;
            this.xOffset = OffsetX;
            this.yOffset = OffsetY;

            BitmapData data = Texture.LockBits(new Rectangle(0, 0, this.width, this.height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            this.stride = data.Stride;
            this.size = this.stride * this.height;
            this.rgba = new byte[this.size];

            Marshal.Copy(data.Scan0, this.rgba, 0, this.size);

            Texture.UnlockBits(data);
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
            X = (X - this.xOffset) % this.width;
            Y = (Y - this.yOffset) % this.height;

            int i = (Y * this.width + X) << 2;
            byte B = this.rgba[i++];
            byte G = this.rgba[i++];
            byte R = this.rgba[i++];
            byte A = this.rgba[i];

            return Color.FromArgb(A, R, G, B);
        }
    }
}
