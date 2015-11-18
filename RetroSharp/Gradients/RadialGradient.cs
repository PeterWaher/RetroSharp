using System;
using System.Collections.Generic;
using System.Drawing;

namespace RetroSharp.Gradients
{
    /// <summary>
    /// Radial gradient coloring algorithm.
    /// </summary>
    public class RadialGradient : ColorAlgorithm
    {
        private int x;
        private int y;
        private LinkedList<ColorStop> stops;

        /// <summary>
        /// Radial gradient coloring algorithm. The coloration is done using a radial gradiant centered at
        /// (<paramref name="FromX"/>,<paramref name="FromY"/>) using the color <paramref name="FromColor"/>.
        /// Pixels are colored depending on the distance from (<paramref name="FromX"/>,<paramref name="FromY"/>) 
        /// where the gradient will have color <paramref name="RadiusColor"/> at a distance of <paramref name="Radius"/>.
        /// </summary>
        /// <param name="FromX">X-coordinate of center point.</param>
        /// <param name="FromY">Y-coordinate of center point.</param>
        /// <param name="FromColor">Color at center point.</param>
        /// <param name="Radius">Radius from the center point.</param>
        /// <param name="RadiusColor">Color at the given radius from the center point.</param>
        public RadialGradient(int FromX, int FromY, Color FromColor, int Radius, Color RadiusColor)
        {
            this.x = FromX;
            this.y = FromY;

            this.stops = new LinkedList<ColorStop>();
            this.stops.AddLast(new ColorStop(0, FromColor));
            this.stops.AddLast(new ColorStop(Radius, RadiusColor));
        }

        /// <summary>
        /// Radial gradient coloring algorithm. The coloration is done using a radial gradiant centered at
        /// (<paramref name="FromX"/>,<paramref name="FromY"/>) using the color <paramref name="FromColor"/>.
        /// Pixels are colored depending on the distance from (<paramref name="FromX"/>,<paramref name="FromY"/>) 
        /// where the gradient will have color <paramref name="RadiusColor"/> at a distance of <paramref name="Radius"/>.
        /// </summary>
        /// <param name="FromX">X-coordinate of center point.</param>
        /// <param name="FromY">Y-coordinate of center point.</param>
        /// <param name="FromColor">Color at center point.</param>
        /// <param name="Radius">Radius from the center point.</param>
        /// <param name="RadiusColor">Color at the given radius from the center point.</param>
        /// <param name="Stops">Additional color stops. Must be ordered by increasing <see cref="ColorStop.Stop"/>. Each
        /// <see cref="ColorStop.Stop"/> value represents a radius from th center point.</param>
        public RadialGradient(int FromX, int FromY, Color FromColor, int Radius, Color RadiusColor, params ColorStop[] Stops)
        {
            this.x = FromX;
            this.y = FromY;

            this.stops = new LinkedList<ColorStop>();

            int c = Stops.Length;
            int i = 0;

            while (i < c && Stops[i].Stop < 0)
                i++;

            this.stops.AddLast(new ColorStop(0, FromColor));

            while (i < c && Stops[i].Stop < Radius)
                this.stops.AddLast(Stops[i++]);

            this.stops.AddLast(new ColorStop(Radius, RadiusColor));

            while (i < c)
                this.stops.AddLast(Stops[i++]);
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
            int dx = X - this.x;
            int dy = Y - this.y;
            double t = Math.Sqrt(dx * dx + dy * dy);

            LinkedListNode<ColorStop> Loop = this.stops.First;
            ColorStop Stop0 = null;
            ColorStop Stop1 = Loop.Value;

            Loop = Loop.Next;
            while (Loop != null && t > Stop1.Stop)
            {
                Stop0 = Stop1;
                Stop1 = Loop.Value;
                Loop = Loop.Next;
            }

            if (Stop0 == null)
                return Stop1.Color;

            if (t > Stop1.Stop)
                return Stop1.Color;

            double dt = Stop1.Stop - Stop0.Stop;
            if (dt <= 0)
                return Stop0.Color;

            t = (t - Stop0.Stop) / dt;

            Color c0 = Stop0.Color;
            Color c1 = Stop1.Color;
            double u = 1 - t;

            int R = (int)(c0.R * u + c1.R * t + 0.5);
            int G = (int)(c0.G * u + c1.G * t + 0.5);
            int B = (int)(c0.B * u + c1.B * t + 0.5);
            int A = (int)(c0.A * u + c1.A * t + 0.5);

            return Color.FromArgb(A, R, G, B);
        }
    }
}
