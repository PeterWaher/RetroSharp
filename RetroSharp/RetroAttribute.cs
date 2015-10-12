using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RetroSharp
{
    /// <summary>
    /// Base class for retro attributes.
    /// </summary>
    public abstract class RetroAttribute : Attribute
    {
        protected static int AssertNonNegative(int i)
        {
            if (i < 0)
                throw new Exception("Value cannot be negative.");

            return i;
        }

        protected static int AssertPositive(int i)
        {
            if (i <= 0)
                throw new Exception("Value must be positive.");

            return i;
        }
    }
}
