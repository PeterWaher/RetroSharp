using RetroSharp;

// This is a template for retro applications using a character-based screen by default.

namespace CharacterTemplate
{
    [CharacterSet("Consolas", 256)]
    [Characters(80, 30)]
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