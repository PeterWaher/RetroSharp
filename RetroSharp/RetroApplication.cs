using System;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Schema;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using RetroSharp.Gradients;

namespace RetroSharp
{
	/// <summary>
	/// Available display modes
	/// </summary>
	public enum DisplayMode
	{
		/// <summary>
		/// Character display mode. The display is made up of a grid of character cells. Each cell can contain a character, a foreground color and 
		/// a background color.
		/// </summary>
		Characters,

		/// <summary>
		/// Raster graphics display mode. The display is made up of a grid of pixels. Each pixels has its own color. Overlaid the raster graphics 
		/// display is a character display. The difference here is that in RasterGraphics mode, background colors are not used. The raster graphics 
		/// display is considered to work as a background.
		/// </summary>
		RasterGraphics
	}

	/// <summary>
	/// Delegate for procedural coloring algorithms. Can be used to provide customized coloration in graphical primitives.
	/// </summary>
	/// <param name="x">X-coordinate of pixel to be colored.</param>
	/// <param name="y">Y-coordinate of pixel to be colored.</param>
	/// <param name="DestinationColor">Color of pixel before being colored.</param>
	/// <returns>Color to use when coloring the pixels.</returns>
	public delegate Color ProceduralColorAlgorithm(int x, int y, Color DestinationColor);

	/// <summary>
	/// Retro-style application. Base class for all retro-style applications.
	/// </summary>
	public class RetroApplication
	{
		internal const int RasterBlockSize = 32;

		private static string fontName = "Consolas";
		private static int characterSetSize = 256;
		private static int consoleWidth = 80;
		private static int consoleHeight = 30;
		private static int consoleSize = consoleWidth * consoleHeight;
		private static int consoleSizeMinusOneRow = consoleWidth * (consoleHeight - 1);
		private static int cursorX = 0;
		private static int cursorY = 0;
		private static int cursorPos = 0;
		private static int consoleWindowLeft = 0;
		private static int consoleWindowTop = 0;
		private static int consoleWindowRight = 79;
		private static int consoleWindowBottom = 29;
		private static int consoleWindowWidth = 80;
		private static int consoleWindowHeight = 30;
		private static Color foregroundColor = C64Colors.LightBlue;
		private static Color backgroundColor = C64Colors.Blue;

		private static int[] characterSetTextures = null;
		private static Size[] characterSetSizes = null;

		private static SpriteTexture[] spriteTexturesStatic = new SpriteTexture[0];
		private static List<SpriteTexture> spriteTexturesDynamic = new List<SpriteTexture>();
		private static LinkedList<Sprite> sprites = new LinkedList<Sprite>();
		private static readonly Vector3d depthZVector = new Vector3d(0, 0, 1);

		private static DisplayMode displayMode = DisplayMode.Characters;
		private static int[] screenBuffer = null;
		protected static int[] emptyRow = null;
		private static Color[] foregroundColorBuffer = null;
		private static Color[] backgroundColorBuffer = null;
		private static Screen screen = new Screen();
		private static ForegroundColor foreground = new ForegroundColor();
		private static BackgroundColor background = new BackgroundColor();

		private static byte[] raster = null;
		private static bool[] rasterBlocks = null;
		private static Color rasterBackgroundColor = Color.Black;
		private static Raster rasterObj = new Raster();
		private static int rasterWidth = 320;
		private static int rasterHeight = 200;
		private static int rasterBlocksX = 0;
		private static int rasterBlocksY = 0;
		private static int rasterStride = 0;
		private static int rasterSize = 0;
		private static int rasterTexture = 0;
		private static int rasterClipLeft = 0;
		private static int rasterClipTop = 0;
		private static int rasterClipRight = 319;
		private static int rasterClipBottom = 199;

		private static GameWindow wnd;
		private static int screenWidth;
		private static int screenHeight;
		private static int visibleScreenWidth;
		private static int visibleScreenHeight;
		private static double totalTime = 0;

		private static int leftMargin = 0;
		private static int rightMargin = 0;
		private static int topMargin = 0;
		private static int bottomMargin = 0;
		private static Color borderColor = Color.Empty;

		private static Dictionary<OpenTK.Input.Key, bool> keysPressed = new Dictionary<OpenTK.Input.Key, bool>();
		private static LinkedList<char> inputBuffer = new LinkedList<char>();
		private static double inputTime = 0;
		private static bool requestingInput = false;
		private static int startInputX = 0;
		private static int startInputY = 0;
		private static ManualResetEvent inputBufferNonempty = new ManualResetEvent(false);

		private static TextWriter consoleOutput = null;
		private static TextReader consoleInput = null;

		private static ManualResetEvent started = new ManualResetEvent(false);
		private static Thread renderingThread = null;
		private static Random gen = new Random();

		private static LinkedList<EventHandler> openGLTasks = new LinkedList<EventHandler>();

		private static AudioContext audioContext = null;
		private static XRamExtension xRam = null;
		private static Dictionary<int, bool> audioBuffers = new Dictionary<int, bool>();
		private static Dictionary<int, int> audioSources = new Dictionary<int, int>();

		#region Execution

		/// <summary>
		/// Starts the execution of the application.
		/// </summary>
		protected static void Initialize()
		{
			DisplayDevice MainDisplay = DisplayDevice.Default;

			screenWidth = MainDisplay.Width;
			screenHeight = MainDisplay.Height;

			StackTrace StackTrace = new StackTrace();
			StackFrame StackFrame = StackTrace.GetFrame(1);
			Type T = StackFrame.GetMethod().ReflectedType;
			int i;

			foreach (ScreenBorderAttribute Attr in T.GetCustomAttributes(typeof(ScreenBorderAttribute), true))
			{
				leftMargin = Attr.LeftMargin;
				rightMargin = Attr.RightMargin;
				topMargin = Attr.TopMargin;
				bottomMargin = Attr.BottomMargin;
				borderColor = Attr.BorderColor;
			}

			visibleScreenWidth = screenWidth - leftMargin - rightMargin;
			visibleScreenHeight = screenHeight - topMargin - bottomMargin;

			foreach (AspectRatioAttribute Attr in T.GetCustomAttributes(typeof(AspectRatioAttribute), true))
			{
				double DimX = Attr.Width;
				double DimY = Attr.Height;
				double DesiredAspectRatio = DimX / DimY;
				double CurrentAspectRatio = ((double)visibleScreenWidth) / visibleScreenHeight;
				int Diff;

				if (DesiredAspectRatio > CurrentAspectRatio)
				{
					// Wants wider that screen actually is. Shrink height of visible screen.

					// w/h=a, h=w/a

					visibleScreenHeight = (int)(visibleScreenWidth / DesiredAspectRatio + 0.5);

					Diff = screenHeight - visibleScreenHeight;
					topMargin += Diff / 2;
					bottomMargin += Diff - (Diff / 2);
				}
				else if (DesiredAspectRatio < CurrentAspectRatio)
				{
					// Wants less wide that screen actually is. Shrink width of visible screen.

					// w/h=a, w=a*h

					visibleScreenWidth = (int)(DesiredAspectRatio * visibleScreenHeight + 0.5);

					Diff = screenWidth - visibleScreenWidth;
					leftMargin += Diff / 2;
					rightMargin += Diff - (Diff / 2);
				}
			}

			if (borderColor.IsEmpty)
				borderColor = foregroundColor;

			foreach (CharacterSetAttribute Attr in T.GetCustomAttributes(typeof(CharacterSetAttribute), true))
			{
				characterSetSize = Attr.CharacterSetSize;
				fontName = Attr.FontName;
			}

			foreach (CharactersAttribute Attr in T.GetCustomAttributes(typeof(CharactersAttribute), true))
			{
				consoleWidth = Attr.Width;
				consoleHeight = Attr.Height;
				foregroundColor = Attr.ForegroundColor;
				backgroundColor = Attr.BackgroundColor;

				consoleWindowRight = consoleWidth - 1;
				consoleWindowBottom = consoleHeight - 1;
				consoleWindowWidth = consoleWindowRight - consoleWindowLeft + 1;
				consoleWindowHeight = consoleWindowBottom - consoleWindowTop + 1;

				displayMode = DisplayMode.Characters;
			}

			rasterWidth = visibleScreenWidth;
			rasterHeight = visibleScreenHeight;

			foreach (RasterGraphicsAttribute Attr in T.GetCustomAttributes(typeof(RasterGraphicsAttribute), true))
			{
				rasterWidth = rasterObj.rasterWidth = Attr.Width;
				rasterHeight = rasterObj.rasterHeight = Attr.Height;

				rasterClipRight = rasterObj.rasterClipRight = rasterWidth - 1;
				rasterClipBottom = rasterObj.rasterClipBottom = rasterHeight - 1;

				rasterBackgroundColor = Attr.BackgroundColor;

				displayMode = DisplayMode.RasterGraphics;
			}

			rasterStride = rasterObj.rasterStride = rasterWidth * 4;
			rasterSize = rasterWidth * rasterStride;

			byte R = rasterBackgroundColor.R;
			byte G = rasterBackgroundColor.G;
			byte B = rasterBackgroundColor.B;
			byte A = rasterBackgroundColor.A;

			raster = rasterObj.raster = new byte[rasterSize];

			rasterBlocksX = rasterWidth / RasterBlockSize;
			if ((rasterWidth % RasterBlockSize) != 0)
				rasterBlocksX++;

			rasterBlocksY = rasterHeight / RasterBlockSize;
			if ((rasterHeight % RasterBlockSize) != 0)
				rasterBlocksY++;

			rasterObj.rasterBlocksX = rasterBlocksX;
			rasterObj.rasterBlocksY = rasterBlocksY;

			rasterBlocks = rasterObj.rasterBlocks = new bool[rasterBlocksX * rasterBlocksY];

			i = 0;
			while (i < rasterSize)
			{
				raster[i++] = R;
				raster[i++] = G;
				raster[i++] = B;
				raster[i++] = A;
			}

			consoleSize = consoleWidth * consoleHeight;
			consoleSizeMinusOneRow = consoleWidth * (consoleHeight - 1);

			int c = consoleWidth * consoleHeight;
			int j = 32 % characterSetSize;

			screenBuffer = new int[c];
			foregroundColorBuffer = new Color[c];
			backgroundColorBuffer = new Color[c];

			emptyRow = new int[consoleWidth];
			for (i = 0; i < consoleWidth; i++)
				emptyRow[i] = j;

			Clear();

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			renderingThread = new Thread(ExecutionThread);
			renderingThread.Priority = ThreadPriority.AboveNormal;
			renderingThread.Name = "OpenGL rendering thread";
			renderingThread.Start();

			started.WaitOne();

			Console.SetOut(consoleOutput = new ConsoleOutput());
			Console.SetIn(consoleInput = new ConsoleInput());
			Console.TreatControlCAsInput = true;
		}

		/// <summary>
		/// Ends the execution of the application.
		/// </summary>
		public static void Terminate()
		{
			if (renderingThread != null)
			{
				renderingThread.Abort();
				renderingThread = null;

				wnd.Exit();

				lock (audioBuffers)
				{
					foreach (KeyValuePair<int, int> Pair in audioSources)
						AL.DeleteSource(Pair.Key);

					foreach (KeyValuePair<int, bool> Pair in audioBuffers)
						AL.DeleteBuffer(Pair.Key);

					audioSources.Clear();
					audioBuffers.Clear();
				}

				if (audioContext != null)
				{
					audioContext.Dispose();
					audioContext = null;
					xRam = null;
				}

				Environment.Exit(1);
			}
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is CtrlCException)
			{
				Terminate();
				return;
			}

			// TODO: OnException event. Can supress termination

			if (!wnd.IsExiting)
			{
				Exception ex = e.ExceptionObject as Exception;

				WriteLine();
				if (ex == null)
					WriteLine(e.ExceptionObject.ToString());
				else
				{
					while (ex != null)
					{
						WriteLine(ex.Message);
						ex = ex.InnerException;
					}
				}

				if (displayMode != DisplayMode.Characters)
				{
					displayMode = DisplayMode.Characters;

					WriteLine("Press ENTER to continue.");
					ReadLine();
				}
			}

			Terminate();
		}

		private static void ExecutionThread()
		{
			DisplayDevice MainDisplay = DisplayDevice.Default;

			wnd = new GameWindow(screenWidth, screenHeight, GraphicsMode.Default, "Retro Console", GameWindowFlags.Fullscreen, MainDisplay);
			wnd.RenderFrame += new EventHandler<FrameEventArgs>(wnd_RenderFrame);
			wnd.UpdateFrame += new EventHandler<FrameEventArgs>(wnd_UpdateFrame);
			wnd.KeyPress += new EventHandler<KeyPressEventArgs>(wnd_KeyPress);
			wnd.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(wnd_KeyDown);
			wnd.KeyUp += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(wnd_KeyUp);
			wnd.MouseDown += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(wnd_MouseDown);
			wnd.MouseEnter += new EventHandler<EventArgs>(wnd_MouseEnter);
			wnd.MouseLeave += new EventHandler<EventArgs>(wnd_MouseLeave);
			wnd.MouseMove += new EventHandler<OpenTK.Input.MouseMoveEventArgs>(wnd_MouseMove);
			wnd.MouseUp += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(wnd_MouseUp);
			wnd.Resize += new EventHandler<EventArgs>(wnd_Resize);
			wnd.Closed += new EventHandler<EventArgs>(wnd_Closed);

			wnd.Cursor = MouseCursor.Empty;

			GenerateCharacterSetTextures();

			if (raster != null)
			{
				rasterTexture = GL.GenTexture();

				GL.BindTexture(TextureTarget.Texture2D, rasterTexture);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);  // Do not wrap.
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);  // Do not wrap.

