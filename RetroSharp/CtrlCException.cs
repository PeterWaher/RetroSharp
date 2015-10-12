using System;
using System.Collections.Generic;
using System.Text;

namespace RetroSharp
{
    /// <summary>
    /// Exception thrown when CTRL+C was pressed during console input.
    /// </summary>
    public class CtrlCException : Exception
    {
        public CtrlCException()
            : base("CTRL-C pressed.")
        {
        }
    }
}
