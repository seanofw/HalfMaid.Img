using HalfMaid.Img;
using HalfMaid.Img.Gdi;
using OpenTK.Mathematics;
using HalfMaid.Img.FileFormats.Png;

namespace ImgTest
{
	public partial class MainForm : Form
	{
		public PureImage32 Image { get; private set; } = PureImage32.Empty;
		public Bitmap? Bitmap { get; private set; }

		public List<(PureImage32 Image, string Description)> UndoList = new List<(PureImage32 Image, string Description)>();
		public List<(PureImage32 Image, string Description)> RedoList = new List<(PureImage32 Image, string Description)>();
		private string? _lastAction;

		private string? _filename;

		public MainForm()
		{
			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			Bitmap?.Dispose();
			Bitmap = null;

			Image = PureImage32.Empty;
		}

		private void Exit_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void Open_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();

			openFileDialog.Filter =
				"All image files|*.jpg;*.jpe;*.jpeg;*.gif;*.png;*.bmp;*.tga;*.ase;*.aseprite"
				+ "|JPEG images (*.jpg;*.jpe;*.jpeg)|*.jpg;*.jpe;*.jpeg"
				+ "|PNG images (*.png)|*.png"
				+ "|GIF images (*.gif)|*.gif"
				+ "|Windows bitmaps (*.bmp)|*.bmp"
				+ "|Targa images (*.tga)|*.tga"
				+ "|Aseprite images (*.ase;*.aseprite)|*.ase;*.aseprite"
				+ "|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 1;
			openFileDialog.Multiselect = false;
			openFileDialog.Title = "Open image file";
			openFileDialog.CheckFileExists = true;
			openFileDialog.CheckPathExists = true;
			openFileDialog.AutoUpgradeEnabled = true;

			DialogResult result = openFileDialog.ShowDialog(this);

			if (result != DialogResult.OK)
				return;

			LoadImage(openFileDialog.FileName);
		}

		private void SaveAs_Click(object sender, EventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();

			saveFileDialog.Filter =
				"JPEG images (*.jpg;*.jpe;*.jpeg)|*.jpg;*.jpe;*.jpeg"
				+ "|PNG images (*.png)|*.png"
				+ "|GIF images (*.gif)|*.gif"
				+ "|Windows bitmaps (*.bmp)|*.bmp"
				+ "|Targa images (*.tga)|*.tga";
			saveFileDialog.FilterIndex = 1;
			saveFileDialog.Title = "Save image file";
			saveFileDialog.OverwritePrompt = true;
			saveFileDialog.AddExtension = true;
			saveFileDialog.CheckPathExists = true;
			saveFileDialog.AutoUpgradeEnabled = true;
			saveFileDialog.FileName = !string.IsNullOrEmpty(_filename)
				? Path.GetFileName(_filename) : string.Empty;
			saveFileDialog.InitialDirectory = !string.IsNullOrEmpty(_filename)
				? (Path.GetDirectoryName(_filename) ?? string.Empty) : string.Empty;

			DialogResult result = saveFileDialog.ShowDialog(this);

			if (result != DialogResult.OK)
				return;

			ImageFormat format = saveFileDialog.FilterIndex switch
			{
				1 => ImageFormat.Jpeg,
				2 => ImageFormat.Png,
				3 => ImageFormat.Gif,
				4 => ImageFormat.Bmp,
				5 => ImageFormat.Targa,
				_ => throw new InvalidOperationException(),
			};

			SaveImage(saveFileDialog.FileName, format);
		}

		private void LoadImage(string fileName)
		{
			PureImage32 image;
			try
			{
				image = PureImage32.LoadFile(fileName)!;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Cannot load image: " + ex.Message, "Error", MessageBoxButtons.OK);
				return;
			}

			if (image == PureImage32.Empty)
			{
				MessageBox.Show("Cannot load image: Unsupported file format.", "Error", MessageBoxButtons.OK);
				return;
			}

			Image = image;
			_lastAction = "Load image";
			_filename = fileName;

			UndoList.Clear();
			RedoList.Clear();

			UpdateImage();
		}

		private void SaveImage(string fileName, ImageFormat imageFormat)
		{
			try
			{
				Image.SaveFile(fileName, imageFormat,
					new PngSaveOptions { CompressionLevel = System.IO.Compression.CompressionLevel.SmallestSize });
			}
			catch (Exception ex)
			{
				MessageBox.Show("Cannot save image: " + ex.Message, "Error", MessageBoxButtons.OK);
				return;
			}
			_filename = fileName;
		}

		private void UpdateImage()
		{
			Bitmap?.Dispose();
			Bitmap = null;

			PureImage32 image2 = Image.ResampleToFit(new Vector2i(ImageBox.Width, ImageBox.Height));
			int leftoverX = ImageBox.Width - image2.Width;
			int leftoverY = ImageBox.Height - image2.Height;
			image2 = image2.Pad(leftoverX / 2, leftoverY / 2, leftoverX - (leftoverX / 2), leftoverY - (leftoverY / 2),
				Color32.Black);

			ImageBox.Image = image2.ToBitmap(false);
		}