				IntPtr Ptr = Marshal.AllocHGlobal(rasterSize);
				try
				{
					Marshal.Copy(raster, 0, Ptr, rasterSize);

					GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, rasterWidth, rasterHeight, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, Ptr);
				}
				finally
				{
					Marshal.FreeHGlobal(Ptr);
				}
			}

			started.Set();

			wnd.Run(60, 60);

			DisposeCharacterSetTextures();
			DisposeSpriteTextures();
		}

		private static void wnd_Closed(object sender, EventArgs e)
		{
			Terminate();
		}

		#endregion

		#region Character sets

		private static void GenerateCharacterSetTextures()
		{
			Bitmap Temp = new Bitmap(1, 1);
			Bitmap Bmp;
			SizeF Size;
			Font Font = new Font(fontName, visibleScreenHeight / consoleHeight, FontStyle.Regular, GraphicsUnit.Pixel);
			SolidBrush White = new SolidBrush(Color.White);
			string s;
			int Width, Height;
			int i;

			characterSetTextures = new int[characterSetSize];
			characterSetSizes = new Size[characterSetSize];

			using (Graphics TempCanvas = Graphics.FromImage(Temp))
			{
				for (i = 0; i < characterSetSize; i++)
				{
					s = new string((char)i, 1);
					Size = TempCanvas.MeasureString(s, Font);

					Width = (int)Math.Ceiling(Size.Width);
					Height = (int)Math.Ceiling(Size.Height);

					using (Bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
					{
						using (Graphics Canvas = Graphics.FromImage(Bmp))
						{
							Canvas.TextRenderingHint = TextRenderingHint.AntiAlias;     // Clear Type has too many strange effects.
							Canvas.Clear(Color.Transparent);
							Canvas.DrawString(s, Font, White, 0, 0);
						}

						DoCustomizeCharacter(i, Bmp, false);
					}
				}
			}

			Font.Dispose();
		}

		private static void DisposeCharacterSetTextures()
		{

			if (characterSetTextures != null)
				GL.DeleteTextures(characterSetTextures.Length, characterSetTextures);
		}

		/// <summary>
		/// Defines a new character. A character has only a single color. The colors of characters on screen are controlled
		/// using the foreground color properties.
		/// 
		/// This version of the method does not support anti-aliased characters. (Anti-aliasing is controlled using the alpha channel.) To
		/// create custom anti-aliased characters, use <see cref="CustomizeCharacter(char, Bitmap) instead."/>
		/// </summary>
		/// <param name="Character">Character to be redefined.</param>
		/// <param name="Rows">Rows containing strings where each character represents a pixel. White space characters represents a 0, all other characters a 1.</param>
		public static void CustomizeCharacter(char Character, params string[] Rows)
		{
			CustomizeCharacter((int)Character, Rows);
		}

		/// <summary>
		/// Defines a new character. A character has only a single color. The colors of characters on screen are controlled
		/// using the foreground color properties.
		/// 
		/// This version of the method does not support anti-aliased characters. (Anti-aliasing is controlled using the alpha channel.) To
		/// create custom anti-aliased characters, use <see cref="CustomizeCharacter(int, Bitmap) instead."/>
		/// </summary>
		/// <param name="Character">Character to be redefined.</param>
		/// <param name="Rows">Rows containing strings where each character represents a pixel. White space characters represents a 0, all other characters a 1.</param>
		public static void CustomizeCharacter(int Character, params string[] Rows)
		{
			int Height = Rows.Length;
			int Width = 0;
			int i;

			foreach (string s in Rows)
			{
				i = s.Length;
				if (i > Width)
					Width = i;
			}

			if (Height == 0 || Width == 0)
				return;

			using (Bitmap Bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
			{
				using (Graphics Canvas = Graphics.FromImage(Bmp))
				{
					Canvas.Clear(Color.Transparent);

					BitmapData data = Bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					int pos = 0;

					int BmpSize = data.Stride * Height;
					byte[] Rgb = new byte[BmpSize];

					foreach (string s in Rows)
					{
						foreach (char ch in s)
						{
							if (ch == ' ')
							{
								Rgb[pos++] = 0;
								Rgb[pos++] = 0;
								Rgb[pos++] = 0;
								Rgb[pos++] = 0;
							}
							else
							{
								Rgb[pos++] = 255;
								Rgb[pos++] = 255;
								Rgb[pos++] = 255;
								Rgb[pos++] = 255;
							}
						}

						i = Width - s.Length;
						while (i-- > 0)
						{
							Rgb[pos++] = 0;
							Rgb[pos++] = 0;
							Rgb[pos++] = 0;
							Rgb[pos++] = 0;
						}
					}

					Marshal.Copy(Rgb, 0, data.Scan0, BmpSize);
					Bmp.UnlockBits(data);
				}

				CustomizeCharacter(Character, Bmp);
			}
		}

		/// <summary>
		/// Defines a new character. A character has only a single color. The colors of characters on screen are controlled
		/// using the foreground color properties. The character can be anti-aliased however. Anti-aliasing is controlled using
		/// the alpha channel of the bitmap.
		/// </summary>
		/// <param name="Character">Character to be redefined.</param>
		/// <param name="CharacterBitmap">Character bitmap.</param>
		public static void CustomizeCharacter(char Character, Bitmap CharacterBitmap)
		{
			CustomizeCharacter((int)Character, CharacterBitmap);
		}

		/// <summary>
		/// Defines a new character. A character has only a single color. The colors of characters on screen are controlled
		/// using the foreground color properties. The character can be anti-aliased however. Anti-aliasing is controlled using
		/// the alpha channel of the bitmap.
		/// </summary>
		/// <param name="Character">Character to be redefined.</param>
		/// <param name="CharacterBitmap">Character bitmap.</param>
		public static void CustomizeCharacter(int Character, Bitmap CharacterBitmap)
		{
			if (Character < 0 || Character >= characterSetSize)
				throw new ArgumentException("Character set only contains " + characterSetSize.ToString() + " characters.");

			Bitmap Bmp = (Bitmap)CharacterBitmap.Clone();

			lock (openGLTasks)
			{
				openGLTasks.AddLast((sender, e) => DoCustomizeCharacter(Character, Bmp, true));
			}
		}

		private static void DoCustomizeCharacter(int Character, Bitmap CharacterBitmap, bool DisposePrevious)
		{
			if (DisposePrevious)
				GL.DeleteTexture(characterSetTextures[Character]);

			int Width = CharacterBitmap.Width;
			int Height = CharacterBitmap.Height;

			characterSetTextures[Character] = GL.GenTexture();
			characterSetSizes[Character] = new Size(Width, Height);

			GL.BindTexture(TextureTarget.Texture2D, characterSetTextures[Character]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);  // Characters should not wrap.
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);  // Characters should not wrap.

			// Correct anti-aliased image to better work with the OpenGL blending process, that only uses the alpha-channel to blend.

			BitmapData data = CharacterBitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			int j;
			byte b;

			int BmpSize = data.Stride * Height;
			byte[] Rgb = new byte[BmpSize];

			Marshal.Copy(data.Scan0, Rgb, 0, BmpSize);

			for (j = 0; j < BmpSize; j++)
			{
				if ((b = Rgb[j]) > 0 && b < 255)
					Rgb[j] = 255;

				j++;

				if ((b = Rgb[j]) > 0 && b < 255)
					Rgb[j] = 255;

				j++;

				if ((b = Rgb[j]) > 0 && b < 255)
					Rgb[j] = 255;

				j++;
			}

			Marshal.Copy(Rgb, 0, data.Scan0, BmpSize);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			CharacterBitmap.UnlockBits(data);
			CharacterBitmap.Dispose();
		}

		#endregion

		#region Sprites

		/// <summary>
		/// Defines a new sprite texture.
		/// </summary>
		/// <param name="SpriteTextureBitmap">Sprite texture.</param>
		/// <param name="DisposeBitmap">If bitmap is to be disposed after the sprite texture has been created.</param>
		/// <returns>Sprite texture index. Use this index when defining what texture to display for a sprite.</returns>
		public static int AddSpriteTexture(Bitmap SpriteTextureBitmap, bool DisposeBitmap)
		{
			return AddSpriteTexture(SpriteTextureBitmap, Point.Empty, Color.Empty, DisposeBitmap);
		}

		/// <summary>
		/// Defines a new sprite texture.
		/// </summary>
		/// <param name="SpriteTextureBitmap">Sprite texture.</param>
		/// <param name="Offset">Offset to sprite anchor point.</param>
		/// <param name="DisposeBitmap">If bitmap is to be disposed after the sprite texture has been created.</param>
		/// <returns>Sprite texture index. Use this index when defining what texture to display for a sprite.</returns>
		public static int AddSpriteTexture(Bitmap SpriteTextureBitmap, Point Offset, bool DisposeBitmap)
		{
			return AddSpriteTexture(SpriteTextureBitmap, Offset, Color.Empty, DisposeBitmap);
		}

		/// <summary>
		/// Defines a new sprite texture.
		/// </summary>
		/// <param name="SpriteTextureBitmap">Sprite texture.</param>
		/// <param name="Transparent">Color to be interpreted as transparent, in case transparency is not encoded using the alpha-channel.</param>
		/// <param name="DisposeBitmap">If bitmap is to be disposed after the sprite texture has been created.</param>
		/// <returns>Sprite texture index. Use this index when defining what texture to display for a sprite.</returns>
		public static int AddSpriteTexture(Bitmap SpriteTextureBitmap, Color Transparent, bool DisposeBitmap)
		{
			return AddSpriteTexture(SpriteTextureBitmap, Point.Empty, Transparent, DisposeBitmap);
		}

		/// <summary>
		/// Defines a new sprite texture.
		/// </summary>
		/// <param name="SpriteTextureBitmap">Sprite texture.</param>
		/// <param name="Offset">Offset to sprite anchor point.</param>
		/// <param name="Transparent">Color to be interpreted as transparent, in case transparency is not encoded using the alpha-channel.</param>
		/// <param name="DisposeBitmap">If bitmap is to be disposed after the sprite texture has been created.</param>
		/// <returns>Sprite texture index. Use this index when defining what texture to display for a sprite.</returns>
		public static int AddSpriteTexture(Bitmap SpriteTextureBitmap, Point Offset, Color Transparent, bool DisposeBitmap)
		{
			Bitmap Bmp = DisposeBitmap ? SpriteTextureBitmap : (Bitmap)SpriteTextureBitmap.Clone();
			SpriteTexture SpriteTexture = new SpriteTexture(-1, new Size(SpriteTextureBitmap.Width, SpriteTextureBitmap.Height), Offset);
			int Result;

			lock (spriteTexturesDynamic)
			{
				Result = spriteTexturesDynamic.Count;
				spriteTexturesDynamic.Add(SpriteTexture);
				spriteTexturesStatic = spriteTexturesDynamic.ToArray();
			}

			lock (openGLTasks)
			{
				openGLTasks.AddLast((sender, e) => DoCreateSpriteTexture(Bmp, SpriteTexture, Transparent, false));
			}

			return Result;
		}

		/// <summary>
		/// Updates an existing sprite texture.
		/// </summary>
		/// <param name="SpriteTextureIndex">Sprite texture index to update.</param>
		/// <param name="SpriteTextureBitmap">Sprite texture.</param>
		/// <param name="Offset">Offset to sprite anchor point.</param>
		/// <param name="Transparent">Color to be interpreted as transparent, in case transparency is not encoded using the alpha-channel.</param>
		/// <param name="DisposeBitmap">If bitmap is to be disposed after the sprite texture has been created.</param>
		public static void UpdateSpriteTexture(int SpriteTextureIndex, Bitmap SpriteTextureBitmap, Point Offset, Color Transparent, bool DisposeBitmap)
		{
			Bitmap Bmp = DisposeBitmap ? SpriteTextureBitmap : (Bitmap)SpriteTextureBitmap.Clone();
			SpriteTexture SpriteTexture;

			lock (spriteTexturesDynamic)
			{
				if (SpriteTextureIndex < 0 || SpriteTextureIndex >= spriteTexturesDynamic.Count)
					throw new ArgumentOutOfRangeException("SpriteTextureIndex", SpriteTextureIndex, "SpriteTextureIndex out of range.");

				SpriteTexture = spriteTexturesDynamic[SpriteTextureIndex];
			}

			lock (openGLTasks)
			{
				openGLTasks.AddLast((sender, e) => DoCreateSpriteTexture(Bmp, SpriteTexture, Transparent, true));
			}
		}

		private static void DoCreateSpriteTexture(Bitmap SpriteTextureBitmap, SpriteTexture SpriteTexture, Color Transparent, bool DisposePrevious)
		{
			int Width = SpriteTextureBitmap.Width;
			int Height = SpriteTextureBitmap.Height;

			if (DisposePrevious)
				GL.DeleteTexture(SpriteTexture.Handle);

			SpriteTexture.Handle = GL.GenTexture();

			GL.BindTexture(TextureTarget.Texture2D, SpriteTexture.Handle);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);  // Sprite textures should not wrap.
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);  // Sprite textures should not wrap.

			// Correct anti-aliased image to better work with the OpenGL blending process, that only uses the alpha-channel to blend.

			BitmapData data = SpriteTextureBitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			if (Transparent != Color.Empty)
			{
				int j;
				byte R, G, B, A;

				R = Transparent.R;
				G = Transparent.G;
				B = Transparent.B;
				A = Transparent.A;

				int BmpSize = data.Stride * Height;
				byte[] Rgb = new byte[BmpSize];

				Marshal.Copy(data.Scan0, Rgb, 0, BmpSize);

				for (j = 0; j < BmpSize; j += 4)
				{
					if (Rgb[j] == B && Rgb[j + 1] == G && Rgb[j + 2] == R && Rgb[j + 3] == A)
					{
						Rgb[j] = 0;
						Rgb[j + 1] = 0;
						Rgb[j + 2] = 0;
						Rgb[j + 3] = 0;
					}
				}

				Marshal.Copy(Rgb, 0, data.Scan0, BmpSize);
			}

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
				PixelType.UnsignedByte, data.Scan0);

			SpriteTextureBitmap.UnlockBits(data);
			SpriteTextureBitmap.Dispose();
		}

		/// <summary>
		/// Disposes of all sprite textures.
		/// </summary>
		public static void DisposeSpriteTextures()
		{
			lock (spriteTexturesDynamic)
			{
				foreach (SpriteTexture SpriteTexture in spriteTexturesDynamic)
					GL.DeleteTexture(SpriteTexture.Handle);

				spriteTexturesDynamic.Clear();
				spriteTexturesStatic = new SpriteTexture[0];
			}
		}

		/// <summary>
		/// Creates a new sprite object. To remove the sprite object from the screen, call the <see cref="Sprite.Dispose"/> method.
		/// </summary>
		/// <param name="X">X-coordinate of anchor point for sprite.</param>
		/// <param name="Y">Y-coordinate of anchor point for sprite.</param>
		/// <param name="SpriteTexture">Sprite texture to use.</param>
		/// <returns>New sprite object.</returns>
		public static Sprite CreateSprite(int X, int Y, int SpriteTexture)
		{
			return CreateSprite(X, Y, 0, SpriteTexture);
		}

		/// <summary>
		/// Creates a new sprite object. To remove the sprite object from the screen, call the <see cref="Sprite.Dispose"/> method.
		/// </summary>
		/// <param name="X">X-coordinate of anchor point for sprite.</param>
		/// <param name="Y">Y-coordinate of anchor point for sprite.</param>
		/// <param name="Angle">Rotation angle of sprite.</param>
		/// <param name="SpriteTexture">Sprite texture to use.</param>
		/// <returns>New sprite object.</returns>
		public static Sprite CreateSprite(int X, int Y, double Angle, int SpriteTexture)
		{
			Sprite Result = new Sprite(X, Y, Angle, SpriteTexture);

			lock (sprites)
			{
				Result.Node = sprites.AddLast(Result);
			}

			return Result;
		}

		internal static void SpriteDisposed(LinkedListNode<Sprite> Node)
		{
			lock (sprites)
			{
				sprites.Remove(Node);
			}
		}

		internal static void MoveSpriteBackward(LinkedListNode<Sprite> Node)
		{
			lock (sprites)
			{
				LinkedListNode<Sprite> Prev = Node.Previous;

				if (Prev != null)
				{
					sprites.Remove(Node);
					sprites.AddBefore(Prev, Node.Value);
				}
			}
		}

		internal static void MoveSpriteForward(LinkedListNode<Sprite> Node)
		{
			lock (sprites)
			{
				LinkedListNode<Sprite> Next = Node.Next;

				if (Next != null)
				{
					sprites.Remove(Node);
					sprites.AddAfter(Next, Node.Value);
				}
			}
		}

		internal static void MoveSpriteBack(LinkedListNode<Sprite> Node)
		{
			lock (sprites)
			{
				LinkedListNode<Sprite> Prev = Node.Previous;

				if (Prev != null)
				{
					sprites.Remove(Node);
					sprites.AddFirst(Node.Value);
				}
			}
		}

		internal static void MoveSpriteFront(LinkedListNode<Sprite> Node)
		{
			lock (sprites)
			{
				LinkedListNode<Sprite> Next = Node.Next;

				if (Next != null)
				{
					sprites.Remove(Node);
					sprites.AddLast(Node.Value);
				}
			}
		}

		#endregion

		#region Rendering

		private static void wnd_Resize(object sender, EventArgs e)
		{
			screenWidth = wnd.Width;
			screenHeight = wnd.Height;

			visibleScreenWidth = screenWidth - leftMargin - rightMargin;
			visibleScreenHeight = screenHeight - topMargin - bottomMargin;

			DisposeCharacterSetTextures();
			GenerateCharacterSetTextures();

			// TODO: Event
		}

		private static void wnd_RenderFrame(object sender, FrameEventArgs e)
		{
			int x, y, i, j, xp, yp, xp2, yp2, w, h;
			double d = e.Time;
			ElapsedTimeEventArgs e2 = new ElapsedTimeEventArgs(e.Time);
			ElapsedTimeEventHandler eventHandler;
			totalTime += d;
			inputTime += d;

			renderingWatch.Reset();
			renderingWatch.Start();

			eventHandler = OnBeforeRender;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}

			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0, screenWidth, screenHeight, 0, -1, 1);
			GL.Viewport(0, 0, screenWidth, screenHeight);
			GL.Disable(EnableCap.DepthTest);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Texture2D);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			eventHandler = OnRenderMatrixSetup;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}

			eventHandler = OnBeforeBackgroundRender;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}

			if (displayMode == DisplayMode.Characters)
			{
				yp2 = topMargin;
				for (y = i = 0; y < consoleHeight; y++)
				{
					yp = yp2;
					yp2 = (y + 1) * visibleScreenHeight / consoleHeight + topMargin;

					xp2 = leftMargin;
					for (x = 0; x < consoleWidth; x++, i++)
					{
						xp = xp2;
						xp2 = (x + 1) * visibleScreenWidth / consoleWidth + leftMargin;

						GL.Color3(backgroundColorBuffer[i]);
						GL.Rect(xp, yp, xp2, yp2);
					}
				}
			}
			else if (displayMode == DisplayMode.RasterGraphics)
			{
				byte[] Data = null;
				IntPtr Ptr = IntPtr.Zero;
				bool First = true;

				try
				{
					for (y = i = 0; y < rasterBlocksY; y++)
					{
						for (x = 0; x < rasterBlocksX; x++, i++)
						{
							if (rasterBlocks[i])
							{
								rasterBlocks[i] = false;

								if (First)
								{
									First = false;
									Ptr = Marshal.AllocHGlobal(RasterBlockSize * RasterBlockSize * 4);
									Data = new byte[RasterBlockSize * RasterBlockSize * 4];

									GL.BindTexture(TextureTarget.Texture2D, rasterTexture);
								}

								xp = (y * rasterWidth + x) * RasterBlockSize * 4;
								w = rasterWidth - x * RasterBlockSize;
								if (w > RasterBlockSize)
									w = RasterBlockSize;

								yp2 = w << 2;

								h = rasterHeight - y * RasterBlockSize;
								if (h > RasterBlockSize)
									h = RasterBlockSize;

								xp2 = yp2 * h;

								for (yp = 0; yp < xp2; yp += yp2)
								{
									Buffer.BlockCopy(raster, xp, Data, yp, yp2);
									xp += rasterStride;
								}

								j = h * yp2;

								Marshal.Copy(Data, 0, Ptr, j);

								GL.TexSubImage2D(TextureTarget.Texture2D, 0, x * RasterBlockSize, y * RasterBlockSize, w, h, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, Ptr);
							}
						}
					}
				}
				finally
				{
					if (!First)
						Marshal.FreeHGlobal(Ptr);
				}

				GL.Enable(EnableCap.Texture2D);

				GL.Color3(Color.White);
				GL.BindTexture(TextureTarget.Texture2D, rasterTexture);

				GL.Begin(PrimitiveType.Quads);
				GL.TexCoord2(0, 0);
				GL.Vertex2(leftMargin, topMargin);

				GL.TexCoord2(1, 0);
				GL.Vertex2(screenWidth - rightMargin, topMargin);

				GL.TexCoord2(1, 1);
				GL.Vertex2(screenWidth - rightMargin, screenHeight - bottomMargin);

				GL.TexCoord2(0, 1);
				GL.Vertex2(leftMargin, screenHeight - bottomMargin);
				GL.End();

				GL.Disable(EnableCap.Texture2D);
			}

			eventHandler = OnBeforeForegroundRender;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}

			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.Blend);

			GL.BlendEquation(BlendEquationMode.FuncAdd);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			yp2 = topMargin;
			for (y = i = 0; y < consoleHeight; y++)
			{
				yp = yp2;
				yp2 = (y + 1) * visibleScreenHeight / consoleHeight + topMargin;

				xp2 = leftMargin;
				for (x = 0; x < consoleWidth; x++, i++)
				{
					xp = xp2;
					xp2 = (x + 1) * visibleScreenWidth / consoleWidth + leftMargin;

					j = screenBuffer[i] % characterSetSize;

					GL.Color3(foregroundColorBuffer[i]);
					GL.BindTexture(TextureTarget.Texture2D, characterSetTextures[j]);

					GL.Begin(PrimitiveType.Quads);
					GL.TexCoord2(0, 0);
					GL.Vertex2(xp, yp);

					GL.TexCoord2(1, 0);
					GL.Vertex2(xp2, yp);

					GL.TexCoord2(1, 1);
					GL.Vertex2(xp2, yp2);

					GL.TexCoord2(0, 1);
					GL.Vertex2(xp, yp2);
					GL.End();
				}
			}

			lock (sprites)
			{
				SpriteTexture[] SpriteTextures = spriteTexturesStatic;
				SpriteTexture SpriteTexture;
				double sx = ((double)visibleScreenWidth) / rasterWidth;
				double sy = ((double)visibleScreenHeight) / rasterHeight;

				j = SpriteTextures.Length;
				foreach (Sprite Sprite in sprites)
				{
					i = Sprite.SpriteTexture;
					if (i < 0 || i >= j)
						continue;

					SpriteTexture = SpriteTextures[i];

					GL.Color3(Color.White);
					GL.BindTexture(TextureTarget.Texture2D, SpriteTexture.Handle);

					GL.Translate(Sprite.X * sx + leftMargin, Sprite.Y * sy + topMargin, 0);
					GL.Scale(sx, sy, 1);
					GL.Rotate(Sprite.Angle, depthZVector);
					GL.Translate(-SpriteTexture.X, -SpriteTexture.Y, 0);

					GL.Begin(PrimitiveType.Quads);
					GL.TexCoord2(0, 0);
					GL.Vertex2(0, 0);

					GL.TexCoord2(1, 0);
					GL.Vertex2(SpriteTexture.Width, 0);

					GL.TexCoord2(1, 1);
					GL.Vertex2(SpriteTexture.Width, SpriteTexture.Height);

					GL.TexCoord2(0, 1);
					GL.Vertex2(0, SpriteTexture.Height);
					GL.End();

					GL.LoadIdentity();
				}
			}

			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);

			eventHandler = OnBeforeCursorRender;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}

			if (requestingInput)
			{
				d = Math.Ceiling(inputTime) - inputTime;
				if (d < 0.75)
				{
					xp = cursorX * visibleScreenWidth / consoleWidth + leftMargin;
					xp2 = (cursorX + 1) * visibleScreenWidth / consoleWidth + leftMargin;

					yp = cursorY * visibleScreenHeight / consoleHeight + topMargin;
					yp2 = (cursorY + 1) * visibleScreenHeight / consoleHeight + topMargin;

					GL.Color3(foregroundColor);
					GL.Rect(xp, yp, xp2, yp2);
				}
			}

			if (leftMargin > 0 || rightMargin > 0 || topMargin > 0 || bottomMargin > 0)
			{
				GL.Color3(borderColor);

				if (topMargin > 0)
					GL.Rect(0, 0, screenWidth, topMargin);

				if (bottomMargin > 0)
					GL.Rect(0, screenHeight - bottomMargin, screenWidth, screenHeight);

				if (leftMargin > 0)
					GL.Rect(0, topMargin, leftMargin, screenHeight - bottomMargin);

				if (rightMargin > 0)
					GL.Rect(screenWidth - rightMargin, topMargin, screenWidth, screenHeight - bottomMargin);
			}

			eventHandler = OnBeforeSwapBuffer;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}

			wnd.SwapBuffers();

			eventHandler = OnRenderDone;
			if (eventHandler != null)
			{
				try
				{
					eventHandler(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}

			renderingWatch.Stop();
			double ms = (renderingWatch.ElapsedTicks * 1000.0) / Stopwatch.Frequency;

			sumRenderingTime += ms - renderingTimes[renderingTimesPosition];
			renderingTimes[renderingTimesPosition] = ms;
			renderingTimesPosition = (renderingTimesPosition + 1) & 127;
		}

		private static double[] renderingTimes = new double[128];
		private static int renderingTimesPosition = 0;
		private static double sumRenderingTime = 0;

		/// <summary>
		/// Returns the average frame rendering time (in milliseconds). The average is computed
		/// over the last 128 frame rendering times.
		/// </summary>
		public static double FrameRenderingTime
		{
			get
			{
				return sumRenderingTime / 128;
			}
		}

		private static void wnd_UpdateFrame(object sender, FrameEventArgs e)
		{
			lock (openGLTasks)
			{
				while (openGLTasks.First != null)
				{
					try
					{
						openGLTasks.First.Value(sender, e);
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.Message);
						Debug.WriteLine(ex.StackTrace.ToString());
					}

					openGLTasks.RemoveFirst();
				}
			}

			ElapsedTimeEventHandler h = OnUpdateModel;
			if (h != null)
			{
				try
				{
					h(sender, new ElapsedTimeEventArgs(e.Time));
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}
		}

		#endregion

		#region Console Output

		private class ConsoleOutput : TextWriter
		{
			public ConsoleOutput()
			{
			}

			public override Encoding Encoding
			{
				get { return System.Text.Encoding.Unicode; }
			}

			public override void Write(string value)
			{
				foreach (char ch in value)
				{
					switch (ch)
					{
						case '\r':
							cursorPos -= cursorX - consoleWindowLeft;
							cursorX = consoleWindowLeft;
							break;

						case '\n':
							cursorY++;
							cursorPos += consoleWidth;
							if (cursorY > consoleWindowBottom)
								this.ScrollUp();
							break;

						case '\t':
							this.Tab();
							break;

						case '\b':
							this.Backspace();
							break;

						case '\f':
							Clear();
							break;

						default:
							screenBuffer[cursorPos] = ch;
							foregroundColorBuffer[cursorPos] = foregroundColor;
							backgroundColorBuffer[cursorPos] = backgroundColor;

							cursorPos++;
							cursorX++;
							if (cursorX > consoleWindowRight)
							{
								cursorX = consoleWindowLeft;
								cursorY++;
								cursorPos = cursorX + cursorY * consoleWidth;
								if (cursorY > consoleWindowBottom)
									this.ScrollUp();
							}
							break;
					}
				}
			}

			public override void Write(char[] buffer, int index, int count)
			{
				char ch;

				while (count-- > 0)
				{
					ch = buffer[index++];

					switch (ch)
					{
						case '\r':
							cursorPos -= cursorX - consoleWindowLeft;
							cursorX = consoleWindowLeft;
							break;

						case '\n':
							cursorY++;
							cursorPos += consoleWidth;
							if (cursorY > consoleWindowBottom)
								this.ScrollUp();
							break;

						case '\t':
							this.Tab();
							break;

						case '\b':
							this.Backspace();
							break;

						case '\f':
							Clear();
							break;

						default:
							screenBuffer[cursorPos] = ch;
							foregroundColorBuffer[cursorPos] = foregroundColor;
							backgroundColorBuffer[cursorPos] = backgroundColor;

							cursorPos++;
							cursorX++;
							if (cursorX > consoleWindowRight)
							{
								cursorX = consoleWindowLeft;
								cursorY++;
								cursorPos = cursorX + cursorY * consoleWidth;
								if (cursorY > consoleWindowBottom)
									this.ScrollUp();
							}
							break;
					}
				}
			}

			public override void WriteLine()
			{
				cursorPos += consoleWidth - cursorX + consoleWindowLeft;
				cursorX = consoleWindowLeft;
				cursorY++;
				if (cursorY > consoleWindowBottom)
					this.ScrollUp();
			}

			private void ScrollUp()
			{
				int i, p;
				p = consoleWindowTop * consoleWidth + consoleWindowLeft;

				for (i = consoleWindowTop; i < consoleWindowBottom; i++, p += consoleWidth)
				{
					Array.Copy(screenBuffer, p + consoleWidth, screenBuffer, p, consoleWindowWidth);
					Array.Copy(foregroundColorBuffer, p + consoleWidth, foregroundColorBuffer, p, consoleWindowWidth);
					Array.Copy(backgroundColorBuffer, p + consoleWidth, backgroundColorBuffer, p, consoleWindowWidth);
				}

				Array.Copy(emptyRow, 0, screenBuffer, p, consoleWindowWidth);

				for (i = 0; i < consoleWindowWidth; i++, p++)
				{
					foregroundColorBuffer[p] = foregroundColor;
					backgroundColorBuffer[p] = backgroundColor;
				}

				cursorY--;
				cursorPos -= consoleWidth;

				if (startInputY > consoleWindowTop)
					startInputY--;
			}

			private void Backspace()
			{
				if (cursorX > startInputX || cursorY > startInputY)
				{
					if (cursorX > consoleWindowLeft)
					{
						cursorX--;
						cursorPos--;
					}
					else
					{
						cursorX = consoleWindowRight;
						cursorY--;
						cursorPos = cursorX + cursorY * consoleWidth;
					}

					screenBuffer[cursorPos] = 32;
					foregroundColorBuffer[cursorPos] = foregroundColor;
					backgroundColorBuffer[cursorPos] = backgroundColor;
				}
			}

			private void Tab()
			{
				int i = 8 - (cursorX & 7);
				this.Write(new string(' ', i));
			}
		}

		/// <summary>
		/// Clears the screen.
		/// </summary>
		public static void Clear()
		{
			ClearConsole();
			ClearRaster();
		}

		/// <summary>
		/// Clears the console part of the screen.
		/// </summary>
		public static void ClearConsole()
		{
			int i;
			int j = 32 % characterSetSize;
			int x, y;

			for (y = consoleWindowTop; y <= consoleWindowBottom; y++)
			{
				i = consoleWindowLeft + y * consoleWidth;

				for (x = consoleWindowLeft; x <= consoleWindowRight; x++, i++)
				{
					screenBuffer[i] = j;
					foregroundColorBuffer[i] = foregroundColor;
					backgroundColorBuffer[i] = backgroundColor;
				}
			}

			cursorX = consoleWindowLeft;
			cursorY = consoleWindowTop;
			cursorPos = consoleWindowLeft + consoleWidth * consoleWindowTop;
			startInputX = consoleWindowLeft;
			startInputY = consoleWindowTop;
		}

		/// <summary>
		/// Clears the raster graphics part of the screen.
		/// </summary>
		public static void ClearRaster()
		{
			if (raster != null)
			{
				int i, x, y;

				byte R = rasterBackgroundColor.R;
				byte G = rasterBackgroundColor.G;
				byte B = rasterBackgroundColor.B;
				byte A = rasterBackgroundColor.A;

				for (y = rasterClipTop; y <= rasterClipBottom; y++)
				{
					i = (rasterClipLeft + y * rasterWidth) << 2;

					for (x = rasterClipLeft; x <= rasterClipRight; x++)
					{
						raster[i++] = R;
						raster[i++] = G;
						raster[i++] = B;
						raster[i++] = A;
					}
				}

				int y1 = rasterClipTop / RasterBlockSize;
				int y2 = rasterClipBottom / RasterBlockSize;
				int x1 = rasterClipLeft / RasterBlockSize;
				int x2 = rasterClipRight / RasterBlockSize;

				for (y = y1; y <= y2; y++)
				{
					i = x1 + y * rasterBlocksX;

					for (x = x1; x <= x2; x++)
						rasterBlocks[i++] = true;
				}
			}
		}

		/// <summary>
		/// Sets the current console window area, and moves the cursor to the top left corner of the window area.
		/// </summary>
		/// <param name="Left">Left coordinate of clip area.</param>
		/// <param name="Top">Top coordinate of clip area.</param>
		/// <param name="Right">Right coordinate of clip area.</param>
		/// <param name="Bottom">Bottom coordainte of clip area.</param>
		public static void SetConsoleWindowArea(int Left, int Top, int Right, int Bottom)
		{
			if (Left < 0)
				Left = 0;
			else if (Left >= consoleWidth)
				Left = consoleWidth - 1;

			if (Right < 0)
				Right = 0;
			else if (Right >= consoleWidth)
				Right = consoleWidth - 1;

			if (Top < 0)
				Top = 0;
			else if (Top >= consoleHeight)
				Top = consoleHeight - 1;

			if (Bottom < 0)
				Bottom = 0;
			else if (Bottom >= consoleHeight)
				Bottom = consoleHeight - 1;

			consoleWindowLeft = Left;
			consoleWindowTop = Top;
			consoleWindowRight = Right;
			consoleWindowBottom = Bottom;

			consoleWindowWidth = consoleWindowRight - consoleWindowLeft + 1;
			consoleWindowHeight = consoleWindowBottom - consoleWindowTop + 1;

			GotoXY(0, 0);
		}

		/// <summary>
		/// Removes the current console window area. This is the same as setting the console window area to the entire console display.
		/// </summary>
		public static void ClearConsoleWindowArea()
		{
			consoleWindowLeft = 0;
			consoleWindowTop = 0;
			consoleWindowRight = consoleWidth - 1;
			consoleWindowBottom = consoleHeight - 1;

			consoleWindowWidth = consoleWindowRight - consoleWindowLeft + 1;
			consoleWindowHeight = consoleWindowBottom - consoleWindowTop + 1;
		}


		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void Write(string value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes characters from <paramref name="buffer"/> to the screen, starting at the zero-based index
		/// <paramref name="index"/> and writing <paramref name="count"/> characters. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="buffer">Buffer containing characters to write.</param>
		/// <param name="index">Points to the first character to write. Zero-based index.</param>
		/// <param name="count">Number of characters to write.</param>
		public static void Write(char[] buffer, int index, int count)
		{
			consoleOutput.Write(buffer, index, count);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void Write(bool value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void Write(char value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes <paramref name="buffer"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="buffer">Characters to output.</param>
		public static void Write(char[] buffer)
		{
			consoleOutput.Write(buffer);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void Write(decimal value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void Write(double value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void Write(float value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void Write(int value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void Write(long value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void Write(object value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		[CLSCompliant(false)]
		public static void Write(uint value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="value">Value to output.</param>
		[CLSCompliant(false)]
		public static void Write(ulong value)
		{
			consoleOutput.Write(value);
		}

		/// <summary>
		/// Writes a string formatted using <see cref="String.Format(string, object)"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="format">String format.</param>
		/// <param name="arg0">Parameter</param>
		public static void Write(string format, object arg0)
		{
			consoleOutput.Write(format, arg0);
		}

		/// <summary>
		/// Writes a string formatted using <see cref="String.Format(string, object[])"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="format">String format.</param>
		/// <param name="arg">Parameters</param>
		public static void Write(string format, params object[] arg)
		{
			consoleOutput.Write(format, arg);
		}

		/// <summary>
		/// Writes a string formatted using <see cref="String.Format(string, object, object)"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="format">String format.</param>
		/// <param name="arg0">Parameter 1</param>
		/// <param name="arg1">Parameter 2</param>
		public static void Write(string format, object arg0, object arg1)
		{
			consoleOutput.Write(format, arg0, arg1);
		}

		/// <summary>
		/// Writes a string formatted using <see cref="String.Format(string, object, object, object)"/> to the screen. The cursor is left at the end of the string.
		/// </summary>
		/// <param name="format">String format.</param>
		/// <param name="arg0">Parameter 1</param>
		/// <param name="arg1">Parameter 2</param>
		/// <param name="arg2">Parameter 3</param>
		public static void Write(string format, object arg0, object arg1, object arg2)
		{
			consoleOutput.Write(format, arg0, arg1, arg2);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void WriteLine(bool value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void WriteLine(char value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes <paramref name="buffer"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="buffer">Characters to output.</param>
		public static void WriteLine(char[] buffer)
		{
			consoleOutput.WriteLine(buffer);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void WriteLine(decimal value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void WriteLine(double value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void WriteLine(float value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void WriteLine(int value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void WriteLine(long value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void WriteLine(object value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		public static void WriteLine(string value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		[CLSCompliant(false)]
		public static void WriteLine(uint value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes <paramref name="value"/> to the screen. The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="value">Value to output.</param>
		[CLSCompliant(false)]
		public static void WriteLine(ulong value)
		{
			consoleOutput.WriteLine(value);
		}

		/// <summary>
		/// Writes a string formatted using <see cref="String.Format(string, object)"/> to the screen. 
		/// The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="format">String format.</param>
		/// <param name="arg0">Parameter</param>
		public static void WriteLine(string format, object arg0)
		{
			consoleOutput.WriteLine(format, arg0);
		}

		/// <summary>
		/// Writes a string formatted using <see cref="String.Format(string, object[])"/> to the screen. 
		/// The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="format">String format.</param>
		/// <param name="arg">Parameters</param>
		public static void WriteLine(string format, params object[] arg)
		{
			consoleOutput.WriteLine(format, arg);
		}

		/// <summary>
		/// Writes characters from <paramref name="buffer"/> to the screen, starting at the zero-based index
		/// <paramref name="index"/> and writing <paramref name="count"/> characters. 
		/// The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="buffer">Buffer containing characters to write.</param>
		/// <param name="index">Points to the first character to write. Zero-based index.</param>
		/// <param name="count">Number of characters to write.</param>
		public static void WriteLine(char[] buffer, int index, int count)
		{
			consoleOutput.WriteLine(buffer, index, count);
		}

		/// <summary>
		/// Writes a string formatted using <see cref="String.Format(string, object, object)"/> to the screen. 
		/// The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="format">String format.</param>
		/// <param name="arg0">Parameter 1</param>
		/// <param name="arg1">Parameter 2</param>
		public static void WriteLine(string format, object arg0, object arg1)
		{
			consoleOutput.WriteLine(format, arg0, arg1);
		}

		/// <summary>
		/// Writes a string formatted using <see cref="String.Format(string, object, object, object)"/> to the screen. 
		/// The cursor will be moved to the beginning of the following line.
		/// </summary>
		/// <param name="format">String format.</param>
		/// <param name="arg0">Parameter 1</param>
		/// <param name="arg1">Parameter 2</param>
		/// <param name="arg2">Parameter 3</param>
		public static void WriteLine(string format, object arg0, object arg1, object arg2)
		{
			consoleOutput.WriteLine(format, arg0, arg1, arg2);
		}

		/// <summary>
		/// Moves the cursor to the beginning of the following line.
		/// </summary>
		public static void WriteLine()
		{
			consoleOutput.WriteLine();
		}

		/// <summary>
		/// Writes a string to the console, but tries to avoid breaking whole words. After the string
		/// the cursor is moved to the beginning of the following line.
		/// </summary>
		/// <param name="s">String to write.</param>
		public static void WriteLineWordWrap(string s)
		{
			WriteWordWrap(s);
			WriteLine();
		}

		/// <summary>
		/// Writes a string to the console, but tries to avoid breaking whole words.
		/// </summary>
		/// <param name="s">String to write.</param>
		public static void WriteWordWrap(string s)
		{
			int SpaceLeft;
			bool First = true;

			foreach (string Word in s.Split(' '))
			{
				if (First)
					First = false;
				else if (CursorX > 0)
					Write(" ");

				SpaceLeft = ConsoleWidth - CursorX;
				if (SpaceLeft < Word.Length)
					WriteLine();

				Write(Word);
			}
		}

		/// <summary>
		/// Sets the cursor position in the character display relative the current console window area.
		/// </summary>
		/// <param name="X">X-coordinate of the cursor position.</param>
		/// <param name="Y">Y-coordinate of the cursor position.</param>
		/// <exception cref="ArgumentException">If trying to set the cursor outside of the screen.</exception>
		public static void GotoXY(int X, int Y)
		{
			if (X < 0 || X >= consoleWindowWidth || Y < 0 || Y >= consoleWindowHeight)
				throw new ArgumentException("Cursor must be within the console window.");

			cursorX = X + consoleWindowLeft;
			cursorY = Y + consoleWindowTop;

			cursorPos = cursorY * consoleWidth + cursorX;
		}

		#endregion

		#region Keyboard input

		private static void wnd_KeyPress(object sender, KeyPressEventArgs e)
		{
			KeyPressedEventHandler h = OnKeyPressed;
			if (h != null)
			{
				try
				{
					KeyPressedEventArgs e2 = new KeyPressedEventArgs(e.KeyChar);
					h(sender, e2);
					if (e2.SupressInput)
						return;
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}

			if (requestingInput)
			{
				lock (inputBuffer)
				{
					inputBuffer.AddLast(e.KeyChar);
				}

				inputBufferNonempty.Set();
			}
		}

		private static void wnd_KeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			lock (keysPressed)
			{
				keysPressed.Remove(e.Key);
			}

			KeyEventHandler h = OnKeyUp;
			if (h != null)
			{
				try
				{
					KeyEventArgs e2 = new KeyEventArgs((int)e.ScanCode, (Key)e.Key, e.Alt, e.Control, e.Shift, e.IsRepeat);
					h(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}
		}

		private static void wnd_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			lock (keysPressed)
			{
				keysPressed[e.Key] = true;
			}

			KeyEventHandler h = OnKeyDown;
			if (h != null)
			{
				try
				{
					KeyEventArgs e2 = new KeyEventArgs((int)e.ScanCode, (Key)e.Key, e.Alt, e.Control, e.Shift, e.IsRepeat);
					h(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}

			// Keys no longer forwarded to KeyPress by OpenTK. Need to forward them manually:
			switch (e.Key)
			{
				case OpenTK.Input.Key.Enter:
					wnd_KeyPress(sender, new KeyPressEventArgs('\r'));
					break;

				case OpenTK.Input.Key.Escape:
					wnd_KeyPress(sender, new KeyPressEventArgs((char)27));
					break;

				case OpenTK.Input.Key.Tab:
					wnd_KeyPress(sender, new KeyPressEventArgs('\t'));
					break;

				case OpenTK.Input.Key.BackSpace:
					wnd_KeyPress(sender, new KeyPressEventArgs('\b'));
					break;

				case OpenTK.Input.Key.C:
					if (e.Control)
						wnd_KeyPress(sender, new KeyPressEventArgs((char)3));
					break;
			}
		}

		/// <summary>
		/// Checks if a key is pressed on the keyboard.
		/// </summary>
		/// <param name="Key">Key</param>
		/// <returns>If the corresponding key is pressed.</returns>
		public static bool IsPressed(KeyCode Key)
		{
			lock (keysPressed)
			{
				return keysPressed.ContainsKey((OpenTK.Input.Key)Key);
			}
		}

		#endregion

		#region Console Input

		private class ConsoleInput : TextReader
		{
			private bool backspaceFound = false;

			public ConsoleInput()
			{
			}

			private void CheckInputRequestMode()
			{
				if (!requestingInput)
				{
					lock (inputBuffer)
					{
						inputBuffer.Clear();
					}

					inputBufferNonempty.Reset();
					requestingInput = true;
					startInputX = cursorX;
					startInputY = cursorY;
				}
			}

			private void CheckInputDone()
			{
				StackTrace StackTrace = new StackTrace();
				StackFrame StackFrame = StackTrace.GetFrame(2);
				Type T = StackFrame.GetMethod().ReflectedType;

				if (T != typeof(ConsoleInput) && T != typeof(TextReader))
					requestingInput = false;
			}

			public override int Peek()
			{
				CheckInputRequestMode();

				lock (inputBuffer)
				{
					if (inputBuffer.First != null)
						return inputBuffer.First.Value;
				}

				CheckInputDone();

				return -1;
			}

			public override int Read()
			{
				int Result;

				CheckInputRequestMode();
				inputBufferNonempty.WaitOne();

				lock (inputBuffer)
				{
					if (inputBuffer.First != null)
					{
						Result = inputBuffer.First.Value;
						inputBuffer.RemoveFirst();

						if (inputBuffer.First == null)
							inputBufferNonempty.Reset();
					}
					else
						return Result = -1;
				}

				if (Result == '\r')
					consoleOutput.WriteLine();
				else if (Result == 3) // CTRL+C
					throw new CtrlCException();
				else if (Result >= 0)
				{
					if (Result == '\b')
						this.backspaceFound = true;

					consoleOutput.Write(new char[] { (char)Result });
				}

				CheckInputDone();

				return Result;
			}

			public override int Read(char[] buffer, int index, int count)
			{
				this.backspaceFound = false;

				int Result = base.Read(buffer, index, count);
				CheckInputDone();

				if (this.backspaceFound)
				{
					Result = this.RemoveBackspaces(buffer, index, Result);
					Result -= index;
				}

				return Result;
			}

			public override int ReadBlock(char[] buffer, int index, int count)
			{
				this.backspaceFound = false;

				int Result = base.ReadBlock(buffer, index, count);
				CheckInputDone();

				if (this.backspaceFound)
				{
					Result = this.RemoveBackspaces(buffer, index, Result);
					Result -= index;
				}

				return Result;
			}

			public override string ReadLine()
			{
				this.backspaceFound = false;

				string Result = base.ReadLine();
				CheckInputDone();

				if (this.backspaceFound)
					return this.RemoveBackspaces(Result);
				else
					return Result;
			}

			public override string ReadToEnd()
			{
				this.backspaceFound = false;

				string Result = base.ReadToEnd();
				CheckInputDone();

				if (this.backspaceFound)
					return this.RemoveBackspaces(Result);
				else
					return Result;
			}

			private string RemoveBackspaces(string s)
			{
				char[] Characters = s.ToCharArray();
				int j = this.RemoveBackspaces(Characters, 0, Characters.Length);

				if (j == 0)
					return string.Empty;
				else
					return new string(Characters, 0, j);
			}

			private int RemoveBackspaces(char[] Characters, int i, int c)
			{
				int j;
				char ch;

				c = Characters.Length;
				j = i;

				while (i < c)
				{
					ch = Characters[i++];

					if (ch != '\b')
						Characters[j++] = ch;
					else if (j > 0)
						j--;
				}

				return j;
			}
		}

		public static int Peek()
		{
			return consoleInput.Peek();
		}

		public static int Read()
		{
			return consoleInput.Read();
		}

		public static int Read(char[] buffer, int index, int count)
		{
			return consoleInput.Read(buffer, index, count);
		}

		public static int ReadBlock(char[] buffer, int index, int count)
		{
			return consoleInput.ReadBlock(buffer, index, count);
		}

		public static string ReadLine()
		{
			return consoleInput.ReadLine();
		}

		public static string ReadToEnd()
		{
			return consoleInput.ReadToEnd();
		}

		#endregion

		#region Random numbers

		/// <summary>
		/// Returns a random number uniformly distributed between 0 and 1.
		/// </summary>
		/// <returns>Random number.</returns>
		public static double Random()
		{
			return gen.NextDouble();
		}

		/// <summary>
		/// Returns a random number uniformly distributed between 0 and <paramref name="Values"/>-1.
		/// </summary>
		/// <param name="Values">Return a random number from among this number of values.</param>
		/// <returns>Random number.</returns>
		public static int Random(int Values)
		{
			return gen.Next(Values);
		}

		/// <summary>
		/// Returns a random number uniformly distribyted between <paramref name="MinValue"/> and <paramref name="MaxValue"/>, both inclusive.
		/// </summary>
		/// <param name="MinValue">Smallest value that can be returned (inclusive).</param>
		/// <param name="MaxValue">Largest value that can be returned (inclusive).</param>
		/// <returns>Random number.</returns>
		public static int Random(int MinValue, int MaxValue)
		{
			return gen.Next(MinValue, MaxValue + 1);
		}

		#endregion

		#region Sleeping

		/// <summary>
		/// Sleeps for <paramref name="Milliseconds"/> milliseconds.
		/// </summary>
		/// <param name="Milliseconds">Time to sleep, in milliseconds.</param>
		public static void Sleep(int Milliseconds)
		{
			Thread.Sleep(Milliseconds);
		}

		#endregion

		#region Type conversions

		#region Int32

		/// <summary>
		/// Converts <paramref name="s"/> to a 32-bit integer value.
		/// </summary>
		/// <param name="s">String</param>
		/// <returns>Converted 32-bit integer value</returns>
		/// <exception cref="Exception">If <paramref name="s"/> is not a 32-bit integer.</exception>
		public static int ToInt(string s)
		{
			return int.Parse(s);
		}

		/// <summary>
		/// Converts <paramref name="Object"/> to a 32-bit integer value.
		/// </summary>
		/// <param name="Object">Object</param>
		/// <returns>Converted 32-bit integer value</returns>
		/// <exception cref="Exception">If <paramref name="Object"/> is not a 32-bit integer value.</exception>
		public static int ToInt(object Object)
		{
			if (Object is int)
				return (int)Object;
			else if (Object is string)
				return ToInt((string)Object);
			else
				return Convert.ToInt32(Object);
		}

		/// <summary>
		/// Tries to convert <paramref name="s"/> to a 32-bit integer value.
		/// </summary>
		/// <param name="s">String to parse.</param>
		/// <param name="Result">The parsed value will be stored in this variable, if possible.</param>
		/// <returns>If the string could be parsed or not.</returns>
		public static bool ToInt(string s, out int Result)
		{
			return int.TryParse(s, out Result);
		}

		#endregion

		#region Int64

		/// <summary>
		/// Converts <paramref name="s"/> to a 64-bit integer value.
		/// </summary>
		/// <param name="s">String</param>
		/// <returns>Converted 64-bit integer value</returns>
		/// <exception cref="Exception">If <paramref name="s"/> is not a 64-bit integer.</exception>
		public static long ToLong(string s)
		{
			return long.Parse(s);
		}

		/// <summary>
		/// Converts <paramref name="Object"/> to a 64-bit integer value.
		/// </summary>
		/// <param name="Object">Object</param>
		/// <returns>Converted 64-bit integer value</returns>
		/// <exception cref="Exception">If <paramref name="Object"/> is not a 64-bit integer value.</exception>
		public static long ToLong(object Object)
		{
			if (Object is int)
				return (int)Object;
			else if (Object is string)
				return ToLong((string)Object);
			else
				return Convert.ToInt64(Object);
		}

		/// <summary>
		/// Tries to convert <paramref name="s"/> to a 64-bit integer value.
		/// </summary>
		/// <param name="s">String to parse.</param>
		/// <param name="Result">The parsed value will be stored in this variable, if possible.</param>
		/// <returns>If the string could be parsed or not.</returns>
		public static bool ToLong(string s, out long Result)
		{
			return long.TryParse(s, out Result);
		}

		#endregion

		#region Double

		/// <summary>
		/// Converts <paramref name="s"/> to a double precision integer value.
		/// </summary>
		/// <param name="s">String</param>
		/// <returns>Converted double precision integer value</returns>
		/// <exception cref="Exception">If <paramref name="s"/> is not a double precision integer.</exception>
		public static double ToDouble(string s)
		{
			return double.Parse(s);
		}

		/// <summary>
		/// Converts <paramref name="Object"/> to a double precision integer value.
		/// </summary>
		/// <param name="Object">Object</param>
		/// <returns>Converted double precision integer value</returns>
		/// <exception cref="Exception">If <paramref name="Object"/> is not a double precision integer value.</exception>
		public static double ToDouble(object Object)
		{
			if (Object is int)
				return (int)Object;
			else if (Object is string)
				return ToDouble((string)Object);
			else
				return Convert.ToDouble(Object);
		}

		/// <summary>
		/// Tries to convert <paramref name="s"/> to a double precision integer value.
		/// </summary>
		/// <param name="s">String to parse.</param>
		/// <param name="Result">The parsed value will be stored in this variable, if possible.</param>
		/// <returns>If the string could be parsed or not.</returns>
		public static bool ToDouble(string s, out double Result)
		{
			return double.TryParse(s, out Result);
		}

		#endregion

		#endregion

		#region Properties

		/// <summary>
		/// Current main screen buffer
		/// </summary>
		public static int[] ScreenBuffer
		{
			get { return screenBuffer; }
		}

		/// <summary>
		/// Current main raster graphics buffer
		/// </summary>
		internal static byte[] RasterBuffer
		{
			get { return raster; }
		}

		/// <summary>
		/// Blocks overview of what parts of the raster display has been changed.
		/// </summary>
		internal static bool[] RasterBlocks
		{
			get { return rasterBlocks; }
		}

		/// <summary>
		/// Numbers of raster blocks along the X-axis.
		/// </summary>
		internal static int RasterBlocksX
		{
			get { return rasterBlocksX; }
		}

		/// <summary>
		/// Numbers of raster blocks along the Y-axis.
		/// </summary>
		internal static int RasterBlocksY
		{
			get { return rasterBlocksY; }
		}

		/// <summary>
		/// Color to use when clearing the raster display using for instance <see cref="Clear()"/>.
		/// </summary>
		internal static Color RasterBackgroundColor
		{
			get { return rasterBackgroundColor; }
			set { rasterBackgroundColor = value; }
		}

		/// <summary>
		/// Current main foreground color buffer
		/// </summary>
		public static Color[] ForegroundColorBuffer
		{
			get { return foregroundColorBuffer; }
		}

		/// <summary>
		/// Current main background color buffer
		/// </summary>
		public static Color[] BackgroundColorBuffer
		{
			get { return backgroundColorBuffer; }
		}

		/// <summary>
		/// Name of current font.
		/// </summary>
		public static string FontName
		{
			get { return fontName; }

			set
			{
				if (fontName != value)
				{
					fontName = value;

					DisposeCharacterSetTextures();
					GenerateCharacterSetTextures();
				}
			}
		}

		/// <summary>
		/// Number of characters in the character set.
		/// </summary>
		public static int CharacterSetSize
		{
			get { return characterSetSize; }

			set
			{
				if (characterSetSize != value)
				{
					if (value <= 0)
						throw new ArgumentException("The character set size must be positive.");

					characterSetSize = value;

					DisposeCharacterSetTextures();
					GenerateCharacterSetTextures();
				}
			}
		}

		/// <summary>
		/// Width of console
		/// </summary>
		public static int ConsoleWidth
		{
			get { return consoleWidth; }

			// TODO: Set property
		}

		/// <summary>
		/// Height of console
		/// </summary>
		public static int ConsoleHeight
		{
			get { return consoleHeight; }

			// TODO: Set property
		}

		/// <summary>
		/// Width of raster graphics display, in pixels
		/// </summary>
		public static int RasterWidth
		{
			get { return rasterWidth; }

			// TODO: Set property
		}

		/// <summary>
		/// Height of raster graphics display, in pixels
		/// </summary>
		public static int RasterHeight
		{
			get { return rasterHeight; }

			// TODO: Set property
		}

		/// <summary>
		/// Left coordainte of raster clip area.
		/// </summary>
		public static int RasterClipLeft
		{
			get { return rasterClipLeft; }
		}

		/// <summary>
		/// Right coordainte of raster clip area.
		/// </summary>
		public static int RasterClipRight
		{
			get { return rasterClipRight; }
		}

		/// <summary>
		/// Top coordainte of raster clip area.
		/// </summary>
		public static int RasterClipTop
		{
			get { return rasterClipTop; }
		}

		/// <summary>
		/// Bottom coordainte of raster clip area.
		/// </summary>
		public static int RasterClipBottom
		{
			get { return rasterClipBottom; }
		}

		/// <summary>
		/// Number of bytes per each line in the raster graphics display.
		/// </summary>
		public static int RasterStride
		{
			get { return rasterStride; }
		}

		/// <summary>
		/// X-coordinate of cursor relative to the current console window.
		/// </summary>
		public static int CursorX
		{
			get { return cursorX - consoleWindowLeft; }

			set
			{
				if (value < 0 || value >= consoleWindowWidth)
					throw new ArgumentException("Cursor must be within the console window.");

				cursorX = value + consoleWindowLeft;
				cursorPos = cursorY * consoleWidth + cursorX;
			}
		}

		/// <summary>
		/// Y-coordinate of cursor relative to the current console window.
		/// </summary>
		public static int CursorY
		{
			get { return cursorY - consoleWindowTop; }

			set
			{
				if (value < 0 || value >= consoleWindowHeight)
					throw new ArgumentException("Cursor must be within the console window.");

				cursorY = value + consoleWindowHeight;
				cursorPos = cursorY * consoleWidth + cursorX;
			}
		}

		/// <summary>
		/// Left coordainte of console window area.
		/// </summary>
		public static int ConsoleWindowLeft
		{
			get { return consoleWindowLeft; }
		}

		/// <summary>
		/// Right coordainte of console window area.
		/// </summary>
		public static int ConsoleWindowRight
		{
			get { return consoleWindowRight; }
		}

		/// <summary>
		/// Top coordainte of console window area.
		/// </summary>
		public static int ConsoleWindowTop
		{
			get { return consoleWindowTop; }
		}

		/// <summary>
		/// Bottom coordainte of console window area.
		/// </summary>
		public static int ConsoleWindowBottom
		{
			get { return consoleWindowBottom; }
		}

		/// <summary>
		/// Width of the current console window area.
		/// </summary>
		public static int ConsoleWindowWidth
		{
			get { return consoleWindowWidth; }
		}

		/// <summary>
		/// Height of the current console window area.
		/// </summary>
		public static int ConsoleWindowHeight
		{
			get { return consoleWindowHeight; }
		}

		/// <summary>
		/// Foreground color of text being output to the console.
		/// </summary>
		public static Color ForegroundColor
		{
			get { return foregroundColor; }
			set { foregroundColor = value; }
		}

		/// <summary>
		/// Background color of text being output to the console.
		/// </summary>
		public static Color BackgroundColor
		{
			get { return backgroundColor; }
			set { backgroundColor = value; }
		}

		/// <summary>
		/// Width of current screen, including border.
		/// </summary>
		public static int ScreenWidth
		{
			get { return screenWidth; }
		}

		/// <summary>
		/// Height of current screen, including border.
		/// </summary>
		public static int ScreenHeight
		{
			get { return screenHeight; }
		}

		/// <summary>
		/// Width of current visible screen, excluding border.
		/// </summary>
		public static int VisibleScreenWidth
		{
			get { return visibleScreenWidth; }
		}

		/// <summary>
		/// Height of current visible screen, excluding border.
		/// </summary>
		public static int VisibleScreenHeight
		{
			get { return visibleScreenHeight; }
		}

		/// <summary>
		/// Time, in seconds, since start of the application.
		/// </summary>
		public static double TotalTime
		{
			get { return totalTime; }
		}

		/// <summary>
		/// Border Left Margin
		/// </summary>
		public static int LeftMargin
		{
			get { return leftMargin; }
			// TODO: Set proeprty
		}

		/// <summary>
		/// Border Right Margin
		/// </summary>
		public static int RightMargin
		{
			get { return rightMargin; }
			// TODO: Set proeprty
		}

		/// <summary>
		/// Border Top Margin
		/// </summary>
		public static int TopMargin
		{
			get { return topMargin; }
			// TODO: Set proeprty
		}

		/// <summary>
		/// Border Bottom Margin
		/// </summary>
		public static int BottomMargin
		{
			get { return bottomMargin; }
			// TODO: Set proeprty
		}

		/// <summary>
		/// Border Color
		/// </summary>
		public static Color BorderColor
		{
			get { return borderColor; }
			set { borderColor = value; }
		}

		/// <summary>
		/// Access to the main screen buffer.
		/// </summary>
		public static Screen Screen
		{
			get { return screen; }
		}

		/// <summary>
		/// Access to the main screen buffer for raster graphics.
		/// </summary>
		public static Raster Raster
		{
			get { return rasterObj; }
		}

		/// <summary>
		/// Access to the foreground color buffer of the main screen.
		/// </summary>
		public static ForegroundColor Foreground
		{
			get { return foreground; }
		}

		/// <summary>
		/// Access to the background color buffer of the main screen.
		/// </summary>
		public static BackgroundColor Background
		{
			get { return background; }
		}

		/// <summary>
		/// Current display mode.
		/// </summary>
		public static DisplayMode DisplayMode
		{
			get { return displayMode; }
			set { displayMode = value; }
		}

		#endregion

		#region Events

		/// <summary>
		/// Event raised when a key has been pressed. Can be used to react on keyboard input or supress keys during input.
		/// </summary>
		public static event KeyPressedEventHandler OnKeyPressed = null;

		/// <summary>
		/// Event raised when a key has been detected to be in a down state.
		/// </summary>
		public static event KeyEventHandler OnKeyDown = null;

		/// <summary>
		/// Event raised when a key has been detected to be in a up state.
		/// </summary>
		public static event KeyEventHandler OnKeyUp = null;

		/// <summary>
		/// Event raised regularly (60 times a second, if performance permits). Can be used to perform model changes in an animated applicaiton.
		/// </summary>
		public static event ElapsedTimeEventHandler OnUpdateModel = null;

		/// <summary>
		/// Event raised before the screen is rendered.
		/// </summary>
		public static event ElapsedTimeEventHandler OnBeforeRender = null;

		/// <summary>
		/// Event raised after initializing OpenGL sreen transformation matrices.
		/// </summary>
		public static event ElapsedTimeEventHandler OnRenderMatrixSetup = null;

		/// <summary>
		/// Event raised before the background is rendered (character set screen)
		/// </summary>
		public static event ElapsedTimeEventHandler OnBeforeBackgroundRender = null;

		/// <summary>
		/// Event raised before the foreground is rendered (character set screen)
		/// </summary>
		public static event ElapsedTimeEventHandler OnBeforeForegroundRender = null;

		/// <summary>
		/// Event raised before rendering the cursor  (character set screen)
		/// </summary>
		public static event ElapsedTimeEventHandler OnBeforeCursorRender = null;

		/// <summary>
		/// Event raised before swapping display buffers (i.e. showing the newly rendered screen).
		/// </summary>
		public static event ElapsedTimeEventHandler OnBeforeSwapBuffer = null;

		/// <summary>
		/// Event raised when the rendering of the screen is complete.
		/// </summary>
		public static event ElapsedTimeEventHandler OnRenderDone = null;

		/// <summary>
		/// Event raised when a mouse button has been released.
		/// </summary>
		public static event MouseEventHandler OnMouseUp = null;

		/// <summary>
		/// Event raised when a mouse button has been clicked.
		/// </summary>
		public static event MouseEventHandler OnMouseDown = null;

		/// <summary>
		/// Event raised when the mouse has moved.
		/// </summary>
		public static event MouseEventHandler OnMouseMove = null;

		/// <summary>
		/// Event raised when the mouse enters the application.
		/// </summary>
		public static event EventHandler OnMouseEnter = null;

		/// <summary>
		/// Event raised when the mouse leaves the application.
		/// </summary>
		public static event EventHandler OnMouseLeave = null;

		#endregion

		#region Resources

		/// <summary>
		/// Returns the binary contents of an embedded resource.
		/// </summary>
		/// <param name="ResourceName">Name of the resource, relative to the namespace of the caller.</param>
		/// <returns>Binary contents of embedded resource</returns>
		/// <exception cref="Exception">If the resource was not found.</exception>
		public static Stream GetResourceStream(string ResourceName)
		{
			StackFrame StackFrame = new StackFrame(1);
			Type Caller = StackFrame.GetMethod().ReflectedType;

			return GetResourceStream(ResourceName, Caller);
		}

		private static Stream GetResourceStream(string ResourceName, Type Caller)
		{
			Assembly Assembly = Caller.Assembly;
			string Namespace = Caller.Namespace;

			ResourceName = Namespace + "." + ResourceName;

			Stream Stream = Assembly.GetManifestResourceStream(ResourceName);
			if (Stream == null)
				throw ResourceNotFoundException(ResourceName, Assembly);

			return Stream;
		}

		/// <summary>
		/// Returns the binary contents of an embedded resource.
		/// </summary>
		/// <param name="ResourceName">Name of the resource, relative to the namespace of the caller.</param>
		/// <returns>Binary contents of embedded resource</returns>
		/// <exception cref="Exception">If the resource was not found.</exception>
		public static byte[] GetResource(string ResourceName)
		{
			StackFrame StackFrame = new StackFrame(1);
			Type Caller = StackFrame.GetMethod().ReflectedType;
			Stream Stream = GetResourceStream(ResourceName, Caller);

			Stream.Position = 0;

			int Size = (int)Stream.Length;
			byte[] Result = new byte[Size];
			Stream.Read(Result, 0, Size);

			return Result;
		}

		/// <summary>
		/// Returns an embedded image.
		/// </summary>
		/// <param name="ResourceName">Name of the image resource, relative to the namespace of the caller.</param>
		/// <returns>Embedded image</returns>
		/// <exception cref="Exception">If the resource was not found.</exception>
		public static Image GetResourceImage(string ResourceName)
		{
			StackFrame StackFrame = new StackFrame(1);
			Type Caller = StackFrame.GetMethod().ReflectedType;
			Stream Stream = GetResourceStream(ResourceName, Caller);

			return Image.FromStream(Stream);
		}

		/// <summary>
		/// Returns an embedded bitmap image.
		/// </summary>
		/// <param name="ResourceName">Name of the bitmap image resource, relative to the namespace of the caller.</param>
		/// <returns>Embedded bitmap image</returns>
		/// <exception cref="Exception">If the resource was not found.</exception>
		public static Bitmap GetResourceBitmap(string ResourceName)
		{
			StackFrame StackFrame = new StackFrame(1);
			Type Caller = StackFrame.GetMethod().ReflectedType;
			Stream Stream = GetResourceStream(ResourceName, Caller);

			return (Bitmap)Image.FromStream(Stream);
		}

		/// <summary>
		/// Returns an embedded WAV audio.
		/// </summary>
		/// <param name="ResourceName">Name of the wav audio resource, relative to the namespace of the caller.</param>
		/// <returns>WAV audio</returns>
		/// <exception cref="Exception">If the resource was not found.</exception>
		public static WavAudio GetResourceWavAudio(string ResourceName)
		{
			StackFrame StackFrame = new StackFrame(1);
			Type Caller = StackFrame.GetMethod().ReflectedType;
			Stream Stream = GetResourceStream(ResourceName, Caller);

			return WavAudio.FromStream(Stream);
		}

		internal static Exception ResourceNotFoundException(string ResourceName, Assembly Assembly)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("A resource named ");
			sb.Append(ResourceName);
			sb.AppendLine(" was not found.");

			sb.AppendLine();
			sb.AppendLine("Resources available in calling assembly:");
			sb.AppendLine();

			foreach (string s in Assembly.GetManifestResourceNames())
				sb.AppendLine(s);

			return new ArgumentException(sb.ToString());
		}

		#endregion

		#region Audio

		private static void CheckAudioInstalled()
		{
			if (audioContext == null)
			{
				audioContext = new AudioContext();
				xRam = new XRamExtension();
			}
		}

		/// <summary>
		/// Uploads and prepares an audio sample. To remove the audio from memory (to make room for other audio samples)
		/// call <see cref="UnloadAudio"/>.
		/// </summary>
		/// <param name="Audio">Wav audio.</param>
		/// <returns>Audio Sample handle</returns>
		public static int UploadAudioSample(WavAudio Audio)
		{
			ALFormat Format;

			switch (Audio.NrChannels)
			{
				case 1:
					switch (Audio.BitsPerSample)
					{
						case 8:
							Format = ALFormat.Mono8;
							break;

						case 16:
							Format = ALFormat.Mono16;
							break;

						default:
							throw new Exception("Only 8 or 16 bits per sample supported.");
					}
					break;

				case 2:
					switch (Audio.BitsPerSample)
					{
						case 8:
							Format = ALFormat.Stereo8;
							break;

						case 16:
							Format = ALFormat.Stereo16;
							break;

						default:
							throw new Exception("Only 8 or 16 bits per sample supported.");
					}
					break;

				default:
					throw new Exception("Only mono or stereo audio samples supported.");
			}

			return UploadAudioSample(Audio.Data, Format, Audio.SampleRate);
		}

		/// <summary>
		/// Uploads and prepares an audio sample. To remove the audio from memory (to make room for other audio samples)
		/// call <see cref="UnloadAudio"/>.
		/// </summary>
		/// <param name="Audio">Binary audio sample.</param>
		/// <param name="AudioFormat">Format of the audio sample.</param>
		/// <param name="Frequency">Frequency, in Hz.</param>
		/// <returns>Audio Sample handle</returns>
		public static int UploadAudioSample(byte[] Audio, ALFormat AudioFormat, int Frequency)
		{
			CheckAudioInstalled();

			int BufferId = AL.GenBuffer();

			if (xRam.IsInitialized)
				xRam.SetBufferMode(1, ref BufferId, XRamExtension.XRamStorage.Hardware);

			int Len = Audio.Length;
			IntPtr Ptr = Marshal.AllocHGlobal(Len);
			try
			{
				Marshal.Copy(Audio, 0, Ptr, Len);
				AL.BufferData(BufferId, AudioFormat, Ptr, Len, Frequency);
			}
			finally
			{
				Marshal.FreeHGlobal(Ptr);
			}

			ALError Error = AL.GetError();
			if (Error != ALError.NoError)
			{
				AL.DeleteBuffer(BufferId);
				throw new Exception(AL.GetErrorString(Error));
			}

			int SourceId = AL.GenSource();

			AL.Source(SourceId, ALSourcei.Buffer, BufferId);

			lock (audioBuffers)
			{
				audioBuffers[BufferId] = true;
				audioSources[SourceId] = BufferId;
			}

			return SourceId;
		}

		/// <summary>
		/// Unloads an audio sample previously uploaded using <see cref="UploadAudio"/>.
		/// </summary>
		/// <param name="AudioSampleHandle">Audio handle returned in a previous <see cref="UploadAudio"/> call.</param>
		public static void UnloadAudioSample(int AudioSampleHandle)
		{
			int BufferId;

			lock (audioBuffers)
			{
				if (audioSources.TryGetValue(AudioSampleHandle, out BufferId))
				{
					audioBuffers.Remove(BufferId);
					audioSources.Remove(AudioSampleHandle);
				}
				else
					return;
			}

			AL.DeleteSource(AudioSampleHandle);
			AL.DeleteBuffer(BufferId);
		}

		public static void PlayAudioSample(int AudioSampleHandle)
		{
			AL.SourcePlay(AudioSampleHandle);
		}

		#endregion

		#region Timing

		private static Stopwatch watch = new Stopwatch();
		private static Stopwatch renderingWatch = new Stopwatch();

		/// <summary>
		/// Starts timing. Call <see cref="EndTiming"/> to get an very precise estimate on the time elapsed.
		/// </summary>
		public static void StartTiming()
		{
			watch.Reset();
			watch.Start();
		}

		/// <summary>
		/// End timing. Start timing by calling <see cref="StartTiming"/>.
		/// </summary>
		/// <returns>Number of seconds since the call of <see cref="StartTiming"/>.</returns>
		public static double EndTiming()
		{
			watch.Stop();

			double d = watch.ElapsedTicks;
			d /= Stopwatch.Frequency;

			return d;
		}

		#endregion

		#region Hit tests

		/// <summary>
		/// Checks if a point lies inside a polygon.
		/// </summary>
		/// <param name="X">X-coordinate</param>
		/// <param name="Y">Y-coordinate</param>
		/// <param name="Points">Points representing the nodes of the polygon.</param>
		/// <returns>If the point lies within the polygon or not.</returns>
		public static bool IsInPolygon(int X, int Y, Point[] Points)
		{
			int NrLeft = 0;
			int NrRight = 0;
			int x0, x1, y0, y1;
			int i, c, xp;

			c = Points.Length;
			if (c < 1)
				return false;

			x1 = Points[c - 1].X;
			y1 = Points[c - 1].Y;

			for (i = 0; i < c; i++)
			{
				x0 = x1;
				y0 = y1;

				x1 = Points[i].X;
				y1 = Points[i].Y;

				if (Y < Math.Min(y0, y1) || Y > Math.Max(y0, y1))
					continue;

				if (y0 == y1)
				{
					if (X >= Math.Min(x0, x1) && X <= Math.Max(x0, x1))
						return true;
				}
				else
				{
					xp = (Y - y0) * (x1 - x0) / (y1 - y0) + x0;
					if (xp < X)
						NrLeft++;
					else if (xp > X)
						NrRight++;
					else
						return true;
				}
			}

			return ((NrLeft & 1) == 1 && (NrRight & 1) == 1);
		}

		/// <summary>
		/// Checks if a line intersects a polygon. The line is given by (<paramref name="X1"/>,<paramref name="Y1"/>)-(<paramref name="X2"/>,<paramref name="Y2"/>).
		/// The polygon is given by its nodes defined in <paramref name="Points"/>.
		/// </summary>
		/// <param name="X1">X-coordinate of first endpoint of line.</param>
		/// <param name="Y1">Y-coordinate of first endpoint of line.</param>
		/// <param name="X2">X-coordinate of second endpoint of line.</param>
		/// <param name="Y2">Y-coordinate of second endpoint of line.</param>
		/// <param name="Points">Nodes defining the polygon.</param>
		/// <returns>If the line lies inside or intersects the boundary of the polygon.</returns>
		public static bool IntersectsPolygon(int X1, int Y1, int X2, int Y2, Point[] Points)
		{
			if (IsInPolygon(X1, Y1, Points))
				return true;

			if (IsInPolygon(X2, Y2, Points))
				return true;

			int x0, y0, x1, y1;
			int i, c = Points.Length;

			if (c < 1)
				return false;

			x1 = Points[c - 1].X;
			y1 = Points[c - 1].Y;

			for (i = 0; i < c; i++)
			{
				x0 = x1;
				y0 = y1;

				x1 = Points[i].X;
				y1 = Points[i].Y;

				if (LinesIntersect(X1, Y1, X2, Y2, x0, y0, x1, y1))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Checks if two line segments intersect.
		/// </summary>
		/// <param name="Line1X1">X-coordinate of first endpoint of line 1.</param>
		/// <param name="Line1Y1">Y-coordinate of first endpoint of line 1.</param>
		/// <param name="Line1X2">X-coordinate of second endpoint of line 1.</param>
		/// <param name="Line1Y2">Y-coordinate of second endpoint of line 1.</param>
		/// <param name="Line2X1">X-coordinate of first endpoint of line 2.</param>
		/// <param name="Line2Y1">Y-coordinate of first endpoint of line 2.</param>
		/// <param name="Line2X2">X-coordinate of second endpoint of line 2.</param>
		/// <param name="Line2Y2">Y-coordinate of second endpoint of line 2.</param>
		/// <returns>If line segments intersect.</returns>
		public static bool LinesIntersect(int Line1X1, int Line1Y1, int Line1X2, int Line1Y2, int Line2X1, int Line2Y1, int Line2X2, int Line2Y2)
		{
			// Equation of line: n dot (P-P1) = 0, where n=normal vector of line. It will be < 0 on one side and > 0 on the other side.

			int nx = Line1Y2 - Line1Y1;
			int ny = Line1X1 - Line1X2;
			int d1 = nx * (Line2X1 - Line1X1) + ny * (Line2Y1 - Line1Y1);
			int d2 = nx * (Line2X2 - Line1X1) + ny * (Line2Y2 - Line1Y1);

			if (Math.Sign(d1) == Math.Sign(d2))
				return false;     // (Line2x1,Line2y1) and (Line2x2,line2y2) on same side of line through (line1x1,line1y1) and (line1x2,line1y2)

			nx = Line2Y2 - Line2Y1;
			ny = Line2X1 - Line2X2;
			d1 = nx * (Line1X1 - Line2X1) + ny * (Line1Y1 - Line2Y1);
			d2 = nx * (Line1X2 - Line2X1) + ny * (Line1Y2 - Line2Y1);

			if (Math.Sign(d1) == Math.Sign(d2))
				return false;     // (Line1x1,Line1y1) and (Line1x2,line1y2) on same side of line through (line2x1,line2y1) and (line2x2,line2y2)

			return true;
		}

		/// <summary>
		/// Checks if two polygons intersect. The first polygon is defined by the nodes <param name="Points1"/> and the second polygon by the nodes
		/// <param name="Points2"/>.
		/// </summary>
		/// <param name="Points1">Nodes defining the first polygon.</param>
		/// <param name="Points2">Nodes defining the second polygon.</param>
		/// <returns>If the polygons intersect.</returns>
		public static bool IntersectsPolygon(Point[] Points1, Point[] Points2)
		{
			int x0, y0, x1, y1;
			int i, c = Points1.Length;

			if (c < 1)
				return false;

			x1 = Points1[c - 1].X;
			y1 = Points1[c - 1].Y;

			for (i = 0; i < c; i++)
			{
				x0 = x1;
				y0 = y1;

				x1 = Points1[i].X;
				y1 = Points1[i].Y;

				if (IntersectsPolygon(x0, y0, x1, y1, Points2))
					return true;
			}

			return false;
		}

		#endregion

		#region Procedural Coloring Algorithms

		#region Linear Gradients

		/// <summary>
		/// Linear gradient coloring algorithm. The coloration is done using a linear gradiant starting at
		/// (<paramref name="FromX"/>,<paramref name="FromY"/>) using the color <paramref name="FromColor"/>
		/// along a line to (<paramref name="ToX"/>,<paramref name="ToY"/>) where the gradient will have
		/// color <paramref name="ToColor"/>. Lines perpendicular to this line will have the same color.
		/// </summary>
		/// <param name="FromX">X-coordinate of first reference point.</param>
		/// <param name="FromY">Y-coordinate of first reference point.</param>
		/// <param name="FromColor">Color at first reference point.</param>
		/// <param name="ToX">X-coordinate of second reference point.</param>
		/// <param name="ToY">Y-coordinate of second reference point.</param>
		/// <param name="ToColor">Color at second reference point.</param>
		public static ProceduralColorAlgorithm LinearGradient(int FromX, int FromY, Color FromColor, int ToX, int ToY, Color ToColor)
		{
			return new LinearGradient(FromX, FromY, FromColor, ToX, ToY, ToColor).GetColor;
		}

		/// <summary>
		/// Linear gradient coloring algorithm. The coloration is done using a linear gradiant starting at
		/// (<paramref name="FromX"/>,<paramref name="FromY"/>) using the color <paramref name="FromColor"/>
		/// along a line to (<paramref name="ToX"/>,<paramref name="ToY"/>) where the gradient will have
		/// color <paramref name="ToColor"/>. Lines perpendicular to this line will have the same color.
		/// This version allows for the insertion of different color stops along the gradient. Each
		/// stop has a corresponding floating point value. Value 0.0 corresponds to 
		/// (<paramref name="FromX"/>,<paramref name="FromY"/>). Value 1.0 corresponds to
		/// (<paramref name="ToX"/>,<paramref name="ToY"/>). It is possible to add stops both with
		/// negative values or with values above 1.0.
		/// </summary>
		/// <param name="FromX">X-coordinate of first reference point.</param>
		/// <param name="FromY">Y-coordinate of first reference point.</param>
		/// <param name="FromColor">Color at first reference point.</param>
		/// <param name="ToX">X-coordinate of second reference point.</param>
		/// <param name="ToY">Y-coordinate of second reference point.</param>
		/// <param name="ToColor">Color at second reference point.</param>
		/// <param name="Stops">Additional color stops. Must be ordered by increasing <see cref="ColorStop.Stop"/>.</param>
		public static ProceduralColorAlgorithm LinearGradient(int FromX, int FromY, Color FromColor, int ToX, int ToY, Color ToColor, params ColorStop[] Stops)
		{
			return new LinearGradient(FromX, FromY, FromColor, ToX, ToY, ToColor, Stops).GetColor;
		}

		#endregion

		#region Radial Gradients

		/// <summary>
		/// Radial gradient coloring algorithm. The coloration is done using a radial gradiant centered at
		/// (<paramref name="FromX"/>,<paramref name="FromY"/>) using the color <paramref name="FromColor"/>.
		/// Pixels are colored depending on the distance from (<paramref name="FromX"/>,<paramref name="FromY"/>) 
		/// where the gradient will have color <paramref name="RadiusColor"/> at a distance of <paramref name="Radius"/>.
		/// </summary>
		/// <param name="FromX">X-coordinate of center point.</param>
		/// <param name="FromY">Y-coordinate of center point.</param>
		/// <param name="FromColor">Color at center point.</param>
		/// <param name="Radius">Radius from the center point.</param>
		/// <param name="RadiusColor">Color at the given radius from the center point.</param>
		public static ProceduralColorAlgorithm RadialGradient(int FromX, int FromY, Color FromColor, int Radius, Color RadiusColor)
		{
			return new RadialGradient(FromX, FromY, FromColor, Radius, RadiusColor).GetColor;
		}

		/// <summary>
		/// Radial gradient coloring algorithm. The coloration is done using a radial gradiant centered at
		/// (<paramref name="FromX"/>,<paramref name="FromY"/>) using the color <paramref name="FromColor"/>.
		/// Pixels are colored depending on the distance from (<paramref name="FromX"/>,<paramref name="FromY"/>) 
		/// where the gradient will have color <paramref name="RadiusColor"/> at a distance of <paramref name="Radius"/>.
		/// </summary>
		/// <param name="FromX">X-coordinate of center point.</param>
		/// <param name="FromY">Y-coordinate of center point.</param>
		/// <param name="FromColor">Color at center point.</param>
		/// <param name="Radius">Radius from the center point.</param>
		/// <param name="RadiusColor">Color at the given radius from the center point.</param>
		/// <param name="Stops">Additional color stops. Must be ordered by increasing <see cref="ColorStop.Stop"/>. Each
		/// <see cref="ColorStop.Stop"/> value represents a radius from th center point.</param>
		public static ProceduralColorAlgorithm RadialGradient(int FromX, int FromY, Color FromColor, int Radius, Color RadiusColor, params ColorStop[] Stops)
		{
			return new RadialGradient(FromX, FromY, FromColor, Radius, RadiusColor, Stops).GetColor;
		}

		#endregion

		#region Texture Fill

		/// <summary>
		/// Colors the destination by repeating a bitmapped texture along the x and y axes.
		/// </summary>
		/// <param name="Texture">Texture</param>
		public static ProceduralColorAlgorithm TextureFill(Bitmap Texture)
		{
			return new TextureFill(Texture).GetColor;
		}

		/// <summary>
		/// Colors the destination by repeating a bitmapped texture along the x and y axes.
		/// </summary>
		/// <param name="Texture">Texture</param>
		/// <param name="OffsetX">Offset along the X-axis.</param>
		/// <param name="OffsetY">Offset along the Y-axis.</param>
		public static ProceduralColorAlgorithm TextureFill(Bitmap Texture, int OffsetX, int OffsetY)
		{
			return new TextureFill(Texture, OffsetX, OffsetY).GetColor;
		}

		#endregion

		#region Blending

		/// <summary>
		/// Blends a color with the destination background.
		/// </summary>
		/// <param name="Color">Color to blend with the destination.</param>
		/// <param name="p">Blending coefficient. 0 = No blending. 1 = Opaque.</param>
		public static ProceduralColorAlgorithm Blend(Color Color, double p)
		{
			return new Gradients.Blend(Color, p).GetColor;
		}

		/// <summary>
		/// Blends two colors
		/// </summary>
		/// <param name="Color1">Color 1</param>
		/// <param name="Color2">Color 2</param>
		/// <param name="p">Interpolation coefficient. 0=<paramref name="Color1"/>, 1=<paramref name="Color2"/>.</param>
		/// <returns>Blended color.</returns>
		public static Color Blend(Color Color1, Color Color2, double p)
		{
			if (p <= 0)
				return Color1;

			if (p >= 1)
				return Color2;

			double u = 1 - p;
			int R = (int)(Color1.R * u + Color2.R * p + 0.5);
			int G = (int)(Color1.G * u + Color2.G * p + 0.5);
			int B = (int)(Color1.B * u + Color2.B * p + 0.5);
			int A = (int)(Color1.A * u + Color2.A * p + 0.5);

			return Color.FromArgb(A, R, G, B);
		}

		#endregion

		#region XOR

		/// <summary>
		/// Performs an exclusive or operation on a color with the destination background.
		/// </summary>
		/// <param name="Color">Color to XOR with the destination.</param>
		public static ProceduralColorAlgorithm Xor(Color Color)
		{
			return new Gradients.Xor(Color).GetColor;
		}

		#endregion

		#endregion

		#region Graphical commands

		#region Clipping

		/// <summary>
		/// Clips a line to the boundaries of a box.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="x1">X-coordinate of second point.</param>
		/// <param name="y1">Y-coordinate of second point.</param>
		/// <param name="Left">Left edge of box.</param>
		/// <param name="Top">Top edge of box.</param>
		/// <param name="Right">Right edge of box.</param>
		/// <param name="Bottom">Bottom edge of box.</param>
		/// <returns>If the line is visible in the box or not.</returns>
		public static bool ClipLine(ref int x1, ref int y1, ref int x2, ref int y2, int Left, int Top, int Right, int Bottom)
		{
			byte Flags1 = 0;
			byte Flags2 = 0;

			if (x1 < Left)
				Flags1 |= 1;

			if (y1 < Top)
				Flags1 |= 2;

			if (x1 > Right)
				Flags1 |= 4;

			if (y1 > Bottom)
				Flags1 |= 8;

			if (x2 < Left)
				Flags2 |= 1;

			if (y2 < Top)
				Flags2 |= 2;

			if (x2 > Right)
				Flags2 |= 4;

			if (y2 > Bottom)
				Flags2 |= 8;

			if ((Flags1 & Flags2) != 0)
				return false;

			if ((Flags1 & 1) != 0)
			{
				y1 = y2 - (y2 - y1) * (x2 - Left) / (x2 - x1);  // x1 != x2
				x1 = Left;

				if (y1 >= Top)
					Flags1 &= 0xff - 2;

				if (y1 <= Bottom)
					Flags1 &= 0xff - 8;
			}

			if ((Flags1 & 2) != 0)
			{
				if (y1 == y2)
					return false;

				x1 = x2 - (x2 - x1) * (y2 - Top) / (y2 - y1);
				y1 = Top;

				if (x1 < Right)
					Flags1 &= 0xff - 4;
			}

			if ((Flags1 & 4) != 0)
			{
				if (x1 == x2)
					return false;

				y1 = y2 - (y2 - y1) * (x2 - Right) / (x2 - x1);
				x1 = Right;

				if (y1 <= Bottom)
					Flags1 &= 0xff - 8;
			}

			if ((Flags1 & 8) != 0)
			{
				if (y1 == y2)
					return false;

				x1 = x2 - (x2 - x1) * (y2 - Bottom) / (y2 - y1);
				y1 = Bottom;
			}

			if (x1 < Left || x1 > Right || y1 < Top || y1 > Bottom)
				return false;

			if ((Flags2 & 1) != 0)
			{
				if (x1 == x2)
					return false;

				y2 = y1 - (y1 - y2) * (x1 - Left) / (x1 - x2);
				x2 = Left;

				if (y2 >= Top)
					Flags2 &= 0xff - 2;

				if (y2 <= Bottom)
					Flags2 &= 0xff - 8;
			}

			if ((Flags2 & 2) != 0)
			{
				if (y1 == y2)
					return false;

				x2 = x1 - (x1 - x2) * (y1 - Top) / (y1 - y2);
				y2 = Top;

				if (x2 < Right)
					Flags2 &= 0xff - 4;
			}

			if ((Flags2 & 4) != 0)
			{
				if (x1 == x2)
					return false;

				y2 = y1 - (y1 - y2) * (x1 - Right) / (x1 - x2);
				x2 = Right;

				if (y2 <= Bottom)
					Flags2 &= 0xff - 8;
			}

			if ((Flags2 & 8) != 0)
			{
				if (y1 == y2)
					return false;

				x2 = x1 - (x1 - x2) * (y1 - Bottom) / (y1 - y2);
				y2 = Bottom;
			}

			if (x2 < Left || x2 > Right || y2 < Top || y2 > Bottom)
				return false;

			return true;
		}

		/// <summary>
		/// Clips a scan line to the boundaries of a box. The coordinates <paramref name="x1"/> and
		/// <paramref name="x2"/> are ordered so that <paramref name="x1"/> &lt; <paramref name="x2"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first end-point.</param>
		/// <param name="x2">X-coordinate of second end-point.</param>
		/// <param name="y">Y-coordinte of scan line.</param>
		/// <param name="Left">Left edge of box.</param>
		/// <param name="Top">Top edge of box.</param>
		/// <param name="Right">Right edge of box.</param>
		/// <param name="Bottom">Bottom edge of box.</param>
		/// <returns>If the scan line is visible in the box or not.</returns>
		public static bool ClipScanLine(ref int x1, ref int x2, int y, int Left, int Top, int Right, int Bottom)
		{
			if (y < Top || y > Bottom)
				return false;

			int t;

			if (x2 < x1)
			{
				t = x1;
				x1 = x2;
				x2 = t;
			}

			if (x1 > Right || x2 < Left)
				return false;

			if (x1 < Left)
				x1 = Left;

			if (x2 > Right)
				x2 = Right;

			return true;
		}

		/// <summary>
		/// Clips a vertical line to the boundaries of a box. The coordinates <paramref name="y1"/> and
		/// <paramref name="y2"/> are ordered so that <paramref name="y1"/> &lt; <paramref name="y2"/>.
		/// </summary>
		/// <param name="x">X-coordinte of vertical line.</param>
		/// <param name="y1">Y-coordinate of first end-point.</param>
		/// <param name="y2">Y-coordinate of second end-point.</param>
		/// <param name="Left">Left edge of box.</param>
		/// <param name="Top">Top edge of box.</param>
		/// <param name="Right">Right edge of box.</param>
		/// <param name="Bottom">Bottom edge of box.</param>
		/// <returns>If the vertical line is visible in the box or not.</returns>
		public static bool ClipVerticalLine(int x, ref int y1, ref int y2, int Left, int Top, int Right, int Bottom)
		{
			if (x < Left || x > Right)
				return false;

			int t;

			if (y2 < y1)
			{
				t = y1;
				y1 = y2;
				y2 = t;
			}

			if (y1 > Bottom || y2 < Top)
				return false;

			if (y1 < Top)
				y1 = Top;

			if (y2 > Bottom)
				y2 = Bottom;

			return true;
		}

		/// <summary>
		/// Clips a box, given the coordinates of two opposing corners, to the boundaries of another box. 
		/// The coordinates <paramref name="x1"/> and <paramref name="x2"/> are ordered so that 
		/// <paramref name="x1"/> &lt; <paramref name="x2"/>.
		/// Similarly the coordinates <paramref name="y1"/> and <paramref name="y2"/> are ordered so 
		/// that <paramref name="y1"/> &lt; <paramref name="y2"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Left">Left edge of box.</param>
		/// <param name="Top">Top edge of box.</param>
		/// <param name="Right">Right edge of box.</param>
		/// <param name="Bottom">Bottom edge of box.</param>
		/// <returns>If the box is visible in the second box or not.</returns>
		public static bool ClipBox(ref int x1, ref int y1, ref int x2, ref int y2, int Left, int Top, int Right, int Bottom)
		{
			int t;

			if (x2 < x1)
			{
				t = x1;
				x1 = x2;
				x2 = t;
			}

			if (x1 > Right || x2 < Left)
				return false;

			if (y2 < y1)
			{
				t = y1;
				y1 = y2;
				y2 = t;
			}

			if (y1 > Bottom || y2 < Top)
				return false;

			if (x1 < Left)
				x1 = Left;

			if (x2 > Right)
				x2 = Right;

			if (y1 < Top)
				y1 = Top;

			if (y2 > Bottom)
				y2 = Bottom;

			return true;
		}

		/// <summary>
		/// Sets the raster clip area.
		/// </summary>
		/// <param name="Left">Left coordinate of clip area.</param>
		/// <param name="Top">Top coordinate of clip area.</param>
		/// <param name="Right">Right coordinate of clip area.</param>
		/// <param name="Bottom">Bottom coordainte of clip area.</param>
		public static void SetClipArea(int Left, int Top, int Right, int Bottom)
		{
			if (Left < 0)
				Left = 0;
			else if (Left >= rasterWidth)
				Left = rasterWidth - 1;

			if (Right < 0)
				Right = 0;
			else if (Right >= rasterWidth)
				Right = rasterWidth - 1;

			if (Top < 0)
				Top = 0;
			else if (Top >= rasterHeight)
				Top = rasterHeight - 1;

			if (Bottom < 0)
				Bottom = 0;
			else if (Bottom >= rasterHeight)
				Bottom = rasterHeight - 1;

			rasterClipLeft = rasterObj.rasterClipLeft = Left;
			rasterClipTop = rasterObj.rasterClipTop = Top;
			rasterClipRight = rasterObj.rasterClipRight = Right;
			rasterClipBottom = rasterObj.rasterClipBottom = Bottom;
		}

		/// <summary>
		/// Removes the current clip area. This is the same as setting the clip area to the entire raster display.
		/// </summary>
		public static void ClearClipArea()
		{
			rasterClipLeft = rasterObj.rasterClipLeft = 0;
			rasterClipTop = rasterObj.rasterClipTop = 0;
			rasterClipRight = rasterObj.rasterClipRight = rasterWidth - 1;
			rasterClipBottom = rasterObj.rasterClipBottom = rasterHeight - 1;
		}

		#endregion

		#region Lines

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y1"/>) and (<paramref name="x2"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawLine(int x1, int y1, int x2, int y2, Color Color)
		{
			if (!ClipLine(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int dx = x2 - x1;
			int dy = y2 - y1;
			int t;
			double a;
			double step;

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				if (x2 < x1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = y1;
				step = ((double)dy) / dx;

				while (x1 <= x2)
				{
					Raster[x1, (int)(a + 0.5)] = Color;
					x1++;
					a += step;
				}
			}
			else
			{
				if (y2 < y1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = x1;
				if (dy == 0)    // only occurs if dx==dy==0, i.e. one point. Will cause no error.
					step = 0;
				else
					step = ((double)dx) / dy;

				while (y1 <= y2)
				{
					Raster[(int)(a + 0.5), y1] = Color;
					y1++;
					a += step;
				}
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y1"/>) and (<paramref name="x2"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawLine(int x1, int y1, int x2, int y2, Color Color, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipLine(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int dx = x2 - x1;
			int dy = y2 - y1;
			int t;
			double a;
			double step;

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				if (x2 < x1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = y1;
				step = ((double)dy) / dx;

				while (x1 <= x2)
				{
					t = (int)(a + 0.5);
					if (!Collision && (Raster[x1, t] != BackgroundColor))
						Collision = true;

					Raster[x1, t] = Color;
					x1++;
					a += step;
				}
			}
			else
			{
				if (y2 < y1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = x1;
				if (dy == 0)    // only occurs if dx==dy==0, i.e. one point. Will cause no error.
					step = 0;
				else
					step = ((double)dx) / dy;

				while (y1 <= y2)
				{
					t = (int)(a + 0.5);
					if (!Collision && (Raster[t, y1] != BackgroundColor))
						Collision = true;

					Raster[t, y1] = Color;
					y1++;
					a += step;
				}
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y1"/>) and (<paramref name="x2"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawLine(int x1, int y1, int x2, int y2, Color Color, BinaryWriter PreviousColors)
		{
			if (!ClipLine(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int dx = x2 - x1;
			int dy = y2 - y1;
			int t;
			double a;
			double step;

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				if (x2 < x1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = y1;
				step = ((double)dy) / dx;

				while (x1 <= x2)
				{
					t = (int)(a + 0.5);
					PreviousColors.Write(Raster[x1, t].ToArgb());
					Raster[x1, t] = Color;
					x1++;
					a += step;
				}
			}
			else
			{
				if (y2 < y1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = x1;
				if (dy == 0)    // only occurs if dx==dy==0, i.e. one point. Will cause no error.
					step = 0;
				else
					step = ((double)dx) / dy;

				while (y1 <= y2)
				{
					t = (int)(a + 0.5);
					PreviousColors.Write(Raster[t, y1].ToArgb());
					Raster[t, y1] = Color;
					y1++;
					a += step;
				}
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y1"/>) and (<paramref name="x2"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawLine(int, int, int, int, Color, BinaryWriter"/>.</param>
		public static void DrawLine(int x1, int y1, int x2, int y2, BinaryReader Colors)
		{
			if (!ClipLine(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int dx = x2 - x1;
			int dy = y2 - y1;
			int t;
			double a;
			double step;

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				if (x2 < x1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = y1;
				step = ((double)dy) / dx;

				while (x1 <= x2)
				{
					t = (int)(a + 0.5);
					Raster[x1, t] = Color.FromArgb(Colors.ReadInt32());
					x1++;
					a += step;
				}
			}
			else
			{
				if (y2 < y1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = x1;
				if (dy == 0)    // only occurs if dx==dy==0, i.e. one point. Will cause no error.
					step = 0;
				else
					step = ((double)dx) / dy;

				while (y1 <= y2)
				{
					t = (int)(a + 0.5);
					Raster[t, y1] = Color.FromArgb(Colors.ReadInt32());
					y1++;
					a += step;
				}
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y1"/>) and (<paramref name="x2"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawLine(int, int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawLine(int x1, int y1, int x2, int y2, BinaryReader Colors, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipLine(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int dx = x2 - x1;
			int dy = y2 - y1;
			int t;
			double a;
			double step;

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				if (x2 < x1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = y1;
				step = ((double)dy) / dx;

				while (x1 <= x2)
				{
					t = (int)(a + 0.5);

					if (!Collision && (Raster[x1, t] != BackgroundColor))
						Collision = true;

					Raster[x1, t] = Color.FromArgb(Colors.ReadInt32());
					x1++;
					a += step;
				}
			}
			else
			{
				if (y2 < y1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = x1;
				if (dy == 0)    // only occurs if dx==dy==0, i.e. one point. Will cause no error.
					step = 0;
				else
					step = ((double)dx) / dy;

				while (y1 <= y2)
				{
					t = (int)(a + 0.5);

					if (!Collision && (Raster[t, y1] != BackgroundColor))
						Collision = true;

					Raster[t, y1] = Color.FromArgb(Colors.ReadInt32());
					y1++;
					a += step;
				}
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y1"/>) and (<paramref name="x2"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawLine(int, int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawLine(int x1, int y1, int x2, int y2, BinaryReader Colors, BinaryWriter PreviousColors)
		{
			if (!ClipLine(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int dx = x2 - x1;
			int dy = y2 - y1;
			int t;
			double a;
			double step;

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				if (x2 < x1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = y1;
				step = ((double)dy) / dx;

				while (x1 <= x2)
				{
					t = (int)(a + 0.5);
					PreviousColors.Write(Raster[x1, t].ToArgb());
					Raster[x1, t] = Color.FromArgb(Colors.ReadInt32());
					x1++;
					a += step;
				}
			}
			else
			{
				if (y2 < y1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = x1;
				if (dy == 0)    // only occurs if dx==dy==0, i.e. one point. Will cause no error.
					step = 0;
				else
					step = ((double)dx) / dy;

				while (y1 <= y2)
				{
					t = (int)(a + 0.5);
					PreviousColors.Write(Raster[t, y1].ToArgb());
					Raster[t, y1] = Color.FromArgb(Colors.ReadInt32());
					y1++;
					a += step;
				}
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y1"/>) and (<paramref name="x2"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		public static void DrawLine(int x1, int y1, int x2, int y2, ProceduralColorAlgorithm ColorAlgorithm)
		{
			if (!ClipLine(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int dx = x2 - x1;
			int dy = y2 - y1;
			int t;
			double a;
			double step;

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				if (x2 < x1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = y1;
				step = ((double)dy) / dx;

				while (x1 <= x2)
				{
					t = (int)(a + 0.5);
					Raster[x1, t] = ColorAlgorithm(x1, t, Raster[x1, t]);
					x1++;
					a += step;
				}
			}
			else
			{
				if (y2 < y1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = x1;
				if (dy == 0)    // only occurs if dx==dy==0, i.e. one point. Will cause no error.
					step = 0;
				else
					step = ((double)dx) / dy;

				while (y1 <= y2)
				{
					t = (int)(a + 0.5);
					Raster[t, y1] = ColorAlgorithm(x1, t, Raster[t, y1]);
					y1++;
					a += step;
				}
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y1"/>) and (<paramref name="x2"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawLine(int x1, int y1, int x2, int y2, ProceduralColorAlgorithm ColorAlgorithm, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipLine(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int dx = x2 - x1;
			int dy = y2 - y1;
			int t;
			double a;
			double step;
			Color cl;

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				if (x2 < x1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = y1;
				step = ((double)dy) / dx;

				while (x1 <= x2)
				{
					t = (int)(a + 0.5);

					cl = Raster[x1, t];
					if (!Collision && (cl != BackgroundColor))
						Collision = true;

					Raster[x1, t] = ColorAlgorithm(x1, t, cl);
					x1++;
					a += step;
				}
			}
			else
			{
				if (y2 < y1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = x1;
				if (dy == 0)    // only occurs if dx==dy==0, i.e. one point. Will cause no error.
					step = 0;
				else
					step = ((double)dx) / dy;

				while (y1 <= y2)
				{
					t = (int)(a + 0.5);

					cl = Raster[t, y1];
					if (!Collision && (cl != BackgroundColor))
						Collision = true;

					Raster[t, y1] = ColorAlgorithm(x1, t, cl);
					y1++;
					a += step;
				}
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y1"/>) and (<paramref name="x2"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawLine(int x1, int y1, int x2, int y2, ProceduralColorAlgorithm ColorAlgorithm, BinaryWriter PreviousColors)
		{
			if (!ClipLine(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int dx = x2 - x1;
			int dy = y2 - y1;
			int t;
			double a;
			double step;
			Color cl;

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				if (x2 < x1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = y1;
				step = ((double)dy) / dx;

				while (x1 <= x2)
				{
					t = (int)(a + 0.5);
					cl = Raster[x1, t];
					PreviousColors.Write(cl.ToArgb());
					Raster[x1, t] = ColorAlgorithm(x1, t, cl);
					x1++;
					a += step;
				}
			}
			else
			{
				if (y2 < y1)
				{
					t = x1;
					x1 = x2;
					x2 = t;

					t = y1;
					y1 = y2;
					y2 = t;

					dx = -dx;
					dy = -dy;
				}

				a = x1;
				if (dy == 0)    // only occurs if dx==dy==0, i.e. one point. Will cause no error.
					step = 0;
				else
					step = ((double)dx) / dy;

				while (y1 <= y2)
				{
					t = (int)(a + 0.5);
					cl = Raster[t, y1];
					PreviousColors.Write(cl.ToArgb());
					Raster[t, y1] = ColorAlgorithm(x1, t, cl);
					y1++;
					a += step;
				}
			}
		}

		#endregion

		#region Scan Lines

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y"/>) and (<paramref name="x2"/>,<paramref name="y"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y">Y-coordinate.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawScanLine(int x1, int x2, int y, Color Color)
		{
			if (!ClipScanLine(ref x1, ref x2, y, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = x2 - x1 + 1;
			int p = y * rasterStride + (x1 << 2);
			int o = (y / RasterBlockSize) * rasterBlocksX;
			int p1 = o + (x1 / RasterBlockSize);
			int p2 = o + (x2 / RasterBlockSize);

			byte R = Color.R;
			byte G = Color.G;
			byte B = Color.B;
			byte A = Color.A;

			while (c-- > 0)
			{
				raster[p++] = R;
				raster[p++] = G;
				raster[p++] = B;
				raster[p++] = A;
			}

			c = p2 - p1 + 1;
			while (c-- > 0)
				rasterBlocks[p1++] = true;
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y"/>) and (<paramref name="x2"/>,<paramref name="y"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y">Y-coordinate.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawScanLine(int x1, int x2, int y, Color Color, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipScanLine(ref x1, ref x2, y, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = x2 - x1 + 1;
			int p = y * rasterStride + (x1 << 2);
			int o = (y / RasterBlockSize) * rasterBlocksX;
			int p1 = o + (x1 / RasterBlockSize);
			int p2 = o + (x2 / RasterBlockSize);

			byte BgR = BackgroundColor.R;
			byte BgG = BackgroundColor.G;
			byte BgB = BackgroundColor.B;
			byte BgA = BackgroundColor.A;

			byte R = Color.R;
			byte G = Color.G;
			byte B = Color.B;
			byte A = Color.A;

			while (c-- > 0)
			{
				if (!Collision && raster[p] != BgR)
					Collision = true;

				raster[p++] = R;

				if (!Collision && raster[p] != BgG)
					Collision = true;

				raster[p++] = G;

				if (!Collision && raster[p] != BgB)
					Collision = true;

				raster[p++] = B;

				if (!Collision && raster[p] != BgA)
					Collision = true;

				raster[p++] = A;
			}

			c = p2 - p1 + 1;
			while (c-- > 0)
				rasterBlocks[p1++] = true;
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y"/>) and (<paramref name="x2"/>,<paramref name="y"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y">Y-coordinate.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawScanLine(int x1, int x2, int y, Color Color, BinaryWriter PreviousColors)
		{
			if (!ClipScanLine(ref x1, ref x2, y, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = x2 - x1 + 1;
			int p = y * rasterStride + (x1 << 2);
			int o = (y / RasterBlockSize) * rasterBlocksX;
			int p1 = o + (x1 / RasterBlockSize);
			int p2 = o + (x2 / RasterBlockSize);

			int PrevColor;

			byte R = Color.R;
			byte G = Color.G;
			byte B = Color.B;
			byte A = Color.A;

			while (c-- > 0)
			{
				PrevColor = raster[p + 3];
				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = R;

				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = G;

				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = B;

				raster[p++] = A;

				PreviousColors.Write(PrevColor);
			}

			c = p2 - p1 + 1;
			while (c-- > 0)
				rasterBlocks[p1++] = true;
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y"/>) and (<paramref name="x2"/>,<paramref name="y"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y">Y-coordinate.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawScanLine(int, int, int, Color, BinaryWriter"/>.</param>
		public static void DrawScanLine(int x1, int x2, int y, BinaryReader Colors)
		{
			if (!ClipScanLine(ref x1, ref x2, y, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = x2 - x1 + 1;
			int p = y * rasterStride + (x1 << 2);
			int o = (y / RasterBlockSize) * rasterBlocksX;
			int p1 = o + (x1 / RasterBlockSize);
			int p2 = o + (x2 / RasterBlockSize);

			byte R, G, B, A;
			int i;

			while (c-- > 0)
			{
				i = Colors.ReadInt32();
				B = (byte)i;
				i >>= 8;
				G = (byte)i;
				i >>= 8;
				R = (byte)i;
				i >>= 8;
				A = (byte)i;

				raster[p++] = R;
				raster[p++] = G;
				raster[p++] = B;
				raster[p++] = A;
			}

			c = p2 - p1 + 1;
			while (c-- > 0)
				rasterBlocks[p1++] = true;
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y"/>) and (<paramref name="x2"/>,<paramref name="y"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y">Y-coordinate.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawScanLine(int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawScanLine(int x1, int x2, int y, BinaryReader Colors, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipScanLine(ref x1, ref x2, y, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = x2 - x1 + 1;
			int p = y * rasterStride + (x1 << 2);
			int o = (y / RasterBlockSize) * rasterBlocksX;
			int p1 = o + (x1 / RasterBlockSize);
			int p2 = o + (x2 / RasterBlockSize);

			byte BgR = BackgroundColor.R;
			byte BgG = BackgroundColor.G;
			byte BgB = BackgroundColor.B;
			byte BgA = BackgroundColor.A;

			byte R, G, B, A, b;
			int PrevColor;
			int i;

			while (c-- > 0)
			{
				i = Colors.ReadInt32();
				B = (byte)i;
				i >>= 8;
				G = (byte)i;
				i >>= 8;
				R = (byte)i;
				i >>= 8;
				A = (byte)i;

				PrevColor = b = raster[p + 3];
				PrevColor <<= 8;

				if (!Collision && b != BgA)
					Collision = true;

				PrevColor |= b = raster[p];
				if (!Collision && b != BgR)
					Collision = true;

				raster[p++] = R;

				PrevColor |= b = raster[p];
				if (!Collision && b != BgG)
					Collision = true;

				raster[p++] = G;

				PrevColor |= b = raster[p];
				if (!Collision && b != BgB)
					Collision = true;

				raster[p++] = B;
				raster[p++] = A;
			}

			c = p2 - p1 + 1;
			while (c-- > 0)
				rasterBlocks[p1++] = true;
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y"/>) and (<paramref name="x2"/>,<paramref name="y"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y">Y-coordinate.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawScanLine(int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawScanLine(int x1, int x2, int y, BinaryReader Colors, BinaryWriter PreviousColors)
		{
			if (!ClipScanLine(ref x1, ref x2, y, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = x2 - x1 + 1;
			int p = y * rasterStride + (x1 << 2);
			int o = (y / RasterBlockSize) * rasterBlocksX;
			int p1 = o + (x1 / RasterBlockSize);
			int p2 = o + (x2 / RasterBlockSize);

			byte R, G, B, A;
			int PrevColor;
			int i;

			while (c-- > 0)
			{
				i = Colors.ReadInt32();
				B = (byte)i;
				i >>= 8;
				G = (byte)i;
				i >>= 8;
				R = (byte)i;
				i >>= 8;
				A = (byte)i;

				PrevColor = raster[p + 3];
				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = R;

				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = G;

				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = B;

				raster[p++] = A;

				PreviousColors.Write(PrevColor);
			}

			c = p2 - p1 + 1;
			while (c-- > 0)
				rasterBlocks[p1++] = true;
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y"/>) and (<paramref name="x2"/>,<paramref name="y"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y">Y-coordinate.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		public static void DrawScanLine(int x1, int x2, int y, ProceduralColorAlgorithm ColorAlgorithm)
		{
			if (!ClipScanLine(ref x1, ref x2, y, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = x2 - x1 + 1;
			int p = y * rasterStride + (x1 << 2);
			int o = (y / RasterBlockSize) * rasterBlocksX;
			int p1 = o + (x1 / RasterBlockSize);
			int p2 = o + (x2 / RasterBlockSize);

			byte R, G, B, A;
			Color Color;

			while (c-- > 0)
			{
				R = raster[p++];
				G = raster[p++];
				B = raster[p++];
				A = raster[p];
				p -= 3;

				Color = ColorAlgorithm(x1++, y, Color.FromArgb(A, R, G, B));

				raster[p++] = Color.R;
				raster[p++] = Color.G;
				raster[p++] = Color.B;
				raster[p++] = Color.A;
			}

			c = p2 - p1 + 1;
			while (c-- > 0)
				rasterBlocks[p1++] = true;
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y"/>) and (<paramref name="x2"/>,<paramref name="y"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y">Y-coordinate.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawScanLine(int x1, int x2, int y, ProceduralColorAlgorithm ColorAlgorithm, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipScanLine(ref x1, ref x2, y, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = x2 - x1 + 1;
			int p = y * rasterStride + (x1 << 2);
			int o = (y / RasterBlockSize) * rasterBlocksX;
			int p1 = o + (x1 / RasterBlockSize);
			int p2 = o + (x2 / RasterBlockSize);

			byte BgR = BackgroundColor.R;
			byte BgG = BackgroundColor.G;
			byte BgB = BackgroundColor.B;
			byte BgA = BackgroundColor.A;

			byte R, G, B, A;
			Color Color;

			while (c-- > 0)
			{
				R = raster[p++];
				G = raster[p++];
				B = raster[p++];
				A = raster[p];
				p -= 3;

				Color = Color.FromArgb(A, R, G, B);

				if (!Collision && Color != BackgroundColor)
					Collision = true;

				Color = ColorAlgorithm(x1++, y, Color);

				R = Color.R;
				G = Color.G;
				B = Color.B;
				A = Color.A;

				raster[p++] = Color.R;
				raster[p++] = Color.G;
				raster[p++] = Color.B;
				raster[p++] = Color.A;
			}

			c = p2 - p1 + 1;
			while (c-- > 0)
				rasterBlocks[p1++] = true;
		}

		/// <summary>
		/// Draws a line between (<paramref name="x1"/>,<paramref name="y"/>) and (<paramref name="x2"/>,<paramref name="y"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x1">X-coordinate of first point.</param>
		/// <param name="x2">X-coordinate of second point.</param>
		/// <param name="y">Y-coordinate.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawScanLine(int x1, int x2, int y, ProceduralColorAlgorithm ColorAlgorithm, BinaryWriter PreviousColors)
		{
			if (!ClipScanLine(ref x1, ref x2, y, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = x2 - x1 + 1;
			int p = y * rasterStride + (x1 << 2);
			int o = (y / RasterBlockSize) * rasterBlocksX;
			int p1 = o + (x1 / RasterBlockSize);
			int p2 = o + (x2 / RasterBlockSize);

			byte R, G, B, A;
			Color Color;

			while (c-- > 0)
			{
				R = raster[p++];
				G = raster[p++];
				B = raster[p++];
				A = raster[p];
				p -= 3;

				Color = Color.FromArgb(A, R, G, B);
				PreviousColors.Write(Color.ToArgb());
				Color = ColorAlgorithm(x1++, y, Color);

				raster[p++] = Color.R;
				raster[p++] = Color.G;
				raster[p++] = Color.B;
				raster[p++] = Color.A;
			}

			c = p2 - p1 + 1;
			while (c-- > 0)
				rasterBlocks[p1++] = true;
		}

		#endregion

		#region Vertical Lines

		/// <summary>
		/// Draws a line between (<paramref name="x"/>,<paramref name="y1"/>) and (<paramref name="x"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x">X-coordinate</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawVerticalLine(int x, int y1, int y2, Color Color)
		{
			if (!ClipVerticalLine(x, ref y1, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = y2 - y1 + 1;
			int p = y1 * rasterStride + (x << 2);
			int o = (x / RasterBlockSize);
			int p1 = (y1 / RasterBlockSize) * rasterBlocksX + o;
			int p2 = (y2 / RasterBlockSize) * rasterBlocksX + o;
			int delta = rasterStride - 4;

			byte R = Color.R;
			byte G = Color.G;
			byte B = Color.B;
			byte A = Color.A;

			while (c-- > 0)
			{
				raster[p++] = R;
				raster[p++] = G;
				raster[p++] = B;
				raster[p++] = A;
				p += delta;
			}

			while (p1 < p2)
			{
				rasterBlocks[p1] = true;
				p1 += rasterBlocksX;
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x"/>,<paramref name="y1"/>) and (<paramref name="x"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x">X-coordinate</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawVerticalLine(int x, int y1, int y2, Color Color, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipVerticalLine(x, ref y1, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = y2 - y1 + 1;
			int p = y1 * rasterStride + (x << 2);
			int o = (x / RasterBlockSize);
			int p1 = (y1 / RasterBlockSize) * rasterBlocksX + o;
			int p2 = (y2 / RasterBlockSize) * rasterBlocksX + o;
			int delta = rasterStride - 4;

			byte BgR = BackgroundColor.R;
			byte BgG = BackgroundColor.G;
			byte BgB = BackgroundColor.B;
			byte BgA = BackgroundColor.A;

			byte R = Color.R;
			byte G = Color.G;
			byte B = Color.B;
			byte A = Color.A;

			while (c-- > 0)
			{
				if (!Collision && raster[p] != BgR)
					Collision = true;

				raster[p++] = R;

				if (!Collision && raster[p] != BgG)
					Collision = true;

				raster[p++] = G;

				if (!Collision && raster[p] != BgB)
					Collision = true;

				raster[p++] = B;

				if (!Collision && raster[p] != BgA)
					Collision = true;

				raster[p++] = A;
				p += delta;
			}

			while (p1 < p2)
			{
				rasterBlocks[p1] = true;
				p1 += rasterBlocksX;
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x"/>,<paramref name="y1"/>) and (<paramref name="x"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x">X-coordinate</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawVerticalLine(int x, int y1, int y2, Color Color, BinaryWriter PreviousColors)
		{
			if (!ClipVerticalLine(x, ref y1, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = y2 - y1 + 1;
			int p = y1 * rasterStride + (x << 2);
			int o = (x / RasterBlockSize);
			int p1 = (y1 / RasterBlockSize) * rasterBlocksX + o;
			int p2 = (y2 / RasterBlockSize) * rasterBlocksX + o;
			int delta = rasterStride - 4;

			int PrevColor;

			byte R = Color.R;
			byte G = Color.G;
			byte B = Color.B;
			byte A = Color.A;

			while (c-- > 0)
			{
				PrevColor = raster[p + 3];
				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = R;

				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = G;

				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = B;

				raster[p++] = A;
				p += delta;

				PreviousColors.Write(PrevColor);
			}

			while (p1 < p2)
			{
				rasterBlocks[p1] = true;
				p1 += rasterBlocksX;
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x"/>,<paramref name="y1"/>) and (<paramref name="x"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x">X-coordinate</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawVerticalLine(int, int, int, Color, BinaryWriter"/>.</param>
		public static void DrawVerticalLine(int x, int y1, int y2, BinaryReader Colors)
		{
			if (!ClipVerticalLine(x, ref y1, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = y2 - y1 + 1;
			int p = y1 * rasterStride + (x << 2);
			int o = (x / RasterBlockSize);
			int p1 = (y1 / RasterBlockSize) * rasterBlocksX + o;
			int p2 = (y2 / RasterBlockSize) * rasterBlocksX + o;
			int delta = rasterStride - 4;

			byte R, G, B, A;
			int i;

			while (c-- > 0)
			{
				i = Colors.ReadInt32();
				B = (byte)i;
				i >>= 8;
				G = (byte)i;
				i >>= 8;
				R = (byte)i;
				i >>= 8;
				A = (byte)i;

				raster[p++] = R;
				raster[p++] = G;
				raster[p++] = B;
				raster[p++] = A;
				p += delta;
			}

			while (p1 < p2)
			{
				rasterBlocks[p1] = true;
				p1 += rasterBlocksX;
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x"/>,<paramref name="y1"/>) and (<paramref name="x"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x">X-coordinate</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawVerticalLine(int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawVerticalLine(int x, int y1, int y2, BinaryReader Colors, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipVerticalLine(x, ref y1, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = y2 - y1 + 1;
			int p = y1 * rasterStride + (x << 2);
			int o = (x / RasterBlockSize);
			int p1 = (y1 / RasterBlockSize) * rasterBlocksX + o;
			int p2 = (y2 / RasterBlockSize) * rasterBlocksX + o;
			int delta = rasterStride - 4;

			byte BgR = BackgroundColor.R;
			byte BgG = BackgroundColor.G;
			byte BgB = BackgroundColor.B;
			byte BgA = BackgroundColor.A;

			byte R, G, B, A, b;
			int PrevColor;
			int i;

			while (c-- > 0)
			{
				i = Colors.ReadInt32();
				B = (byte)i;
				i >>= 8;
				G = (byte)i;
				i >>= 8;
				R = (byte)i;
				i >>= 8;
				A = (byte)i;

				PrevColor = b = raster[p + 3];
				PrevColor <<= 8;

				if (!Collision && b != BgA)
					Collision = true;

				PrevColor |= b = raster[p];
				if (!Collision && b != BgR)
					Collision = true;

				raster[p++] = R;

				PrevColor |= b = raster[p];
				if (!Collision && b != BgG)
					Collision = true;

				raster[p++] = G;

				PrevColor |= b = raster[p];
				if (!Collision && b != BgB)
					Collision = true;

				raster[p++] = B;
				raster[p++] = A;
				p += delta;
			}

			while (p1 < p2)
			{
				rasterBlocks[p1] = true;
				p1 += rasterBlocksX;
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x"/>,<paramref name="y1"/>) and (<paramref name="x"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x">X-coordinate</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawVerticalLine(int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawVerticalLine(int x, int y1, int y2, BinaryReader Colors, BinaryWriter PreviousColors)
		{
			if (!ClipVerticalLine(x, ref y1, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = y2 - y1 + 1;
			int p = y1 * rasterStride + (x << 2);
			int o = (x / RasterBlockSize);
			int p1 = (y1 / RasterBlockSize) * rasterBlocksX + o;
			int p2 = (y2 / RasterBlockSize) * rasterBlocksX + o;
			int delta = rasterStride - 4;

			byte R, G, B, A;
			int PrevColor;
			int i;

			while (c-- > 0)
			{
				i = Colors.ReadInt32();
				B = (byte)i;
				i >>= 8;
				G = (byte)i;
				i >>= 8;
				R = (byte)i;
				i >>= 8;
				A = (byte)i;

				PrevColor = raster[p + 3];
				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = R;

				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = G;

				PrevColor <<= 8;
				PrevColor |= raster[p];
				raster[p++] = B;

				raster[p++] = A;
				p += delta;

				PreviousColors.Write(PrevColor);
			}

			while (p1 < p2)
			{
				rasterBlocks[p1] = true;
				p1 += rasterBlocksX;
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x"/>,<paramref name="y1"/>) and (<paramref name="x"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x">X-coordinate</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		public static void DrawVerticalLine(int x, int y1, int y2, ProceduralColorAlgorithm ColorAlgorithm)
		{
			if (!ClipVerticalLine(x, ref y1, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = y2 - y1 + 1;
			int p = y1 * rasterStride + (x << 2);
			int o = (x / RasterBlockSize);
			int p1 = (y1 / RasterBlockSize) * rasterBlocksX + o;
			int p2 = (y2 / RasterBlockSize) * rasterBlocksX + o;
			int delta = rasterStride - 4;

			byte R, G, B, A;
			Color Color;

			while (c-- > 0)
			{
				R = raster[p++];
				G = raster[p++];
				B = raster[p++];
				A = raster[p];
				p -= 3;

				Color = ColorAlgorithm(x, y1++, Color.FromArgb(A, R, G, B));

				raster[p++] = Color.R;
				raster[p++] = Color.G;
				raster[p++] = Color.B;
				raster[p++] = Color.A;
				p += delta;
			}

			while (p1 < p2)
			{
				rasterBlocks[p1] = true;
				p1 += rasterBlocksX;
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x"/>,<paramref name="y1"/>) and (<paramref name="x"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x">X-coordinate</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawVerticalLine(int x, int y1, int y2, ProceduralColorAlgorithm ColorAlgorithm, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipVerticalLine(x, ref y1, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = y2 - y1 + 1;
			int p = y1 * rasterStride + (x << 2);
			int o = (x / RasterBlockSize);
			int p1 = (y1 / RasterBlockSize) * rasterBlocksX + o;
			int p2 = (y2 / RasterBlockSize) * rasterBlocksX + o;
			int delta = rasterStride - 4;

			byte BgR = BackgroundColor.R;
			byte BgG = BackgroundColor.G;
			byte BgB = BackgroundColor.B;
			byte BgA = BackgroundColor.A;

			byte R, G, B, A;
			Color Color;

			while (c-- > 0)
			{
				R = raster[p++];
				G = raster[p++];
				B = raster[p++];
				A = raster[p];
				p -= 3;

				Color = Color.FromArgb(A, R, G, B);

				if (!Collision && Color != BackgroundColor)
					Collision = true;

				Color = ColorAlgorithm(x, y1++, Color);

				R = Color.R;
				G = Color.G;
				B = Color.B;
				A = Color.A;

				raster[p++] = Color.R;
				raster[p++] = Color.G;
				raster[p++] = Color.B;
				raster[p++] = Color.A;
				p += delta;
			}

			while (p1 < p2)
			{
				rasterBlocks[p1] = true;
				p1 += rasterBlocksX;
			}
		}

		/// <summary>
		/// Draws a line between (<paramref name="x"/>,<paramref name="y1"/>) and (<paramref name="x"/>,<paramref name="y2"/>), using the color <paramref name="Color"/>.
		/// </summary>
		/// <param name="x">X-coordinate</param>
		/// <param name="y1">Y-coordinate of first point.</param>
		/// <param name="y2">Y-coordinate of second point.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawVerticalLine(int x, int y1, int y2, ProceduralColorAlgorithm ColorAlgorithm, BinaryWriter PreviousColors)
		{
			if (!ClipVerticalLine(x, ref y1, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			int c = y2 - y1 + 1;
			int p = y1 * rasterStride + (x << 2);
			int o = (x / RasterBlockSize);
			int p1 = (y1 / RasterBlockSize) * rasterBlocksX + o;
			int p2 = (y2 / RasterBlockSize) * rasterBlocksX + o;
			int delta = rasterStride - 4;

			byte R, G, B, A;
			Color Color;

			while (c-- > 0)
			{
				R = raster[p++];
				G = raster[p++];
				B = raster[p++];
				A = raster[p];
				p -= 3;

				Color = Color.FromArgb(A, R, G, B);
				PreviousColors.Write(Color.ToArgb());
				Color = ColorAlgorithm(x, y1++, Color);

				raster[p++] = Color.R;
				raster[p++] = Color.G;
				raster[p++] = Color.B;
				raster[p++] = Color.A;
				p += delta;
			}

			while (p1 < p2)
			{
				rasterBlocks[p1] = true;
				p1 += rasterBlocksX;
			}
		}

		#endregion

		#region Draw Rectangle

		/// <summary>
		/// Draws a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawRectangle(int x1, int y1, int x2, int y2, Color Color)
		{
			DrawLine(x1, y1, x2, y1, Color);
			DrawLine(x2, y1, x2, y2, Color);
			DrawLine(x2, y2, x1, y2, Color);
			DrawLine(x1, y2, x1, y1, Color);
		}

		/// <summary>
		/// Draws a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawRectangle(int x1, int y1, int x2, int y2, Color Color, Color BackgroundColor, out bool Collision)
		{
			bool b;

			DrawLine(x1, y1, x2, y1, Color, BackgroundColor, out Collision);

			DrawLine(x2, y1, x2, y2, Color, BackgroundColor, out b);
			Collision |= b;

			DrawLine(x2, y2, x1, y2, Color, BackgroundColor, out b);
			Collision |= b;

			DrawLine(x1, y2, x1, y1, Color, BackgroundColor, out b);
			Collision |= b;
		}

		/// <summary>
		/// Draws a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawRectangle(int x1, int y1, int x2, int y2, Color Color, BinaryWriter PreviousColors)
		{
			DrawLine(x1, y1, x2, y1, Color, PreviousColors);
			DrawLine(x2, y1, x2, y2, Color, PreviousColors);
			DrawLine(x2, y2, x1, y2, Color, PreviousColors);
			DrawLine(x1, y2, x1, y1, Color, PreviousColors);
		}

		/// <summary>
		/// Draws a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawRectangle(int, int, int, int, Color, BinaryWriter"/>.</param>
		public static void DrawRectangle(int x1, int y1, int x2, int y2, BinaryReader Colors)
		{
			DrawLine(x1, y1, x2, y1, Colors);
			DrawLine(x2, y1, x2, y2, Colors);
			DrawLine(x2, y2, x1, y2, Colors);
			DrawLine(x1, y2, x1, y1, Colors);
		}

		/// <summary>
		/// Draws a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawRectangle(int, int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawRectangle(int x1, int y1, int x2, int y2, BinaryReader Colors, Color BackgroundColor, out bool Collision)
		{
			bool b;

			DrawLine(x1, y1, x2, y1, Colors, BackgroundColor, out Collision);

			DrawLine(x2, y1, x2, y2, Colors, BackgroundColor, out b);
			Collision |= b;

			DrawLine(x2, y2, x1, y2, Colors, BackgroundColor, out b);
			Collision |= b;

			DrawLine(x1, y2, x1, y1, Colors, BackgroundColor, out b);
			Collision |= b;
		}

		/// <summary>
		/// Draws a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawRectangle(int, int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawRectangle(int x1, int y1, int x2, int y2, BinaryReader Colors, BinaryWriter PreviousColors)
		{
			DrawLine(x1, y1, x2, y1, Colors, PreviousColors);
			DrawLine(x2, y1, x2, y2, Colors, PreviousColors);
			DrawLine(x2, y2, x1, y2, Colors, PreviousColors);
			DrawLine(x1, y2, x1, y1, Colors, PreviousColors);
		}

		/// <summary>
		/// Draws a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		public static void DrawRectangle(int x1, int y1, int x2, int y2, ProceduralColorAlgorithm ColorAlgorithm)
		{
			DrawLine(x1, y1, x2, y1, ColorAlgorithm);
			DrawLine(x2, y1, x2, y2, ColorAlgorithm);
			DrawLine(x2, y2, x1, y2, ColorAlgorithm);
			DrawLine(x1, y2, x1, y1, ColorAlgorithm);
		}

		/// <summary>
		/// Draws a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawRectangle(int x1, int y1, int x2, int y2, ProceduralColorAlgorithm ColorAlgorithm, Color BackgroundColor, out bool Collision)
		{
			bool b;

			DrawLine(x1, y1, x2, y1, ColorAlgorithm, BackgroundColor, out Collision);

			DrawLine(x2, y1, x2, y2, ColorAlgorithm, BackgroundColor, out b);
			Collision |= b;

			DrawLine(x2, y2, x1, y2, ColorAlgorithm, BackgroundColor, out b);
			Collision |= b;

			DrawLine(x1, y2, x1, y1, ColorAlgorithm, BackgroundColor, out b);
			Collision |= b;
		}

		/// <summary>
		/// Draws a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawRectangle(int x1, int y1, int x2, int y2, ProceduralColorAlgorithm ColorAlgorithm, BinaryWriter PreviousColors)
		{
			DrawLine(x1, y1, x2, y1, ColorAlgorithm, PreviousColors);
			DrawLine(x2, y1, x2, y2, ColorAlgorithm, PreviousColors);
			DrawLine(x2, y2, x1, y2, ColorAlgorithm, PreviousColors);
			DrawLine(x1, y2, x1, y1, ColorAlgorithm, PreviousColors);
		}

		#endregion

		#region Fill Rectangle

		/// <summary>
		/// Fills a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillRectangle(int x1, int y1, int x2, int y2, Color Color)
		{
			if (!ClipBox(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			while (y1 <= y2)
				DrawScanLine(x1, x2, y1++, Color);
		}

		/// <summary>
		/// Fills a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void FillRectangle(int x1, int y1, int x2, int y2, Color Color, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipBox(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			bool b;

			Collision = false;

			while (y1 <= y2)
			{
				DrawScanLine(x1, x2, y1++, Color, BackgroundColor, out b);
				Collision |= b;
			}
		}

		/// <summary>
		/// Fills a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void FillRectangle(int x1, int y1, int x2, int y2, Color Color, BinaryWriter PreviousColors)
		{
			if (!ClipBox(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			while (y1 <= y2)
				DrawScanLine(x1, x2, y1++, Color, PreviousColors);
		}

		/// <summary>
		/// Fills a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="FillRectangle(int, int, int, int, Color, BinaryWriter"/>.</param>
		public static void FillRectangle(int x1, int y1, int x2, int y2, BinaryReader Colors)
		{
			if (!ClipBox(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			while (y1 <= y2)
				DrawScanLine(x1, x2, y1++, Colors);
		}

		/// <summary>
		/// Fills a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="FillRectangle(int, int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void FillRectangle(int x1, int y1, int x2, int y2, BinaryReader Colors, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipBox(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			bool b;

			while (y1 <= y2)
			{
				DrawScanLine(x1, x2, y1++, Colors, BackgroundColor, out b);
				Collision |= b;
			}
		}

		/// <summary>
		/// Fills a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="FillRectangle(int, int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void FillRectangle(int x1, int y1, int x2, int y2, BinaryReader Colors, BinaryWriter PreviousColors)
		{
			if (!ClipBox(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			while (y1 <= y2)
				DrawScanLine(x1, x2, y1++, Colors, PreviousColors);
		}

		/// <summary>
		/// Fills a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		public static void FillRectangle(int x1, int y1, int x2, int y2, ProceduralColorAlgorithm ColorAlgorithm)
		{
			if (!ClipBox(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			while (y1 <= y2)
				DrawScanLine(x1, x2, y1++, ColorAlgorithm);
		}

		/// <summary>
		/// Fills a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void FillRectangle(int x1, int y1, int x2, int y2, ProceduralColorAlgorithm ColorAlgorithm, Color BackgroundColor, out bool Collision)
		{
			Collision = false;

			if (!ClipBox(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			bool b;

			while (y1 <= y2)
			{
				DrawScanLine(x1, x2, y1++, ColorAlgorithm, BackgroundColor, out b);
				Collision |= b;
			}
		}

		/// <summary>
		/// Fills a rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void FillRectangle(int x1, int y1, int x2, int y2, ProceduralColorAlgorithm ColorAlgorithm, BinaryWriter PreviousColors)
		{
			if (!ClipBox(ref x1, ref y1, ref x2, ref y2, rasterClipLeft, rasterClipTop, rasterClipRight, rasterClipBottom))
				return;

			while (y1 <= y2)
				DrawScanLine(x1, x2, y1++, ColorAlgorithm, PreviousColors);
		}

		#endregion

		#region Draw Ellipse

		/// <summary>
		/// Draws an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, Color Color)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			DrawRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Color);
		}

		/// <summary>
		/// Draws an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, Color Color, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			DrawRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Color, BackgroundColor, out Collision);
		}

		/// <summary>
		/// Draws an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, Color Color, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			DrawRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Color, PreviousColors);
		}

		/// <summary>
		/// Draws an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, BinaryReader Colors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			DrawRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Colors);
		}

		/// <summary>
		/// Draws an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, BinaryReader Colors, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			DrawRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Colors, BackgroundColor, out Collision);
		}

		/// <summary>
		/// Draws an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, BinaryReader Colors, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			DrawRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Colors, PreviousColors);
		}

		/// <summary>
		/// Draws an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			DrawRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, ColorAlgorithm);
		}

		/// <summary>
		/// Draws an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			DrawRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, ColorAlgorithm, BackgroundColor, out Collision);
		}

		/// <summary>
		/// Draws an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			DrawRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, ColorAlgorithm, PreviousColors);
		}

		#endregion

		#region Fill Ellipse

		/// <summary>
		/// Fills an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, Color Color)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			FillRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Color);
		}

		/// <summary>
		/// Fills an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, Color Color, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			FillRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Color, BackgroundColor, out Collision);
		}

		/// <summary>
		/// Fills an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, Color Color, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			FillRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Color, PreviousColors);
		}

		/// <summary>
		/// Fills an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, BinaryReader Colors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			FillRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Colors);
		}

		/// <summary>
		/// Fills an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, BinaryReader Colors, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			FillRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Colors, BackgroundColor, out Collision);
		}

		/// <summary>
		/// Fills an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, BinaryReader Colors, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			FillRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, Colors, PreviousColors);
		}

		/// <summary>
		/// Fills an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			FillRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, ColorAlgorithm);
		}

		/// <summary>
		/// Fills an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			FillRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, ColorAlgorithm, BackgroundColor, out Collision);
		}

		/// <summary>
		/// Fills an ellipse
		/// </summary>
		/// <param name="CenterX">X-coordinate of the center of the ellipse.</param>
		/// <param name="CenterY">Y-coordinate of the center of the ellipse.</param>
		/// <param name="RadiusX">Radius of the ellipse, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the ellipse, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillEllipse(int CenterX, int CenterY, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			FillRoundedRectangle(CenterX - RadiusX, CenterY - RadiusY, CenterX + RadiusX, CenterY + RadiusY, RadiusX, RadiusY, ColorAlgorithm, PreviousColors);
		}

		#endregion

		#region Draw Rounded Rectangle

		/// <summary>
		/// Draws a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, Color Color)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			if (CenterX1 + 1 <= CenterX2 - 1)
			{
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y1, Color);
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y2, Color);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				DrawVerticalLine(x1, CenterY1 + 1, CenterY2 - 1, Color);
				DrawVerticalLine(x2, CenterY1 + 1, CenterY2 - 1, Color);
			}

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				Raster[CenterX1 + dx, CenterY2 + dy] = Color;
				Raster[CenterX1 + dx, CenterY1 - dy] = Color;
				Raster[CenterX2 - dx, CenterY2 + dy] = Color;
				Raster[CenterX2 - dx, CenterY1 - dy] = Color;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					Raster[CenterX1 + dx, CenterY2 + dy] = Color;
					Raster[CenterX1 + dx, CenterY1 - dy] = Color;
					Raster[CenterX2 - dx, CenterY2 + dy] = Color;
					Raster[CenterX2 - dx, CenterY1 - dy] = Color;
				}
				while (dx < 0);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				Raster[CenterX2 + dx, CenterY1 + dy] = Color;
				Raster[CenterX2 + dx, CenterY2 - dy] = Color;
				Raster[CenterX1 - dx, CenterY1 + dy] = Color;
				Raster[CenterX1 - dx, CenterY2 - dy] = Color;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					Raster[CenterX2 + dx, CenterY1 + dy] = Color;
					Raster[CenterX2 + dx, CenterY2 - dy] = Color;
					Raster[CenterX1 - dx, CenterY1 + dy] = Color;
					Raster[CenterX1 - dx, CenterY2 - dy] = Color;
				}
				while (dy < 0);
			}
		}

		/// <summary>
		/// Draws a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, Color Color, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;
			bool b;

			if (CenterX1 + 1 <= CenterX2 - 1)
			{
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y1, Color, BackgroundColor, out b);
				if (b)
					Collision = true;

				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y2, Color, BackgroundColor, out b);
				if (b)
					Collision = true;
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				DrawVerticalLine(x1, CenterY1 + 1, CenterY2 - 1, Color, BackgroundColor, out b);
				if (b)
					Collision = true;

				DrawVerticalLine(x2, CenterY1 + 1, CenterY2 - 1, Color, BackgroundColor, out b);
				if (b)
					Collision = true;
			}

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				if (Raster[CenterX1 + dx, CenterY2 + dy] != BackgroundColor ||
					Raster[CenterX1 + dx, CenterY1 - dy] != BackgroundColor ||
					Raster[CenterX2 - dx, CenterY2 + dy] != BackgroundColor ||
					Raster[CenterX2 - dx, CenterY1 - dy] != BackgroundColor)
				{
					Collision = true;
				}

				Raster[CenterX1 + dx, CenterY2 + dy] = Color;
				Raster[CenterX1 + dx, CenterY1 - dy] = Color;
				Raster[CenterX2 - dx, CenterY2 + dy] = Color;
				Raster[CenterX2 - dx, CenterY1 - dy] = Color;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					if (Raster[CenterX1 + dx, CenterY2 + dy] != BackgroundColor ||
						Raster[CenterX1 + dx, CenterY1 - dy] != BackgroundColor ||
						Raster[CenterX2 - dx, CenterY2 + dy] != BackgroundColor ||
						Raster[CenterX2 - dx, CenterY1 - dy] != BackgroundColor)
					{
						Collision = true;
					}

					Raster[CenterX1 + dx, CenterY2 + dy] = Color;
					Raster[CenterX1 + dx, CenterY1 - dy] = Color;
					Raster[CenterX2 - dx, CenterY2 + dy] = Color;
					Raster[CenterX2 - dx, CenterY1 - dy] = Color;
				}
				while (dx < 0);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				if (Raster[CenterX2 + dx, CenterY1 + dy] != BackgroundColor ||
					Raster[CenterX2 + dx, CenterY2 - dy] != BackgroundColor ||
					Raster[CenterX1 - dx, CenterY1 + dy] != BackgroundColor ||
					Raster[CenterX1 - dx, CenterY2 - dy] != BackgroundColor)
				{
					Collision = true;
				}

				Raster[CenterX2 + dx, CenterY1 + dy] = Color;
				Raster[CenterX2 + dx, CenterY2 - dy] = Color;
				Raster[CenterX1 - dx, CenterY1 + dy] = Color;
				Raster[CenterX1 - dx, CenterY2 - dy] = Color;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					if (Raster[CenterX2 + dx, CenterY1 + dy] != BackgroundColor ||
						Raster[CenterX2 + dx, CenterY2 - dy] != BackgroundColor ||
						Raster[CenterX1 - dx, CenterY1 + dy] != BackgroundColor ||
						Raster[CenterX1 - dx, CenterY2 - dy] != BackgroundColor)
					{
						Collision = true;
					}

					Raster[CenterX2 + dx, CenterY1 + dy] = Color;
					Raster[CenterX2 + dx, CenterY2 - dy] = Color;
					Raster[CenterX1 - dx, CenterY1 + dy] = Color;
					Raster[CenterX1 - dx, CenterY2 - dy] = Color;
				}
				while (dy < 0);
			}
		}

		/// <summary>
		/// Draws a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, Color Color, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			if (CenterX1 + 1 <= CenterX2 - 1)
			{
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y1, Color, PreviousColors);
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y2, Color, PreviousColors);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				DrawVerticalLine(x1, CenterY1 + 1, CenterY2 - 1, Color, PreviousColors);
				DrawVerticalLine(x2, CenterY1 + 1, CenterY2 - 1, Color, PreviousColors);
			}

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				PreviousColors.Write(Raster[CenterX1 + dx, CenterY2 + dy].ToArgb());
				PreviousColors.Write(Raster[CenterX1 + dx, CenterY1 - dy].ToArgb());
				PreviousColors.Write(Raster[CenterX2 - dx, CenterY2 + dy].ToArgb());
				PreviousColors.Write(Raster[CenterX2 - dx, CenterY1 - dy].ToArgb());

				Raster[CenterX1 + dx, CenterY2 + dy] = Color;
				Raster[CenterX1 + dx, CenterY1 - dy] = Color;
				Raster[CenterX2 - dx, CenterY2 + dy] = Color;
				Raster[CenterX2 - dx, CenterY1 - dy] = Color;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					PreviousColors.Write(Raster[CenterX1 + dx, CenterY2 + dy].ToArgb());
					PreviousColors.Write(Raster[CenterX1 + dx, CenterY1 - dy].ToArgb());
					PreviousColors.Write(Raster[CenterX2 - dx, CenterY2 + dy].ToArgb());
					PreviousColors.Write(Raster[CenterX2 - dx, CenterY1 - dy].ToArgb());

					Raster[CenterX1 + dx, CenterY2 + dy] = Color;
					Raster[CenterX1 + dx, CenterY1 - dy] = Color;
					Raster[CenterX2 - dx, CenterY2 + dy] = Color;
					Raster[CenterX2 - dx, CenterY1 - dy] = Color;
				}
				while (dx < 0);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				PreviousColors.Write(Raster[CenterX2 + dx, CenterY1 + dy].ToArgb());
				PreviousColors.Write(Raster[CenterX2 + dx, CenterY2 - dy].ToArgb());
				PreviousColors.Write(Raster[CenterX1 - dx, CenterY1 + dy].ToArgb());
				PreviousColors.Write(Raster[CenterX1 - dx, CenterY2 - dy].ToArgb());

				Raster[CenterX2 + dx, CenterY1 + dy] = Color;
				Raster[CenterX2 + dx, CenterY2 - dy] = Color;
				Raster[CenterX1 - dx, CenterY1 + dy] = Color;
				Raster[CenterX1 - dx, CenterY2 - dy] = Color;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					PreviousColors.Write(Raster[CenterX2 + dx, CenterY1 + dy].ToArgb());
					PreviousColors.Write(Raster[CenterX2 + dx, CenterY2 - dy].ToArgb());
					PreviousColors.Write(Raster[CenterX1 - dx, CenterY1 + dy].ToArgb());
					PreviousColors.Write(Raster[CenterX1 - dx, CenterY2 - dy].ToArgb());

					Raster[CenterX2 + dx, CenterY1 + dy] = Color;
					Raster[CenterX2 + dx, CenterY2 - dy] = Color;
					Raster[CenterX1 - dx, CenterY1 + dy] = Color;
					Raster[CenterX1 - dx, CenterY2 - dy] = Color;
				}
				while (dy < 0);
			}
		}

		/// <summary>
		/// Draws a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, BinaryReader Colors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			if (CenterX1 + 1 <= CenterX2 - 1)
			{
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y1, Colors);
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y2, Colors);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				DrawVerticalLine(x1, CenterY1 + 1, CenterY2 - 1, Colors);
				DrawVerticalLine(x2, CenterY1 + 1, CenterY2 - 1, Colors);
			}

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				Raster[CenterX1 + dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX1 + dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX2 - dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX2 - dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					Raster[CenterX1 + dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX1 + dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX2 - dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX2 - dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());
				}
				while (dx < 0);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				Raster[CenterX2 + dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX2 + dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX1 - dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX1 - dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					Raster[CenterX2 + dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX2 + dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX1 - dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX1 - dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());
				}
				while (dy < 0);
			}
		}

		/// <summary>
		/// Draws a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, BinaryReader Colors, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;
			bool b;

			if (CenterX1 + 1 <= CenterX2 - 1)
			{
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y1, Colors, BackgroundColor, out b);
				if (b)
					Collision = true;

				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y2, Colors, BackgroundColor, out b);
				if (b)
					Collision = true;
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				DrawVerticalLine(x1, CenterY1 + 1, CenterY2 - 1, Colors, BackgroundColor, out b);
				if (b)
					Collision = true;

				DrawVerticalLine(x2, CenterY1 + 1, CenterY2 - 1, Colors, BackgroundColor, out b);
				if (b)
					Collision = true;
			}

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				if (Raster[CenterX1 + dx, CenterY2 + dy] != BackgroundColor ||
					Raster[CenterX1 + dx, CenterY1 - dy] != BackgroundColor ||
					Raster[CenterX2 - dx, CenterY2 + dy] != BackgroundColor ||
					Raster[CenterX2 - dx, CenterY1 - dy] != BackgroundColor)
				{
					Collision = true;
				}

				Raster[CenterX1 + dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX1 + dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX2 - dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX2 - dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					if (Raster[CenterX1 + dx, CenterY2 + dy] != BackgroundColor ||
						Raster[CenterX1 + dx, CenterY1 - dy] != BackgroundColor ||
						Raster[CenterX2 - dx, CenterY2 + dy] != BackgroundColor ||
						Raster[CenterX2 - dx, CenterY1 - dy] != BackgroundColor)
					{
						Collision = true;
					}

					Raster[CenterX1 + dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX1 + dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX2 - dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX2 - dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());
				}
				while (dx < 0);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				if (Raster[CenterX2 + dx, CenterY1 + dy] != BackgroundColor ||
					Raster[CenterX2 + dx, CenterY2 - dy] != BackgroundColor ||
					Raster[CenterX1 - dx, CenterY1 + dy] != BackgroundColor ||
					Raster[CenterX1 - dx, CenterY2 - dy] != BackgroundColor)
				{
					Collision = true;
				}

				Raster[CenterX2 + dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX2 + dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX1 - dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX1 - dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					if (Raster[CenterX2 + dx, CenterY1 + dy] != BackgroundColor ||
						Raster[CenterX2 + dx, CenterY2 - dy] != BackgroundColor ||
						Raster[CenterX1 - dx, CenterY1 + dy] != BackgroundColor ||
						Raster[CenterX1 - dx, CenterY2 - dy] != BackgroundColor)
					{
						Collision = true;
					}

					Raster[CenterX2 + dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX2 + dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX1 - dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX1 - dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());
				}
				while (dy < 0);
			}
		}

		/// <summary>
		/// Draws a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, BinaryReader Colors, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			if (CenterX1 + 1 <= CenterX2 - 1)
			{
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y1, Colors, PreviousColors);
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y2, Colors, PreviousColors);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				DrawVerticalLine(x1, CenterY1 + 1, CenterY2 - 1, Colors, PreviousColors);
				DrawVerticalLine(x2, CenterY1 + 1, CenterY2 - 1, Colors, PreviousColors);
			}

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				PreviousColors.Write(Raster[CenterX1 + dx, CenterY2 + dy].ToArgb());
				PreviousColors.Write(Raster[CenterX1 + dx, CenterY1 - dy].ToArgb());
				PreviousColors.Write(Raster[CenterX2 - dx, CenterY2 + dy].ToArgb());
				PreviousColors.Write(Raster[CenterX2 - dx, CenterY1 - dy].ToArgb());

				Raster[CenterX1 + dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX1 + dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX2 - dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX2 - dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					PreviousColors.Write(Raster[CenterX1 + dx, CenterY2 + dy].ToArgb());
					PreviousColors.Write(Raster[CenterX1 + dx, CenterY1 - dy].ToArgb());
					PreviousColors.Write(Raster[CenterX2 - dx, CenterY2 + dy].ToArgb());
					PreviousColors.Write(Raster[CenterX2 - dx, CenterY1 - dy].ToArgb());

					Raster[CenterX1 + dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX1 + dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX2 - dx, CenterY2 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX2 - dx, CenterY1 - dy] = Color.FromArgb(Colors.ReadInt32());
				}
				while (dx < 0);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				PreviousColors.Write(Raster[CenterX2 + dx, CenterY1 + dy].ToArgb());
				PreviousColors.Write(Raster[CenterX2 + dx, CenterY2 - dy].ToArgb());
				PreviousColors.Write(Raster[CenterX1 - dx, CenterY1 + dy].ToArgb());
				PreviousColors.Write(Raster[CenterX1 - dx, CenterY2 - dy].ToArgb());

				Raster[CenterX2 + dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX2 + dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX1 - dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
				Raster[CenterX1 - dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					PreviousColors.Write(Raster[CenterX2 + dx, CenterY1 + dy].ToArgb());
					PreviousColors.Write(Raster[CenterX2 + dx, CenterY2 - dy].ToArgb());
					PreviousColors.Write(Raster[CenterX1 - dx, CenterY1 + dy].ToArgb());
					PreviousColors.Write(Raster[CenterX1 - dx, CenterY2 - dy].ToArgb());

					Raster[CenterX2 + dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX2 + dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX1 - dx, CenterY1 + dy] = Color.FromArgb(Colors.ReadInt32());
					Raster[CenterX1 - dx, CenterY2 - dy] = Color.FromArgb(Colors.ReadInt32());
				}
				while (dy < 0);
			}
		}

		/// <summary>
		/// Draws a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx, dy, x, y;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			if (CenterX1 + 1 <= CenterX2 - 1)
			{
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y1, ColorAlgorithm);
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y2, ColorAlgorithm);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				DrawVerticalLine(x1, CenterY1 + 1, CenterY2 - 1, ColorAlgorithm);
				DrawVerticalLine(x2, CenterY1 + 1, CenterY2 - 1, ColorAlgorithm);
			}

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				x = CenterX1 + dx;
				y = CenterY2 + dy;
				Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

				x = CenterX1 + dx;
				y = CenterY1 - dy;
				Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

				x = CenterX2 - dx;
				y = CenterY2 + dy;
				Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

				x = CenterX2 - dx;
				y = CenterY1 - dy;
				Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					x = CenterX1 + dx;
					y = CenterY2 + dy;
					Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

					x = CenterX1 + dx;
					y = CenterY1 - dy;
					Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

					x = CenterX2 - dx;
					y = CenterY2 + dy;
					Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

					x = CenterX2 - dx;
					y = CenterY1 - dy;
					Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);
				}
				while (dx < 0);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				x = CenterX2 + dx;
				y = CenterY1 + dy;
				Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

				x = CenterX2 + dx;
				y = CenterY2 - dy;
				Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

				x = CenterX1 - dx;
				y = CenterY1 + dy;
				Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

				x = CenterX1 - dx;
				y = CenterY2 - dy;
				Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					x = CenterX2 + dx;
					y = CenterY1 + dy;
					Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

					x = CenterX2 + dx;
					y = CenterY2 - dy;
					Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

					x = CenterX1 - dx;
					y = CenterY1 + dy;
					Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);

					x = CenterX1 - dx;
					y = CenterY2 - dy;
					Raster[x, y] = ColorAlgorithm(x, y, Raster[x, y]);
				}
				while (dy < 0);
			}
		}

		/// <summary>
		/// Draws a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx, dy, x, y;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;
			Color cl;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;
			bool b;

			if (CenterX1 + 1 <= CenterX2 - 1)
			{
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y1, ColorAlgorithm, BackgroundColor, out b);
				if (b)
					Collision = true;

				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y2, ColorAlgorithm, BackgroundColor, out b);
				if (b)
					Collision = true;
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				DrawVerticalLine(x1, CenterY1 + 1, CenterY2 - 1, ColorAlgorithm, BackgroundColor, out b);
				if (b)
					Collision = true;

				DrawVerticalLine(x2, CenterY1 + 1, CenterY2 - 1, ColorAlgorithm, BackgroundColor, out b);
				if (b)
					Collision = true;
			}

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				x = CenterX1 + dx;
				y = CenterY2 + dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				if (cl != BackgroundColor)
					Collision = true;

				x = CenterX1 + dx;
				y = CenterY1 - dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				if (cl != BackgroundColor)
					Collision = true;

				x = CenterX2 - dx;
				y = CenterY2 + dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				if (cl != BackgroundColor)
					Collision = true;

				x = CenterX2 - dx;
				y = CenterY1 - dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				if (cl != BackgroundColor)
					Collision = true;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					x = CenterX1 + dx;
					y = CenterY2 + dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					if (cl != BackgroundColor)
						Collision = true;

					x = CenterX1 + dx;
					y = CenterY1 - dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					if (cl != BackgroundColor)
						Collision = true;

					x = CenterX2 - dx;
					y = CenterY2 + dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					if (cl != BackgroundColor)
						Collision = true;

					x = CenterX2 - dx;
					y = CenterY1 - dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					if (cl != BackgroundColor)
						Collision = true;
				}
				while (dx < 0);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				x = CenterX2 + dx;
				y = CenterY1 + dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				if (cl != BackgroundColor)
					Collision = true;

				x = CenterX2 + dx;
				y = CenterY2 - dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				if (cl != BackgroundColor)
					Collision = true;

				x = CenterX1 - dx;
				y = CenterY1 + dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				if (cl != BackgroundColor)
					Collision = true;

				x = CenterX1 - dx;
				y = CenterY2 - dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				if (cl != BackgroundColor)
					Collision = true;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					x = CenterX2 + dx;
					y = CenterY1 + dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					if (cl != BackgroundColor)
						Collision = true;

					x = CenterX2 + dx;
					y = CenterY2 - dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					if (cl != BackgroundColor)
						Collision = true;

					x = CenterX1 - dx;
					y = CenterY1 + dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					if (cl != BackgroundColor)
						Collision = true;

					x = CenterX1 - dx;
					y = CenterY2 - dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					if (cl != BackgroundColor)
						Collision = true;
				}
				while (dy < 0);
			}
		}

		/// <summary>
		/// Draws a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx, dy, x, y;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;
			Color cl;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			if (CenterX1 + 1 <= CenterX2 - 1)
			{
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y1, ColorAlgorithm, PreviousColors);
				DrawScanLine(CenterX1 + 1, CenterX2 - 1, y2, ColorAlgorithm, PreviousColors);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				DrawVerticalLine(x1, CenterY1 + 1, CenterY2 - 1, ColorAlgorithm, PreviousColors);
				DrawVerticalLine(x2, CenterY1 + 1, CenterY2 - 1, ColorAlgorithm, PreviousColors);
			}

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				x = CenterX1 + dx;
				y = CenterY2 + dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				PreviousColors.Write(cl.ToArgb());

				x = CenterX1 + dx;
				y = CenterY1 - dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				PreviousColors.Write(cl.ToArgb());

				x = CenterX2 - dx;
				y = CenterY2 + dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				PreviousColors.Write(cl.ToArgb());

				x = CenterX2 - dx;
				y = CenterY1 - dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				PreviousColors.Write(cl.ToArgb());

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					x = CenterX1 + dx;
					y = CenterY2 + dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					PreviousColors.Write(cl.ToArgb());

					x = CenterX1 + dx;
					y = CenterY1 - dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					PreviousColors.Write(cl.ToArgb());

					x = CenterX2 - dx;
					y = CenterY2 + dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					PreviousColors.Write(cl.ToArgb());

					x = CenterX2 - dx;
					y = CenterY1 - dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					PreviousColors.Write(cl.ToArgb());
				}
				while (dx < 0);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				x = CenterX2 + dx;
				y = CenterY1 + dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				PreviousColors.Write(cl.ToArgb());

				x = CenterX2 + dx;
				y = CenterY2 - dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				PreviousColors.Write(cl.ToArgb());

				x = CenterX1 - dx;
				y = CenterY1 + dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				PreviousColors.Write(cl.ToArgb());

				x = CenterX1 - dx;
				y = CenterY2 - dy;
				cl = Raster[x, y];
				Raster[x, y] = ColorAlgorithm(x, y, cl);
				PreviousColors.Write(cl.ToArgb());

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}

					x = CenterX2 + dx;
					y = CenterY1 + dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					PreviousColors.Write(cl.ToArgb());

					x = CenterX2 + dx;
					y = CenterY2 - dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					PreviousColors.Write(cl.ToArgb());

					x = CenterX1 - dx;
					y = CenterY1 + dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					PreviousColors.Write(cl.ToArgb());

					x = CenterX1 - dx;
					y = CenterY2 - dy;
					cl = Raster[x, y];
					Raster[x, y] = ColorAlgorithm(x, y, cl);
					PreviousColors.Write(cl.ToArgb());
				}
				while (dy < 0);
			}
		}

		#endregion

		#region Fill Rounded Rectangle

		/// <summary>
		/// Fills a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, Color Color)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx0;
			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx0 = dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Color);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Color);

						dx0 = ++dx;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Color);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Color);

						dx0 = dx;
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dx < 0);

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Color);
				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Color);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Color);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Color);

						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Color);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Color);

						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dy < 0);

				DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1, Color);
				if (CenterY1 < CenterY2)
					DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2, Color);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
				FillRectangle(x1, CenterY1 + 1, x2, CenterY2 - 1, Color);
		}

		/// <summary>
		/// Fills a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, Color Color, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx0;
			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;
			bool b;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx0 = dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Color, BackgroundColor, out b);
						if (b)
							Collision = true;

						if (dy > 0 || CenterY1 < CenterY2)
						{
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Color, BackgroundColor, out b);
							if (b)
								Collision = true;
						}

						dx0 = ++dx;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Color, BackgroundColor, out b);
						if (b)
							Collision = true;

						if (dy > 0 || CenterY1 < CenterY2)
						{
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Color, BackgroundColor, out b);
							if (b)
								Collision = true;
						}

						dx0 = dx;
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dx < 0);

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Color, BackgroundColor, out b);
				if (b)
					Collision = true;

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Color, BackgroundColor, out b);
				if (b)
					Collision = true;
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Color, BackgroundColor, out b);
						if (b)
							Collision = true;

						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Color, BackgroundColor, out b);
						if (b)
							Collision = true;

						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Color, BackgroundColor, out b);
						if (b)
							Collision = true;

						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Color, BackgroundColor, out b);
						if (b)
							Collision = true;

						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dy < 0);

				DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1, Color, BackgroundColor, out b);
				if (b)
					Collision = true;

				if (CenterY1 < CenterY2)
				{
					DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2, Color, BackgroundColor, out b);
					if (b)
						Collision = true;
				}
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				FillRectangle(x1, CenterY1 + 1, x2, CenterY2 - 1, Color, BackgroundColor, out b);
				if (b)
					Collision = true;
			}
		}

		/// <summary>
		/// Fills a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, Color Color, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx0;
			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx0 = dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Color, PreviousColors);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Color, PreviousColors);

						dx0 = ++dx;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Color, PreviousColors);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Color, PreviousColors);

						dx0 = dx;
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dx < 0);

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Color, PreviousColors);
				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Color, PreviousColors);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Color, PreviousColors);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Color, PreviousColors);

						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Color, PreviousColors);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Color, PreviousColors);

						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dy < 0);

				DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1, Color, PreviousColors);
				if (CenterY1 < CenterY2)
					DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2, Color, PreviousColors);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
				FillRectangle(x1, CenterY1 + 1, x2, CenterY2 - 1, Color, PreviousColors);
		}

		/// <summary>
		/// Fills a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, BinaryReader Colors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx0;
			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx0 = dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Colors);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Colors);

						dx0 = ++dx;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Colors);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Colors);

						dx0 = dx;
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dx < 0);

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Colors);
				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Colors);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Colors);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Colors);

						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Colors);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Colors);

						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dy < 0);

				DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1, Colors);
				if (CenterY1 < CenterY2)
					DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2, Colors);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
				FillRectangle(x1, CenterY1 + 1, x2, CenterY2 - 1, Colors);
		}

		/// <summary>
		/// Fills a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, BinaryReader Colors, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx0;
			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;
			bool b;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx0 = dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Colors, BackgroundColor, out b);
						if (b)
							Collision = true;

						if (dy > 0 || CenterY1 < CenterY2)
						{
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Colors, BackgroundColor, out b);
							if (b)
								Collision = true;
						}

						dx0 = ++dx;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Colors, BackgroundColor, out b);
						if (b)
							Collision = true;

						if (dy > 0 || CenterY1 < CenterY2)
						{
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Colors, BackgroundColor, out b);
							if (b)
								Collision = true;
						}

						dx0 = dx;
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dx < 0);

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Colors, BackgroundColor, out b);
				if (b)
					Collision = true;

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Colors, BackgroundColor, out b);
				if (b)
					Collision = true;
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Colors, BackgroundColor, out b);
						if (b)
							Collision = true;

						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Colors, BackgroundColor, out b);
						if (b)
							Collision = true;

						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Colors, BackgroundColor, out b);
						if (b)
							Collision = true;

						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Colors, BackgroundColor, out b);
						if (b)
							Collision = true;

						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dy < 0);

				DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1, Colors, BackgroundColor, out b);
				if (b)
					Collision = true;

				if (CenterY1 < CenterY2)
				{
					DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2, Colors, BackgroundColor, out b);
					if (b)
						Collision = true;
				}
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				FillRectangle(x1, CenterY1 + 1, x2, CenterY2 - 1, Colors, BackgroundColor, out b);
				if (b)
					Collision = true;
			}
		}

		/// <summary>
		/// Fills a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, BinaryReader Colors, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx0;
			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx0 = dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Colors, PreviousColors);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Colors, PreviousColors);

						dx0 = ++dx;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Colors, PreviousColors);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Colors, PreviousColors);

						dx0 = dx;
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dx < 0);

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, Colors, PreviousColors);
				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, Colors, PreviousColors);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Colors, PreviousColors);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Colors, PreviousColors);

						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, Colors, PreviousColors);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, Colors, PreviousColors);

						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dy < 0);

				DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1, Colors, PreviousColors);
				if (CenterY1 < CenterY2)
					DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2, Colors, PreviousColors);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
				FillRectangle(x1, CenterY1 + 1, x2, CenterY2 - 1, Colors, PreviousColors);
		}

		/// <summary>
		/// Fills a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx0;
			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx0 = dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, ColorAlgorithm);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, ColorAlgorithm);

						dx0 = ++dx;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, ColorAlgorithm);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, ColorAlgorithm);

						dx0 = dx;
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dx < 0);

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, ColorAlgorithm);
				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, ColorAlgorithm);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, ColorAlgorithm);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, ColorAlgorithm);

						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, ColorAlgorithm);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, ColorAlgorithm);

						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dy < 0);

				DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1, ColorAlgorithm);
				if (CenterY1 < CenterY2)
					DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2, ColorAlgorithm);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
				FillRectangle(x1, CenterY1 + 1, x2, CenterY2 - 1, ColorAlgorithm);
		}

		/// <summary>
		/// Fills a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx0;
			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;
			bool b;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx0 = dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, ColorAlgorithm, BackgroundColor, out b);
						if (b)
							Collision = true;

						if (dy > 0 || CenterY1 < CenterY2)
						{
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, ColorAlgorithm, BackgroundColor, out b);
							if (b)
								Collision = true;
						}

						dx0 = ++dx;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, ColorAlgorithm, BackgroundColor, out b);
						if (b)
							Collision = true;

						if (dy > 0 || CenterY1 < CenterY2)
						{
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, ColorAlgorithm, BackgroundColor, out b);
							if (b)
								Collision = true;
						}

						dx0 = dx;
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dx < 0);

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, ColorAlgorithm, BackgroundColor, out b);
				if (b)
					Collision = true;

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, ColorAlgorithm, BackgroundColor, out b);
				if (b)
					Collision = true;
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, ColorAlgorithm, BackgroundColor, out b);
						if (b)
							Collision = true;

						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, ColorAlgorithm, BackgroundColor, out b);
						if (b)
							Collision = true;

						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, ColorAlgorithm, BackgroundColor, out b);
						if (b)
							Collision = true;

						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, ColorAlgorithm, BackgroundColor, out b);
						if (b)
							Collision = true;

						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dy < 0);

				DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1, ColorAlgorithm, BackgroundColor, out b);
				if (b)
					Collision = true;

				if (CenterY1 < CenterY2)
				{
					DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2, ColorAlgorithm, BackgroundColor, out b);
					if (b)
						Collision = true;
				}
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
			{
				FillRectangle(x1, CenterY1 + 1, x2, CenterY2 - 1, ColorAlgorithm, BackgroundColor, out b);
				if (b)
					Collision = true;
			}
		}

		/// <summary>
		/// Fills a rounded rectangle using coordinates of two opposing corners.
		/// </summary>
		/// <param name="x1">X-coordinate of first corner.</param>
		/// <param name="y1">Y-coordinate of first corner.</param>
		/// <param name="x2">X-coordinate of second corner.</param>
		/// <param name="y2">Y-coordinate of second corner.</param>
		/// <param name="RadiusX">Radius of the corners of the rounded rectangle, along the X-axis.</param>
		/// <param name="RadiusY">Radius of the corners of the rounded rectangle, along the Y-axis.</param>
		/// <param name="Color">Color to use.</param>
		public static void FillRoundedRectangle(int x1, int y1, int x2, int y2, int RadiusX, int RadiusY, ProceduralColorAlgorithm ColorAlgorithm, BinaryWriter PreviousColors)
		{
			if (RadiusX < 0 || RadiusY < 0)
				return;

			int dx0;
			int dx, dy;
			int dxprim, dyprim;
			double cx, cy, d1, d2, d3, t, uprim, tprim;

			if (x2 < x1)
			{
				dx = x1;
				x1 = x2;
				x2 = dx;
			}

			if (y2 < y1)
			{
				dy = y1;
				y1 = y2;
				y2 = dy;
			}

			int CenterX1 = x1 + RadiusX;
			int CenterX2 = x2 - RadiusX;
			int CenterY1 = y1 + RadiusY;
			int CenterY2 = y2 - RadiusY;

			cx = 1.0 / (RadiusX * RadiusX + 0.01);
			cy = 1.0 / (RadiusY * RadiusY + 0.01);

			if (RadiusX > RadiusY)
			{
				dx0 = dx = -RadiusX;
				dy = 0;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, ColorAlgorithm, PreviousColors);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, ColorAlgorithm, PreviousColors);

						dx0 = ++dx;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, ColorAlgorithm, PreviousColors);

						if (dy > 0 || CenterY1 < CenterY2)
							DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, ColorAlgorithm, PreviousColors);

						dx0 = dx;
						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dx < 0);

				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY2 + dy, ColorAlgorithm, PreviousColors);
				DrawScanLine(CenterX1 + dx0, CenterX2 - dx0, CenterY1 - dy, ColorAlgorithm, PreviousColors);
			}
			else
			{
				dx = 0;
				dy = -RadiusY;
				dxprim = dx + 1;
				dyprim = dy + 1;

				t = dx * dx * cx;
				tprim = dxprim * dxprim * cx;
				uprim = dyprim * dyprim * cy;
				do
				{
					d1 = Math.Abs(tprim + dy * dy * cy - 1);  // Pixel to the right
					d2 = Math.Abs(tprim + uprim - 1);         // Pixel down to the right
					d3 = Math.Abs(t + uprim - 1);             // Pixel downwards

					if (d1 <= Math.Min(d2, d3))
					{
						dx++;
						t = tprim;
						dxprim++;
						tprim = dxprim * dxprim * cx;
					}
					else if (d2 <= Math.Min(d1, d3))
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, ColorAlgorithm, PreviousColors);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, ColorAlgorithm, PreviousColors);

						dx++;
						dy++;
						t = tprim;
						dxprim++;
						dyprim++;
						tprim = dxprim * dxprim * cx;
						uprim = dyprim * dyprim * cy;
					}
					else
					{
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1 + dy, ColorAlgorithm, PreviousColors);
						DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2 - dy, ColorAlgorithm, PreviousColors);

						dy++;
						dyprim++;
						uprim = dyprim * dyprim * cy;
					}
				}
				while (dy < 0);

				DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY1, ColorAlgorithm, PreviousColors);
				if (CenterY1 < CenterY2)
					DrawScanLine(CenterX2 + dx, CenterX1 - dx, CenterY2, ColorAlgorithm, PreviousColors);
			}

			if (CenterY1 + 1 <= CenterY2 - 1)
				FillRectangle(x1, CenterY1 + 1, x2, CenterY2 - 1, ColorAlgorithm, PreviousColors);
		}

		#endregion

		#region Draw Polygon

		/// <summary>
		/// Draws a polygon using coordinates of two opposing corners.
		/// </summary>
		/// <param name="Points">Points in the polygon.</param>
		/// <param name="Color">Color to use.</param>
		public static void DrawPolygon(Point[] Points, Color Color)
		{
			int c = Points.Length;
			if (c <= 1)
				return;

			Point Prev = Points[c - 1];

			foreach (Point P in Points)
			{
				DrawLine(Prev.X, Prev.Y, P.X, P.Y, Color);
				Prev = P;
			}
		}

		/// <summary>
		/// Draws a polygon using coordinates of two opposing corners.
		/// </summary>
		/// <param name="Points">Points in the polygon.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawPolygon(Point[] Points, Color Color, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			int c = Points.Length;
			if (c <= 1)
				return;

			MemoryStream ms = new MemoryStream();
			BinaryWriter w = new BinaryWriter(ms);
			Point Prev = Points[c - 1];

			foreach (Point P in Points)
			{
				DrawLine(Prev.X, Prev.Y, P.X, P.Y, Color, w);
				Prev = P;
			}

			w.Flush();
			c = (int)(ms.Position / 4);
			ms.Position = 0;

			BinaryReader r = new BinaryReader(ms);
			int bg = BackgroundColor.ToArgb();
			int cl = Color.ToArgb();
			int i;

			while (c > 0)
			{
				i = r.ReadInt32();
				if (i != bg && i != cl)
				{
					Collision = true;
					break;
				}
			}
		}

		/// <summary>
		/// Draws a polygon using coordinates of two opposing corners.
		/// </summary>
		/// <param name="Points">Points in the polygon.</param>
		/// <param name="Color">Color to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawPolygon(Point[] Points, Color Color, BinaryWriter PreviousColors)
		{
			int c = Points.Length;
			if (c <= 1)
				return;

			Point Prev = Points[c - 1];

			foreach (Point P in Points)
			{
				DrawLine(Prev.X, Prev.Y, P.X, P.Y, Color, PreviousColors);
				Prev = P;
			}
		}

		/// <summary>
		/// Draws a polygon using coordinates of two opposing corners.
		/// </summary>
		/// <param name="Points">Points in the polygon.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawPolygon(int, int, int, int, Color, BinaryWriter"/>.</param>
		public static void DrawPolygon(Point[] Points, BinaryReader Colors)
		{
			int c = Points.Length;
			if (c <= 1)
				return;

			Point Prev = Points[c - 1];

			foreach (Point P in Points)
			{
				DrawLine(Prev.X, Prev.Y, P.X, P.Y, Colors);
				Prev = P;
			}
		}

		/// <summary>
		/// Draws a polygon using coordinates of two opposing corners.
		/// </summary>
		/// <param name="Points">Points in the polygon.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawPolygon(int, int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawPolygon(Point[] Points, BinaryReader Colors, Color BackgroundColor, out bool Collision)
		{
			Collision = false;
			int c = Points.Length;
			if (c <= 1)
				return;

			MemoryStream ms = new MemoryStream();
			BinaryWriter w = new BinaryWriter(ms);
			Point Prev = Points[c - 1];
			long Pos = Colors.BaseStream.Position;

			foreach (Point P in Points)
			{
				DrawLine(Prev.X, Prev.Y, P.X, P.Y, Colors, w);
				Prev = P;
			}

			w.Flush();
			c = (int)(ms.Position / 4);
			ms.Position = 0;

			long Pos2 = Colors.BaseStream.Position;
			Colors.BaseStream.Position = Pos;

			BinaryReader r = new BinaryReader(ms);
			int bg = BackgroundColor.ToArgb();
			int cl;
			int i;

			while (c > 0)
			{
				cl = Colors.ReadInt32();
				i = r.ReadInt32();
				if (i != bg && i != cl)
				{
					Collision = true;
					break;
				}
			}

			Colors.BaseStream.Position = Pos2;
		}

		/// <summary>
		/// Draws a polygon using coordinates of two opposing corners.
		/// </summary>
		/// <param name="Points">Points in the polygon.</param>
		/// <param name="Colors">Colors to use when drawing the line. Such a set of colors can be obtained by previously having called
		/// <see cref="DrawPolygon(int, int, int, int, Color, BinaryWriter"/>.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawPolygon(Point[] Points, BinaryReader Colors, BinaryWriter PreviousColors)
		{
			int c = Points.Length;
			if (c <= 1)
				return;

			Point Prev = Points[c - 1];

			foreach (Point P in Points)
			{
				DrawLine(Prev.X, Prev.Y, P.X, P.Y, Colors, PreviousColors);
				Prev = P;
			}
		}

		/// <summary>
		/// Draws a polygon using coordinates of two opposing corners.
		/// </summary>
		/// <param name="Points">Points in the polygon.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		public static void DrawPolygon(Point[] Points, ProceduralColorAlgorithm ColorAlgorithm)
		{
			int c = Points.Length;
			if (c <= 1)
				return;

			Point Prev = Points[c - 1];

			foreach (Point P in Points)
			{
				DrawLine(Prev.X, Prev.Y, P.X, P.Y, ColorAlgorithm);
				Prev = P;
			}
		}

		/*
		/// <summary>
		/// Draws a polygon using coordinates of two opposing corners.
		/// </summary>
		/// <param name="Points">Points in the polygon.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="BackgroundColor">Expected background color</param>
		/// <param name="Collision">If any of the pixels overwritten by the line is NOT the background color.</param>
		public static void DrawPolygon(Point[] Points, ProceduralColorAlgorithm ColorAlgorithm, Color BackgroundColor, out bool Collision)
		{
			// TODO
		}*/

		/// <summary>
		/// Draws a polygon using coordinates of two opposing corners.
		/// </summary>
		/// <param name="Points">Points in the polygon.</param>
		/// <param name="ColorAlgorithm">Coloring algorithm to use.</param>
		/// <param name="PreviousColors">Returns an enumerable set of colors representing the colors overwritten when drawing the line.</param>
		public static void DrawPolygon(Point[] Points, ProceduralColorAlgorithm ColorAlgorithm, BinaryWriter PreviousColors)
		{
			int c = Points.Length;
			if (c <= 1)
				return;

			Point Prev = Points[c - 1];

			foreach (Point P in Points)
			{
				DrawLine(Prev.X, Prev.Y, P.X, P.Y, ColorAlgorithm, PreviousColors);
				Prev = P;
			}
		}

		#endregion

		#endregion

		#region File Handling

		/// <summary>
		/// Loads a binary file.
		/// </summary>
		/// <param name="FileName">Name of file to load.</param>
		/// <returns>Binary contents of file.</returns>
		public static byte[] LoadBinaryFile(string FileName)
		{
			using (FileStream f = File.OpenRead(FileName))
			{
				int c = (int)f.Length;
				byte[] Result = new byte[c];

				f.Read(Result, 0, c);

				return Result;
			}
		}

		/// <summary>
		/// Loads an image file.
		/// </summary>
		/// <param name="FileName">Name of file to load.</param>
		/// <returns>Image.</returns>
		public static Image LoadImageFile(string FileName)
		{
			using (FileStream f = File.OpenRead(FileName))
			{
				return Image.FromStream(f);
			}
		}

		/// <summary>
		/// Loads a text file.
		/// </summary>
		/// <param name="FileName">Name of file to load.</param>
		/// <returns>Text contents of file.</returns>
		public static string LoadTextFile(string FileName)
		{
			using (FileStream f = File.OpenRead(FileName))
			{
				using (StreamReader r = new StreamReader(f))
				{
					return r.ReadToEnd();
				}
			}
		}

		/// <summary>
		/// Loads an XML file.
		/// </summary>
		/// <param name="FileName">Name of file to load.</param>
		/// <param name="Schemas">Schemas to use to validate the XML file.</param>
		/// <returns>XML contents of file.</returns>
		public static XmlDocument LoadXmlFile(string FileName, params XmlSchema[] Schemas)
		{
			using (FileStream f = File.OpenRead(FileName))
			{
				XmlDocument Result = new XmlDocument();
				Result.Load(f);

				if (Schemas.Length > 0)
				{
					foreach (XmlSchema Schema in Schemas)
						Result.Schemas.Add(Schema);

					List<ValidationEventArgs> ReportedMessages = new List<ValidationEventArgs>();
					Result.Validate((s, e) => ReportedMessages.Add(e));
					CheckMessages(ReportedMessages);
				}

				return Result;
			}
		}

		/// <summary>
		/// Loads a binary file.
		/// </summary>
		/// <param name="FileName">Name of file to load.</param>
		/// <returns>Binary contents of file.</returns>
		public static XmlSchema LoadXmlSchemaFile(string FileName)
		{
			using (FileStream f = File.OpenRead(FileName))
			{
				List<ValidationEventArgs> ReportedMessages = new List<ValidationEventArgs>();
				XmlSchema Result = XmlSchema.Read(f, (s, e) => ReportedMessages.Add(e));
				CheckMessages(ReportedMessages);
				return Result;
			}
		}

		private static void CheckMessages(IEnumerable<ValidationEventArgs> Reported)
		{
			StringBuilder sb = null;

			foreach (ValidationEventArgs e in Reported)
			{
				if (e.Severity == XmlSeverityType.Error)
				{
					if (sb == null)
						sb = new StringBuilder();

					sb.AppendLine(e.Message);
				}
			}

			if (sb != null)
				throw new Exception(sb.ToString());
		}

		#endregion

		#region Mouse

		/// <summary>
		/// Gets the position of the current mouse pointer.
		/// </summary>
		/// <returns></returns>
		public static Point GetMousePointer()
		{
			return MouseToScreenCoordinates(OpenTK.Input.Mouse.GetState(0));
		}

		private static Point MouseToScreenCoordinates(OpenTK.Input.MouseState State)
		{
			//int X = ((State.X - leftMargin) * rasterWidth) / visibleScreenWidth;
			//int Y = ((State.Y - topMargin) * rasterHeight) / visibleScreenHeight;
			int X = (State.X * rasterWidth) / screenWidth;
			int Y = (State.Y * rasterHeight) / screenHeight;

			return new Point(X, Y);
		}

		private static void wnd_MouseUp(object sender, OpenTK.Input.MouseButtonEventArgs e)
		{
			MouseEventHandler h = OnMouseUp;
			if (h != null)
			{
				try
				{
					OpenTK.Input.MouseState State = e.Mouse;
					MouseEventArgs e2 = new MouseEventArgs(MouseToScreenCoordinates(State),
						State.LeftButton == OpenTK.Input.ButtonState.Pressed,
						State.MiddleButton == OpenTK.Input.ButtonState.Pressed,
						State.RightButton == OpenTK.Input.ButtonState.Pressed);

					h(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}
		}

		private static void wnd_MouseMove(object sender, OpenTK.Input.MouseMoveEventArgs e)
		{
			MouseEventHandler h = OnMouseMove;
			if (h != null)
			{
				try
				{
					OpenTK.Input.MouseState State = e.Mouse;
					MouseEventArgs e2 = new MouseEventArgs(MouseToScreenCoordinates(State),
						State.LeftButton == OpenTK.Input.ButtonState.Pressed,
						State.MiddleButton == OpenTK.Input.ButtonState.Pressed,
						State.RightButton == OpenTK.Input.ButtonState.Pressed);

					h(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}
		}

		private static void wnd_MouseLeave(object sender, EventArgs e)
		{
			EventHandler h = OnMouseLeave;
			if (h != null)
			{
				try
				{
					h(sender, e);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}
		}

		private static void wnd_MouseEnter(object sender, EventArgs e)
		{
			EventHandler h = OnMouseEnter;
			if (h != null)
			{
				try
				{
					h(sender, e);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}
		}

		private static void wnd_MouseDown(object sender, OpenTK.Input.MouseButtonEventArgs e)
		{
			MouseEventHandler h = OnMouseDown;
			if (h != null)
			{
				try
				{
					OpenTK.Input.MouseState State = e.Mouse;
					MouseEventArgs e2 = new MouseEventArgs(MouseToScreenCoordinates(State),
						State.LeftButton == OpenTK.Input.ButtonState.Pressed,
						State.MiddleButton == OpenTK.Input.ButtonState.Pressed,
						State.RightButton == OpenTK.Input.ButtonState.Pressed);

					h(sender, e2);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(ex.StackTrace.ToString());
				}
			}
		}

		#endregion

		/*
         * TODO:
         * 
         * Update events.
         * Layers
         * scrolling & paralax
         * screen & layer direct access
         * split screen
         * 
         * Graphics:
         * FillFlood(x,y,Color)
         * FillPolygon(IEnumerable<Point>, Color)
         * DrawImage(x,y,Image)
         * DrawImage(x1,y1,x2,y2,Image)
         * DrawText(x,y,String)     Uses console font
         * DrawText(x,y,String,Font)
         * 
         * 
         * Examples:
         * Mandelbrot (iteration)
         * Julia (iteration)
         * Koch (recursion)
         * 
         * Games:
         * Text adventue
         * Arcanoid
         * Scramble
         * Mask
         * Myran
         * Thrust
         * Space Invaders
         * Load Runner
         * Tanks
         * Car race game.
         * Gorillaz
		 * PacMan
        */
	}
}