using System;
using System.Collections.Generic;
using System.Drawing;
using RetroSharp;

// This application is a simple implementation of the classic Asteroids game.
//
// The application introduces:
// * Animated raster graphics
// * Vector graphics
// * Simple vector hit tests
// * Combination of console and raster graphics
// * Simple windowed operations

namespace Asteroids
{
    [RasterGraphics(640, 480)]
    [ScreenBorder(30, 20, KnownColor.DarkGray)]
    [AspectRatio(4, 3)]
    [Characters(40, 30, KnownColor.White)]
    public class Program : RetroApplication
    {
        public static void Main(string[] args)
        {
            Initialize();

            RotatingObject Ship = null;
            LinkedList<RotatingObject> Asteroids = new LinkedList<RotatingObject>();
            LinkedList<Particle> Particles = new LinkedList<Particle>();
            LinkedList<Particle> Shots = new LinkedList<Particle>();
            double ToRadians = Math.PI / 180;
            double d;
            int i, j, k;
            int[] Radiuses;
            int[] Angles;
            int Level = 0;
            int Lives = 3;
            int Score = 0;
            int TeleportsLeft = 3;
            int HighScore = 0;
            int HighLevel = 0;
            double ImmortalSecondsLeft = 0;
            double NewShipSecondsLeft = 0;
            bool Collision;
            bool Done = false;
            bool GameOver = false;

            // Source of sound: http://www.freesound.org/people/jobro/sounds/35686/
            int ShotSound = UploadAudioSample(GetResourceWavAudio("35686__jobro__laser9.wav"));

            // Source of sound: http://www.freesound.org/people/Tony%20B%20kksm/sounds/80938/
            int AsteroidHitSound = UploadAudioSample(GetResourceWavAudio("80938__tony-b-kksm__soft-explosion.wav"));

            // Source of sound: http://www.freesound.org/people/sandyrb/sounds/35643/
            int ShipExplodeSound = UploadAudioSample(GetResourceWavAudio("35643__sandyrb__usat-bomb.wav"));

            // Source of sound: http://www.freesound.org/people/fins/sounds/172207/
            int TeleportSound = UploadAudioSample(GetResourceWavAudio("172207__fins__teleport.wav"));

            // Source of sound: http://www.freesound.org/people/fins/sounds/133284/
            int LevelCompletedSound = UploadAudioSample(GetResourceWavAudio("133284__fins__level-completed.wav"));

            // Source of sound: http://www.freesound.org/people/fins/sounds/133283/
            int GameOverSound = UploadAudioSample(GetResourceWavAudio("133283__fins__game-over.wav"));

            OpenConsoleWindow(4, 4, 35, 25, "Welcome to Asteroids.");

            WriteLine();
            WriteLine("Keys in the game:");
            WriteLine();
            WriteLine("Left: Turn ship left");
            WriteLine("Right: Turn ship right");
            WriteLine("Up: Accelerate forwards");
            WriteLine("Down: Teleport to safe location");
            WriteLine("Space: Fire");
            WriteLine("Esc: Close application");
            WriteLine("Q: Quit current game");
            WriteLine();
            WriteLine("To play, press ENTER.");

            ReadLine();
            ClearConsoleWindowArea();
            Clear();

            Write("Ships: ");
            int NrShipsPos = CursorX;

            ForegroundColor = Color.Salmon;
            Write(Lives.ToString("D2"));
            ForegroundColor = Color.White;

            Write(" Hyper: ");
            int NrTeleportsPos = CursorX;

            ForegroundColor = Color.Salmon;
            Write(TeleportsLeft.ToString("D2"));
            ForegroundColor = Color.White;

            Write(" Lvl: ");
            int LevelPos = CursorX;

            ForegroundColor = Color.Salmon;
            Write(Level.ToString("D2"));
            ForegroundColor = Color.White;

            Write(" Score: ");
            int ScorePos = CursorX;

            ForegroundColor = Color.Salmon;
            Write(Score.ToString("D5"));

            OnUpdateModel += (s, e) =>
                {
                    if (GameOver)
                        return;

                    double ElapsedSeconds = e.Seconds;

                    if (Asteroids.First == null)
                    {
                        ImmortalSecondsLeft = 4;

                        Level++;
                        GotoXY(LevelPos, 0);
                        Write(Level.ToString("D2"));

                        for (i = 0; i < 5 + 5 * Level; i++)
                        {
                            Radiuses = new int[16];
                            Angles = new int[16];

                            for (j = 0; j < 16; j++)
                            {
                                Radiuses[j] = Random(16, 32);
                                Angles[j] = Random(0, 359);
                            }

                            Array.Sort<int>(Angles);

                            j = Random(0, 359);
                            k = Random(200, 400);

                            Asteroids.AddLast(new RotatingObject(Radiuses, Angles,
                                320 + Math.Cos(j * ToRadians) * k,
                                240 + Math.Sin(j * ToRadians) * k,
                                60 * Random() - 30,
                                60 * Random() - 30,
                                0, 200 * Random() - 100));
                        }

                        if (Level > 1)
                            PlayAudioSample(LevelCompletedSound);
                    }

                    foreach (RotatingObject Obj in Asteroids)
                    {
                        Obj.Draw(Color.Black);
                        Obj.Move(ElapsedSeconds);
                        Obj.Draw(Color.White);

                        if (Ship != null && ImmortalSecondsLeft <= 0 && IntersectsPolygon(Ship.Points, Obj.Points))
                        {
                            Ship.Draw(Color.Black);

                            for (i = 0; i < 200; i++)
                            {
                                d = Random() * 360 * ToRadians;
                                j = Random(10, 100);

                                Particles.AddLast(new Particle(
                                    Ship.X,  // X
                                    Ship.Y,  // Y
                                    Ship.VelocityX + Math.Cos(d) * j,                       // Velocity X
                                    Ship.VelocityY + Math.Sin(d) * j,                       // Velocity Y
                                    Blend(Color.Blue, Color.White, Random()),
                                    4));
                            }

                            Ship = null;
                            NewShipSecondsLeft = 4;

                            PlayAudioSample(ShipExplodeSound);
                        }
                    }

                    if (Ship != null)
                    {
                        Ship.Draw(Color.Black);

                        if (IsPressed(KeyCode.Left))
                            Ship.Angle -= 3;

                        if (IsPressed(KeyCode.Right))
                            Ship.Angle += 3;

                        if (IsPressed(KeyCode.Up))
                        {
                            Ship.VelocityX += Math.Cos(Ship.Angle * ToRadians) * 4;
                            Ship.VelocityY += Math.Sin(Ship.Angle * ToRadians) * 4;

                            d = (Random() * 30 - 15 + Ship.Angle + 180) * ToRadians;    // Particle direction
                            Particles.AddLast(new Particle(
                                Ship.X + Math.Cos((Ship.Angle + 180) * ToRadians) * 5,  // X
                                Ship.Y + Math.Sin((Ship.Angle + 180) * ToRadians) * 5,  // Y
                                Ship.VelocityX + Math.Cos(d) * 100,                       // Velocity X
                                Ship.VelocityY + Math.Sin(d) * 100,                       // Velocity Y
                                Blend(Color.Yellow, Color.Orange, Random()),
                                2));
                        }

                        if (TeleportsLeft > 0 && ImmortalSecondsLeft <= 0 && IsPressed(KeyCode.Down))
                        {
                            do
                            {
                                Ship.X = Random(50, RasterWidth - 50);
                                Ship.Y = Random(50, RasterHeight - 50);
                                Ship.VelocityX = 0;
                                Ship.VelocityY = 0;

                                Ship.CalcPoints();

                                Collision = false;

                                foreach (RotatingObject Obj in Asteroids)
                                {
                                    if (IntersectsPolygon(Obj.Points, Ship.Points))
                                    {
                                        Collision = true;
                                        break;
                                    }
                                }
                            }
                            while (Collision);

                            ImmortalSecondsLeft = 4;

                            PlayAudioSample(TeleportSound);

                            TeleportsLeft--;
                            GotoXY(NrTeleportsPos, 0);
                            Write(TeleportsLeft.ToString("D2"));
                        }

                        Ship.Move(ElapsedSeconds);

                        if (ImmortalSecondsLeft > 0)
                        {
                            ImmortalSecondsLeft -= ElapsedSeconds;

                            if (Math.IEEERemainder(ImmortalSecondsLeft, 0.5) < 0)
                                Ship.Draw(Blend(Color.Blue, Color.White, 0.5));
                            else
                                Ship.Draw(Color.White);
                        }
                        else
                            Ship.Draw(Color.White);
                    }
                    else
                    {
                        NewShipSecondsLeft -= ElapsedSeconds;

                        if (NewShipSecondsLeft <= 0 && Lives > 0)
                        {
                            Ship = new RotatingObject(new int[] { 15, 15, 5, 15 }, new int[] { 0, 135, 180, 225 }, 320, 240, 0, 0, 270, 0);
                            ImmortalSecondsLeft = 4;

                            Lives--;
                            GotoXY(NrShipsPos, 0);
                            Write(Lives.ToString("D2"));
                        }
                        else if (Lives == 0)
                        {
                            Lives--;
                            PlayAudioSample(GameOverSound);
                        }
                        else if (NewShipSecondsLeft < -1.5 && Lives == -1)
                        {
                            Lives--;
                            GameOver = true;
                        }
                    }

                    LinkedListNode<Particle> ParticleNode = Particles.First;
                    LinkedListNode<Particle> Next;
                    Particle Particle;

                    while (ParticleNode != null)
                    {
                        Particle = ParticleNode.Value;

                        if (Particle.Draw(Color.Black))
                        {
                            Particle.Move(ElapsedSeconds);
                            Particle.Draw(Particle.Color);
                            ParticleNode = ParticleNode.Next;
                        }
                        else
                        {
                            Next = ParticleNode.Next;
                            Particles.Remove(ParticleNode);
                            ParticleNode = Next;
                        }
                    }

                    LinkedListNode<Particle> ShotNode = Shots.First;
                    LinkedListNode<RotatingObject> AsteroidNode;
                    RotatingObject Asteroid;
                    Particle Shot;
                    int x0, y0, x1, y1;
                    double vx, vy;

                    while (ShotNode != null)
                    {
                        Shot = ShotNode.Value;

                        if (Shot.Draw(Color.Black))
                        {
                            Shot.Move(ElapsedSeconds);
                            x0 = (int)(Shot.PrevX + 0.5);
                            y0 = (int)(Shot.PrevY + 0.5);
                            x1 = (int)(Shot.X + 0.5);
                            y1 = (int)(Shot.Y + 0.5);

                            AsteroidNode = Asteroids.First;
                            while (AsteroidNode != null)
                            {
                                Asteroid = AsteroidNode.Value;

                                if (IntersectsPolygon(x0, y0, x1, y1, Asteroid.Points))
                                {
                                    i = Asteroid.Points.Length;

                                    if (i > 4)
                                    {
                                        i /= 2;

                                        for (k = 0; k < 3; k++)
                                        {
                                            Radiuses = new int[i];
                                            Angles = new int[i];

                                            for (j = 0; j < i; j++)
                                            {
                                                Radiuses[j] = Random(i, i << 1);
                                                Angles[j] = Random(0, 359);
                                            }

                                            Array.Sort<int>(Angles);

                                            j = Random(0, 359);

                                            do
                                            {
                                                vx = Asteroid.VelocityX + 100 * Random() - 50;
                                            }
                                            while (Math.Abs(vx) < 5);   // To avoid slow moving objects hidden by the border.

                                            do
                                            {
                                                vy = Asteroid.VelocityY + 100 * Random() - 50;
                                            }
                                            while (Math.Abs(vy) < 5);   // To avoid slow moving objects hidden by the border.

                                            Asteroids.AddLast(new RotatingObject(Radiuses, Angles,
                                                Asteroid.X,
                                                Asteroid.Y,
                                                vx,
                                                vy,
                                                Asteroid.Angle, Asteroid.VelocityAngle + 200 * Random() - 100));
                                        }
                                    }

                                    for (i = 0; i < 30; i++)
                                    {
                                        d = Random() * 360 * ToRadians;
                                        j = Random(10, 100);

                                        Particles.AddLast(new Particle(
                                            Asteroid.X,  // X
                                            Asteroid.Y,  // Y
                                            Asteroid.VelocityX + Math.Cos(d) * j,                       // Velocity X
                                            Asteroid.VelocityY + Math.Sin(d) * j,                       // Velocity Y
                                            Blend(Color.Red, Color.Yellow, Random()),
                                            2));
                                    }

                                    Asteroid.Draw(Color.Black);
                                    Asteroids.Remove(AsteroidNode);

                                    PlayAudioSample(AsteroidHitSound);

                                    Score++;
                                    GotoXY(ScorePos, 0);
                                    Write(Score.ToString("D5"));

                                    break;
                                }
                                else
                                    AsteroidNode = AsteroidNode.Next;
                            }

                            if (AsteroidNode == null)
                            {
                                Shot.Draw(Shot.Color);
                                ShotNode = ShotNode.Next;
                            }
                            else
                            {
                                Next = ShotNode.Next;
                                Shots.Remove(ShotNode);
                                ShotNode = Next;
                            }
                        }
                        else
                        {
                            Next = ShotNode.Next;
                            Shots.Remove(ShotNode);
                            ShotNode = Next;
                        }
                    }
                };

            OnKeyPressed += (s, e) =>
                {
                    switch (e.Character)
                    {
                        case '\x1b':
                            Done = true;
                            break;

                        case ' ':
                            if (Ship != null)
                            {
                                d = Ship.Angle * ToRadians;                 // Shot direction
                                Shots.AddLast(new Particle(
                                    Ship.X + Math.Cos(d) * 15,              // X
                                    Ship.Y + Math.Sin(d) * 15,              // Y
                                    Ship.VelocityX + Math.Cos(d) * 200,     // Velocity X
                                    Ship.VelocityY + Math.Sin(d) * 200,     // Velocity Y
                                    Color.White,
                                    3));

                                PlayAudioSample(ShotSound);
                            }
                            break;

                        case 'q':
                        case 'Q':
                            GameOver = true;
                            PlayAudioSample(GameOverSound);
                            break;
                    }
                };

            while (!Done)
            {
                while (!GameOver && !Done)
                {
                    Sleep(10);
                }

                if (!Done)
                {
                    ForegroundColor = Color.White;
                    OpenConsoleWindow(4, 4, 35, 25, "Game Over!");

                    WriteLine();
                    Write("Level: ");
                    ForegroundColor = Color.Salmon;
                    WriteLine(Level.ToString("D2"));
                    ForegroundColor = Color.White;

                    Write("Score: ");
                    ForegroundColor = Color.Salmon;
                    WriteLine(Score.ToString("D5"));
                    ForegroundColor = Color.White;

                    if (Score > HighScore)
                    {
                        WriteLine();
                        WriteLine("New High Score!");

                        HighScore = Score;
                        HighLevel = Level;
                    }

                    WriteLine();
                    Write("Highest Level: ");
                    ForegroundColor = Color.Salmon;
                    WriteLine(HighLevel.ToString("D2"));
                    ForegroundColor = Color.White;

                    Write("Highest Score: ");
                    ForegroundColor = Color.Salmon;
                    WriteLine(HighScore.ToString("D5"));
                    ForegroundColor = Color.White;

                    WriteLine();
                    WriteLine("To play again, press ENTER.");
                    WriteLine("To quit, press CTRL+C.");

                    ReadLine();
                    ClearConsoleWindowArea();
                    Clear();

                    Asteroids.Clear();
                    Particles.Clear();
                    Shots.Clear();
                    Level = 0;
                    Lives = 3;
                    Score = 0;
                    TeleportsLeft = 3;
                    Ship = null;
                    GameOver = false;

                    Write("Ships: ");
                    ForegroundColor = Color.Salmon;
                    Write(Lives.ToString("D2"));
                    ForegroundColor = Color.White;

                    Write(" Hyper: ");
                    ForegroundColor = Color.Salmon;
                    Write(TeleportsLeft.ToString("D2"));
                    ForegroundColor = Color.White;

                    Write(" Lvl: ");
                    ForegroundColor = Color.Salmon;
                    Write(Level.ToString("D2"));
                    ForegroundColor = Color.White;

                    Write(" Score: ");
                    ForegroundColor = Color.Salmon;
                    Write(Score.ToString("D5"));
                }
            }

            Terminate();
        }

