using System;
using System.Collections.Generic;
using System.Drawing;

namespace RetroSharp.Gradients
{
    /// <summary>
    /// Abstract base class for coloring algorithms.
    /// </summary>
    public abstract class ColorAlgorithm
    {
        /// <summary>
        /// Abstract base class for coloring algorithms.
        /// </summary>
        public ColorAlgorithm()
        {
        }

        /// <summary>
        /// Gets the color for a given coordinate given the color of the pixel before coloration.
        /// </summary>
        /// <param name="X">X-coordinate.</param>
        /// <param name="Y">Y-coordinate.</param>
        /// <param name="DestinationColor">Color before coloration.</param>
        /// <returns>Color to use for the corresponding pixel.</returns>
        public abstract Color GetColor(int X, int Y, Color DestinationColor);
    }
}
