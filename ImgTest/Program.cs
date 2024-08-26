
using HalfMaid.Img;

namespace ImgTest
{
	internal static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			for (int r = 0; r < 256; r++)
			{
				for (int g = 0; g < 256; g++)
				{
					for (int b = 0; b < 256; b++)
					{
						Color32 c = new Color32(r, g, b, 255);
						(float hue, float sat, float brt) = c.ToHsb();
						Color32 c2 = Color32.FromHsb(hue, sat, brt);
						if (Math.Abs(c.R - c2.R) >= 1
							|| Math.Abs(c.G - c2.G) >= 1
							|| Math.Abs(c.B - c2.B) >= 1)
							System.Diagnostics.Debug.WriteLine($"Fail: {c} != {c2}");
					}
				}
			}

			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			ApplicationConfiguration.Initialize();
			Application.Run(new MainForm());
		}
	}
}