        public static void OpenConsoleWindow(int x1, int y1, int x2, int y2, string Title)
        {
            int RasterX1 = x1 * RasterWidth / ConsoleWidth - 16;
            int RasterX2 = (x2 + 1) * RasterWidth / ConsoleWidth + 16;
            int RasterY1 = y1 * RasterHeight / ConsoleHeight - 16;
            int RasterY2 = (y2 + 1) * RasterHeight / ConsoleHeight + 16;

            DrawWindow(RasterX1, RasterY1, RasterX2, RasterY2);
            SetConsoleWindowArea(x1 - 1, y1 - 1, x2 + 1, y2 + 1);
            ClearConsole();

            SetConsoleWindowArea(x1, y1, x2, y2);
            WriteLine(Title);

            SetConsoleWindowArea(x1, y1 + 2, x2, y2);
        }

        public static void DrawWindow(int x1, int y1, int x2, int y2)
        {
            int i;

            SetClipArea(x1, y1, x2, y1 + 39);
            FillRoundedRectangle(x1, y1, x2, y2, 10, 10, Blend(Color.FromArgb(0, 160, 128), 0.8));

            SetClipArea(x1, y1 + 40, x2, y2);
            FillRoundedRectangle(x1, y1, x2, y2, 10, 10, Blend(Color.FromArgb(0, 0, 128), 0.8));

            ClearClipArea();

            for (i = 0; i < 3; i++)
            {
                DrawRoundedRectangle(x1, y1, x2, y2, 10, 10, Color.White);

                x1++;
                x2--;

                DrawRoundedRectangle(x1, y1, x2, y2, 10, 10, Color.White);

                y1++;
                y2--;

                x1--;
                x2++;

                DrawRoundedRectangle(x1, y1, x2, y2, 10, 10, Color.White);

                x1++;
                x2--;
            }
        }