		private void MainForm_SizeChanged(object sender, EventArgs e)
		{
			UpdateImage();
		}

		private void Edit_Undo_Click(object sender, EventArgs e)
		{
			if (UndoList.Any())
			{
				RedoList.Insert(0, (Image, _lastAction ?? string.Empty));
				Image = PureImage32.Empty;
				_lastAction = null;

				(Image, _lastAction) = UndoList.First();
				UndoList.RemoveAt(0);

				UpdateImage();
			}
		}

		private void Edit_Redo_Click(object sender, EventArgs e)
		{
			if (RedoList.Any())
			{
				UndoList.Insert(0, (Image, _lastAction ?? string.Empty));
				Image = PureImage32.Empty;

				(Image, _lastAction) = RedoList.First();
				RedoList.RemoveAt(0);

				UpdateImage();
			}
		}

		private void UpdateImageWithUndo(Action<Image32> modifyImageInPlace, string description)
		{
			Image32 copy = Image.ToImage32();
			modifyImageInPlace(copy);
			UpdateImageWithUndo(copy, description);
		}

		private void UpdateImageWithUndo(PureImage32 newImage, string description)
		{
			UndoList.Insert(0, (Image, _lastAction ?? string.Empty));
			Image = PureImage32.Empty;
			_lastAction = null;

			const int UndoLimit = 10;
			if (UndoList.Count > UndoLimit)
				UndoList.RemoveRange(UndoLimit, UndoList.Count - UndoLimit);

			RedoList.Clear();

			Image = newImage;
			_lastAction = description;

			UpdateImage();
		}

		private void EnableDisableUndoRedo()
		{
			Edit_Undo.Enabled = UndoList.Any();
			Edit_Undo.Text = UndoList.Any() ? $"Undo {_lastAction}" : "Undo";
			Edit_Redo.Enabled = RedoList.Any();
			Edit_Redo.Text = RedoList.Any() ? $"Redo {RedoList[0].Description}" : "Redo";
			Edit_Copy.Enabled = Image.Width > 0 && Image.Height > 0;
			Edit_Paste.Enabled = ClipboardImage.Exists();
		}

