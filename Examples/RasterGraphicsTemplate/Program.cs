using System;
using System.Drawing;
using RetroSharp;

// This is a template for retro applications using a raster graphics-based screen by default.

namespace RasterGraphicsTemplate
{
    [RasterGraphics(320, 200)]
    [ScreenBorder(30, 20)]
    class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            // Enter code here.

            Terminate();
        }
    }
}