        public abstract class MovingObject
        {
            public double X;
            public double Y;
            public double PrevX;
            public double PrevY;
            public double VelocityX;
            public double VelocityY;

            public MovingObject(double X, double Y, double VelocityX, double VelocityY)
            {
                this.X = this.PrevX = X;
                this.Y = this.PrevY = Y;
                this.VelocityX = VelocityX;
                this.VelocityY = VelocityY;
            }

            public virtual void Move(double ElapsedSeconds)
            {
                this.X += this.VelocityX * ElapsedSeconds;
                this.Y += this.VelocityY * ElapsedSeconds;

                if (this.X < -30)
                    this.X += RasterWidth + 60;

                if (this.X > RasterWidth + 30)
                    this.X -= RasterWidth + 60;

                if (this.Y < -30)
                    this.Y += RasterHeight + 60;

                if (this.Y > RasterHeight + 30)
                    this.Y -= RasterHeight + 60;
            }

            public abstract bool Draw(Color Color);
        }

        public class RotatingObject : MovingObject
        {
            public double VelocityAngle;
            public double Angle;
            public Point[] Points;
            private int[] radiuses;
            private int[] angles;
            private int c;
            private int maxRadius;
            private bool recalc;

            public RotatingObject(int[] Radius, int[] Angles, double X, double Y, double VelocityX, double VelocityY, double Angle, double VelocityAngle)
                : base(X, Y, VelocityX, VelocityY)
            {
                this.radiuses = Radius;
                this.angles = Angles;

                c = Radius.Length;
                this.Points = new Point[c];
                this.recalc = true;

                this.Angle = Angle;
                this.VelocityAngle = VelocityAngle;

                this.maxRadius = 0;
                foreach (int R in Radius)
                {
                    if (R > this.maxRadius)
                        this.maxRadius = R;
                }
            }

