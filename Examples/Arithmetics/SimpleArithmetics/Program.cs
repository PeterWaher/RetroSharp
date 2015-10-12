using RetroSharp;

// This application shows how simple arithmetics can be done.

namespace SimpleArithmetics
{
    [CharacterSet("Consolas", 256)]
    [Characters(80, 32)]
    [ScreenBorder(30, 20)]
    class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            WriteLine("Enter three numbers A, B and C below.");
            WriteLine();

            Write("A: ");
            double A = ToDouble(ReadLine());

            Write("B: ");
            double B = ToDouble(ReadLine());

            Write("C: ");
            double C = ToDouble(ReadLine());

            WriteLine();

            Write("A + B = ");
            WriteLine(A + B);

            Write("A - B = ");
            WriteLine(A - B);

            Write("A * B = ");
            WriteLine(A * B);

            Write("A / B = ");
            WriteLine(A / B);

            Write("A + B * C = ");
            WriteLine(A + B * C);

            Write("A + (B * C) = ");
            WriteLine(A + (B * C));

            Write("(A + B) * C = ");
            WriteLine((A + B) * C);

            WriteLine();
            WriteLine("Press ENTER to continue.");
            ReadLine();

            Terminate();
        }
    }
}