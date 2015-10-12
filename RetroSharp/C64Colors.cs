using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RetroSharp
{
    public static class C64Colors
    {
        public static readonly Color Black = Color.FromArgb(0, 0, 0);
        public static readonly Color White = Color.FromArgb(255, 255, 255);
        public static readonly Color Red = Color.FromArgb(224, 64, 64);
        public static readonly Color Cyan = Color.FromArgb(96, 255, 255);
        public static readonly Color Magenta = Color.FromArgb(224, 96, 224);
        public static readonly Color Green = Color.FromArgb(64, 224, 64);
        public static readonly Color Blue = Color.FromArgb(64, 64, 224);
        public static readonly Color Yellow = Color.FromArgb(255, 255, 64);
        public static readonly Color Orange = Color.FromArgb(224, 160, 64);
        public static readonly Color Brown = Color.FromArgb(156, 116, 72);
        public static readonly Color Pink = Color.FromArgb(255, 160, 160);
        public static readonly Color DarkGrey = Color.FromArgb(84, 84, 84);
        public static readonly Color Grey = Color.FromArgb(136, 136, 136);
        public static readonly Color LightGreen = Color.FromArgb(160, 255, 160);
        public static readonly Color LightBlue = Color.FromArgb(160, 160, 255);
        public static readonly Color LightGrey = Color.FromArgb(192, 192, 192);
        public static readonly Color[] Palette = new Color[] { Black, White, Red, Cyan, Magenta, Green, Blue, Yellow, Orange, Brown, Pink, DarkGrey, Grey, LightGreen, LightBlue, LightGrey };
    }
}