            public override void Move(double ElapsedSeconds)
            {
                base.Move(ElapsedSeconds);

                this.Angle = Math.IEEERemainder(this.Angle + this.VelocityAngle * ElapsedSeconds, 360);
                this.recalc = true;
            }

            public void CalcPoints()
            {
                double ToRadians = Math.PI / 180.0;
                double r, a;
                int i;

                for (i = 0; i < c; i++)
                {
                    r = radiuses[i];
                    a = (angles[i] + Angle) * ToRadians;
                    this.Points[i] = new Point((int)(r * Math.Cos(a) + X + 0.5), (int)(r * Math.Sin(a) + Y + 0.5));
                }

                this.recalc = false;
            }

            public override bool Draw(Color Color)
            {
                if (this.recalc)
                    this.CalcPoints();

                DrawPolygon(this.Points, Color);

                return true;
            }

            public bool Hits(double X, double Y)
            {
                X -= this.X;
                Y -= this.Y;

                if (Math.Abs(X) > this.maxRadius || Math.Abs(Y) > this.maxRadius)
                    return false;

                return true;
            }
        }

        public class Particle : MovingObject
        {
            public Color Color;
            private double Lifetime;
            private double TotalTime;
            private double p = 1;

            public Particle(double X, double Y, double VelocityX, double VelocityY, Color Color, double Lifetime)
                : base(X, Y, VelocityX, VelocityY)
            {
                this.Color = Color;
                this.Lifetime = this.TotalTime = Lifetime;
            }

            public override void Move(double ElapsedSeconds)
            {
                base.Move(ElapsedSeconds);

                this.Lifetime -= ElapsedSeconds;
                this.p = this.Lifetime / this.TotalTime;
            }

            public override bool Draw(Color Color)
            {
                Color cl = Blend(Color.Black, Color, p);

                int x = (int)(this.X + 0.5);
                int y = (int)(this.Y + 0.5);

                Raster[x - 1, y] = cl;
                Raster[x, y] = cl;
                Raster[x + 1, y] = cl;
                Raster[x, y - 1] = cl;
                Raster[x, y + 1] = cl;

                return this.Lifetime >= 0 && x >= -1 && x <= RasterWidth + 1 && y >= -1 && y <= RasterHeight + 1;
            }
        }

    }
}