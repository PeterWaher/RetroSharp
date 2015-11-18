using System;
using System.Collections.Generic;
using System.Drawing;

namespace RetroSharp.Gradients
{
    /// <summary>
    /// Linear gradient coloring algorithm.
    /// </summary>
    public class LinearGradient : ColorAlgorithm
    {
        private int fromX;
        private int fromY;
        private int toX;
        private int toY;
        private LinkedList<ColorStop> stops;
        private double cx, cy;

        /// <summary>
        /// Linear gradient coloring algorithm. The coloration is done using a linear gradiant starting at
        /// (<paramref name="FromX"/>,<paramref name="FromY"/>) using the color <paramref name="FromColor"/>
        /// along a line to (<paramref name="ToX"/>,<paramref name="ToY"/>) where the gradient will have
        /// color <paramref name="ToColor"/>. Lines perpendicular to this line will have the same color.
        /// </summary>
        /// <param name="FromX">X-coordinate of first reference point.</param>
        /// <param name="FromY">Y-coordinate of first reference point.</param>
        /// <param name="FromColor">Color at first reference point.</param>
        /// <param name="ToX">X-coordinate of second reference point.</param>
        /// <param name="ToY">Y-coordinate of second reference point.</param>
        /// <param name="ToColor">Color at second reference point.</param>
        public LinearGradient(int FromX, int FromY, Color FromColor, int ToX, int ToY, Color ToColor)
        {
            this.fromX = FromX;
            this.fromY = FromY;
            this.toX = ToX;
            this.toY = ToY;

            this.stops = new LinkedList<ColorStop>();
            this.stops.AddLast(new ColorStop(0, FromColor));
            this.stops.AddLast(new ColorStop(1, ToColor));

            this.Init();
        }

        /// <summary>
        /// Linear gradient coloring algorithm. The coloration is done using a linear gradiant starting at
        /// (<paramref name="FromX"/>,<paramref name="FromY"/>) using the color <paramref name="FromColor"/>
        /// along a line to (<paramref name="ToX"/>,<paramref name="ToY"/>) where the gradient will have
        /// color <paramref name="ToColor"/>. Lines perpendicular to this line will have the same color.
        /// This version allows for the insertion of different color stops along the gradient. Each
        /// stop has a corresponding floating point value. Value 0.0 corresponds to 
        /// (<paramref name="FromX"/>,<paramref name="FromY"/>). Value 1.0 corresponds to
        /// (<paramref name="ToX"/>,<paramref name="ToY"/>). It is possible to add stops both with
        /// negative values or with values above 1.0.
        /// </summary>
        /// <param name="FromX">X-coordinate of first reference point.</param>
        /// <param name="FromY">Y-coordinate of first reference point.</param>
        /// <param name="FromColor">Color at first reference point.</param>
        /// <param name="ToX">X-coordinate of second reference point.</param>
        /// <param name="ToY">Y-coordinate of second reference point.</param>
        /// <param name="ToColor">Color at second reference point.</param>
        /// <param name="Stops">Additional color stops. Must be ordered by increasing <see cref="ColorStop.Stop"/>.</param>
        public LinearGradient(int FromX, int FromY, Color FromColor, int ToX, int ToY, Color ToColor, params ColorStop[] Stops)
        {
            this.fromX = FromX;
            this.fromY = FromY;
            this.toX = ToX;
            this.toY = ToY;

            this.stops = new LinkedList<ColorStop>();

            int c = Stops.Length;
            int i = 0;

            while (i < c && Stops[i].Stop < 0)
                this.stops.AddLast(Stops[i++]);

            this.stops.AddLast(new ColorStop(0, FromColor));

            while (i < c && Stops[i].Stop < 1)
                this.stops.AddLast(Stops[i++]);

            this.stops.AddLast(new ColorStop(1, ToColor));

            while (i < c)
                this.stops.AddLast(Stops[i++]);

            this.Init();
        }

        private void Init()
        {
            double dx = this.toX - this.fromX;
            double dy = this.toY - this.fromY;
            double l = dx * dx + dy * dy;

            if (l == 0)
            {
                this.cx = 0;
                this.cy = 0;
            }
            else
            {
                this.cx = dx / l;
                this.cy = dy / l;
            }
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
            double t = (X - this.fromX) * this.cx + (Y - this.fromY) * this.cy;
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
