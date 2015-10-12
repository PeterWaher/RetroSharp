using RetroSharp;

// This example is a classical first example. It shows how to display text on the screen
// and wait for user input.

namespace HelloWorld
{
    [CharacterSet("Consolas", 256)]
    [Characters(80, 30)]
    [ScreenBorder(30, 20)]
    class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            WriteLine("Hello World!");

            WriteLine();
            WriteLine();
            WriteLine();
            WriteLine("Press ENTER to continue.");
            ReadLine();

            Terminate();
        }
    }
}