		private void Orientation_FlipHorizontally_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.FlipHorz(), "Flip Horizontally");
		}

		private void Orientation_FlipHorizontallyInPlace_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(image => image.FlipHorz(), "Flip Horizontally (inplace)");
		}

		private void Orientation_FlipVertically_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.FlipVert(), "Flip Vertically");
		}

		private void Orientation_FlipVerticallyInPlace_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(image => image.FlipVert(), "Flip Vertically (inplace)");
		}

		private void Orientation_Rotate90Clockwise_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.Rotate90(), "Rotate 90° Clockwise");
		}

		private void Orientation_Rotate90Counterclockwise_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.Rotate90CCW(), "Rotate 90° Counterclockwise");
		}

		private void Orientation_Rotate180_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.Rotate180(), "Rotate 180°");
		}

		private void Orientation_Rotate180InPlace_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(image => image.Rotate180(), "Rotate 180° (inplace)");
		}

		private void Effects_Invert_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.Invert(), "Invert");
		}

		private void Effects_Grayscale_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.Grayscale(), "Grayscale");
		}

		private void Effects_Sepia_Click(object sender, EventArgs e)
		{
			AmountResult? result = AmountDialog.Show(this, "Sepia", 0.125);
			if (result == null) return;

			UpdateImageWithUndo(Image.Sepia(result.Value), "Sepia");
		}

		private void Effects_Desaturate_Click(object sender, EventArgs e)
		{
			AmountResult? result = AmountDialog.Show(this, "Desaturate", 0.5, true);
			if (result == null) return;

			UpdateImageWithUndo(Image.Desaturate(1.0 - result.Value, result.UseWeighting), "Desaturate");
		}

		private void Color_Paletted_Click(object sender, EventArgs e)
		{
			PaletteResult? paletteResult = PaletteForm.Show(this);
			if (paletteResult == null) return;

			Color32[] palette;
			if (paletteResult.StandardPalette != null)
				palette = paletteResult.StandardPalette;
			else
				palette = Image.Quantize(paletteResult.NumColors,
					!paletteResult.AdaptivePalette, paletteResult.IncludeAlpha);

			if (paletteResult.DitherMode.HasValue)
			{
				Image8 image8 = Image.ToImage8(palette, paletteResult.DitherMode.Value,
					paletteResult.UseVisualWeighting);

				UpdateImageWithUndo(image8.ToImage32(), "Convert to 8-bit paletted image");
			}
			else
			{
				Image32 image = new Image32(16 * 16, 16 * ((palette.Length + 15) >> 4));
				for (int i = 0; i < palette.Length; i++)
				{
					int x = (i & 0xF) * 16;
					int y = ((i >> 4) & 0xF) * 16;
					image.FillRect(x, y, 15, 15, palette[i]);
				}
				UpdateImageWithUndo(image, "Convert to palette");
			}
		}

		private void Effects_Gamma_Click(object sender, EventArgs e)
		{
			AmountResult? result = AmountDialog.Show(this, "Gamma", 1.0,
				allowWeighting: false, exponentialMode: true, max: 4.0);
			if (result == null) return;

			UpdateImageWithUndo(Image.Gamma(1.0 / result.Value), "Gamma");
		}

		private void Effects_BoxBlur_Click(object sender, EventArgs e)
		{
			AmountResult? result = AmountDialog.Show(this, "Box Blur", 1.0);
			if (result == null) return;

			UpdateImageWithUndo(Image.BoxBlur(result.Value), "Box Blur");
		}

		private void Effects_ApproximateGaussianBlur_Click(object sender, EventArgs e)
		{
			AmountResult? result = AmountDialog.Show(this, "Approximate Gaussian Blur", 1.0);
			if (result == null) return;

			UpdateImageWithUndo(Image.RoundBlur(result.Value), "Approximate Gaussian Blur");
		}

		private void Effects_Sharpen_Click(object sender, EventArgs e)
		{
			AmountResult? result = AmountDialog.Show(this, "Sharpen", 1.0);
			if (result == null) return;

			UpdateImageWithUndo(Image.Sharpen(result.Value), "Sharpen");
		}

		private void Effects_Emboss_Click(object sender, EventArgs e)
		{
			AmountResult? result = AmountDialog.Show(this, "Emboss", 1.0);
			if (result == null) return;

			UpdateImageWithUndo(Image.Emboss(result.Value), "Emboss");
		}

		private void Effects_EdgeDetect_Click(object sender, EventArgs e)
		{
			AmountResult? result = AmountDialog.Show(this, "Edge Detect", 1.0);
			if (result == null) return;

			UpdateImageWithUndo(Image.EdgeDetect(result.Value), "Edge Detect");
		}

		private void Color_SwapRG_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.SwapChannels(ColorChannel.Green, ColorChannel.Red, ColorChannel.Blue), "Swap Red <-> Green");
		}

		private void Color_SwapRB_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.SwapChannels(ColorChannel.Blue, ColorChannel.Green, ColorChannel.Red), "Swap Red <-> Blue");
		}

		private void Color_SwapGB_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.SwapChannels(ColorChannel.Red, ColorChannel.Blue, ColorChannel.Green), "Swap Green <-> Blue");
		}

		private void Color_SplitChannelR_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.ExtractChannel(ColorChannel.Red).ToImage32(), "Extract Red Channel");
		}

		private void Color_SplitChannelG_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.ExtractChannel(ColorChannel.Green).ToImage32(), "Extract Green Channel");
		}

		private void Color_SplitChannelB_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.ExtractChannel(ColorChannel.Blue).ToImage32(), "Extract Blue Channel");
		}

		private void Color_SplitChannelA_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(Image.ExtractChannel(ColorChannel.Alpha).ToImage32(), "Extract Alpha Channel");
		}

		private void Edit_Copy_Click(object sender, EventArgs e)
		{
			Image.Copy();
		}

		private void Edit_Paste_Click(object sender, EventArgs e)
		{
			UpdateImageWithUndo(image => image.Paste(), "Paste image");
		}

		private void Edit_Menu_Opening(object sender, EventArgs e)
		{
			EnableDisableUndoRedo();
		}

		private void Effects_HueSaturationBrightness_Click(object sender, EventArgs e)
		{
			HueSatBrt hueSatBrt = new HueSatBrt();

			DialogResult result = hueSatBrt.ShowDialog(this);
			if (result != DialogResult.OK)
				return;

			UpdateImageWithUndo(image => image.HueSaturationBrightness(hueSatBrt.Hue, hueSatBrt.Sat, hueSatBrt.Brt),
				"Hue/Saturation/Brightness");
		}

		private void Effects_BrightnessContrast_Click(object sender, EventArgs e)
		{
			BrightnessContrast brightnessContrast = new BrightnessContrast();

			DialogResult result = brightnessContrast.ShowDialog(this);
			if (result != DialogResult.OK)
				return;

			UpdateImageWithUndo(Image.BrightnessContrast(brightnessContrast.Brt, brightnessContrast.Cont),
				"Brightness/Contrast");
		}

		private void Effects_GaussianBlur_Click(object sender, EventArgs e)
		{
			AmountResult? result = AmountDialog.Show(this, "Gaussian Blur", 1.0,
				exponentialMode: true, max: 50);
			if (result == null) return;

			UpdateImageWithUndo(Image.GaussianBlur(result.Value), "Gaussian Blur");
		}

		private void Effects_ColorTemperature_Click(object sender, EventArgs e)
		{
			AmountResult? result = AmountDialog.Show(this, "Color Temperature", 6600, max: 13200,
				textFormat: "0");
			if (result == null) return;

			UpdateImageWithUndo(Image.ColorTemperature(result.Value), "Color Temperature");
		}
	}
}