using System;
using System.Collections.Generic;
using System.Text;

namespace RetroSharp
{
    public delegate void ElapsedTimeEventHandler(object Sender, ElapsedTimeEventArgs e);

    /// <summary>
    /// Event arguments for recurrent and timed event handlers.
    /// </summary>
    public class ElapsedTimeEventArgs : EventArgs
    {
        private double seconds;

        /// <summary>
        /// Event arguments for recurrent and timed event handlers.
        /// </summary>
        /// <param name="Seconds">Seconds elapsed since last event.</param>
        public ElapsedTimeEventArgs(double Seconds)
        {
            this.seconds = Seconds;
        }

        /// <summary>
        /// Seconds elapsed since last event.
        /// </summary>
        public double Seconds
        {
            get { return this.seconds; }
        }

        /// <summary>
        /// Total number of seconds elapsed since start of application.
        /// </summary>
        public double TotalSeconds
        {
            get { return RetroApplication.TotalTime; }
        }

    }
}
