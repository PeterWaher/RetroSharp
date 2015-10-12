using System;
using System.Drawing;

namespace RetroSharp
{
    /// <summary>
    /// Access to the pixels of the raster graphics display.
    /// </summary>
    public class Raster 
    {
        internal int rasterWidth = 320;
        internal int rasterHeight = 200;
        internal int rasterStride = 0;
        internal int rasterClipLeft = 0;
        internal int rasterClipTop = 0;
        internal int rasterClipRight = 319;
        internal int rasterClipBottom = 199;
        internal int rasterBlocksX = 0;
        internal int rasterBlocksY = 0;
        internal byte[] raster = null;
        internal bool[] rasterBlocks = null;

        /// <summary>
        /// Access to the pixels of the raster graphics display.
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <returns>Character value</returns>
        public Color this[int x, int y]
        {
            get
            {
                int w;

                if (x < 0 || x >= (w = this.rasterWidth))
                    return Color.Empty;

                if (y < 0 || y >= this.rasterHeight)
                    return Color.Empty;

                int i = y * this.rasterStride + (x << 2);

                byte R = this.raster[i++];
                byte G = this.raster[i++];
                byte B = this.raster[i++];
                byte A = this.raster[i++];

                return Color.FromArgb(A, R, G, B);
            }

            set
            {
                if (x >= this.rasterClipLeft && y >= this.rasterClipTop && x <= this.rasterClipRight && y <= this.rasterClipBottom)
                {
                    int p = (this.rasterWidth * y + x) << 2;

                    this.raster[p++] = value.R;
                    this.raster[p++] = value.G;
                    this.raster[p++] = value.B;
                    this.raster[p] = value.A;

                    this.rasterBlocks[(y / RetroApplication.RasterBlockSize) * this.rasterBlocksX + (x / RetroApplication.RasterBlockSize)] = true;
                }
            }
        }
    }
}
