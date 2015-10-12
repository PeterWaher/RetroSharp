using System;
using System.Collections.Generic;
using System.Drawing;

namespace RetroSharp
{
    /// <summary>
    /// Defines a color stop in a coloration algorithm.
    /// </summary>
    public class ColorStop
    {
        private double t;
        private Color color;

        /// <summary>
        /// Defines a color stop in a coloration algorithm.
        /// </summary>
        /// <param name="Stop">Interpolation value.</param>
        /// <param name="Color">Color</param>
        public ColorStop(double Stop, Color Color)
        {
            this.t = Stop;
            this.color = Color;
        }

        /// <summary>
        /// Interpolation value.
        /// </summary>
        public double Stop
        { 
            get { return this.t; } 
        }

        /// <summary>
        /// Color
        /// </summary>
        public Color Color
        {
            get { return this.color; }
        }
    }
}
