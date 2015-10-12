using RetroSharp;

namespace ConsoleApplication
{
    [CharacterSet("Consolas", 256)]
    [Characters(80, 30)]
    [ScreenBorder(30,20)]
    class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            int Number = Random(1, 100);
            int Guess = 0;
            string s;

            WriteLine("Hello");
            do
            {
                Write("Guess a number between 1 and 100: ");
                s = ReadLine();

                if (!int.TryParse(s, out Guess))
                    WriteLine("That is not an integer number.");

                else if (Guess < Number)
                    WriteLine("Too low.");

                else if (Guess > Number)
                    WriteLine("Too high.");
            }
            while (Guess != Number);

            WriteLine("Well done. Press ENTER to continue.");
            ReadLine();

            Terminate();
        }
    }
}