using HalfMaid.Img;

namespace ImgTest
{
	public partial class PaletteForm : Form
	{
		private Color32[]? _standardPalette;
		private int _numColors = 256;
		private bool _includeAlpha = false;
		private bool _adaptivePalette = true;
		private bool _useVisualWeighting = false;
		private DitherMode? _ditherMode = DitherMode.FloydSteinberg;

		private PaletteForm()
		{
			InitializeComponent();
		}

		public static new PaletteResult? Show(IWin32Window ownerWindow)
		{
			PaletteForm paletteForm = new PaletteForm();
			DialogResult result = paletteForm.ShowDialog(ownerWindow);
			if (result != DialogResult.OK)
				return null;

			return new PaletteResult(paletteForm._standardPalette,
				paletteForm._numColors, paletteForm._adaptivePalette, paletteForm._includeAlpha,
				paletteForm._useVisualWeighting, paletteForm._ditherMode);
		}

		private void OkButton_Click(object sender, EventArgs e)
		{
			_standardPalette =
				  WebSafePaletteRadioButton.Checked ? Palettes.Web216.ToArray()
				: Grayscale256PaletteRadioButton.Checked ? Palettes.Grayscale256.ToArray()
				: Grayscale64PaletteRadioButton.Checked ? Palettes.Grayscale64A.ToArray()
				: Grayscale16PaletteRadioButton.Checked ? Palettes.Grayscale16A.ToArray()
				: BWPaletteRadioButton.Checked ? Palettes.BlackAndWhite.ToArray()
				: CgaPaletteRadioButton.Checked ? Palettes.Cga16.ToArray()
				: CgaAltPaletteRadioButton.Checked ? Palettes.Cga16Alt.ToArray()
				: Ega64PaletteRadioButton.Checked ? Palettes.Ega64.ToArray()
				: Commodore64_16PaletteRadioButton.Checked ? Palettes.Commodore64_16.ToArray()
				: Nes54PaletteRadioButton.Checked ? Palettes.NES54.ToArray()
				: Nes64PaletteRadioButton.Checked ? Palettes.NES64.ToArray()
				: null;

			_adaptivePalette = MedianCutAdaptiveRadioButton.Checked;

			_numColors = int.TryParse(ColorCountTextBox.Text, out int v) ? v : 0;

			_includeAlpha = IncludeAlphaCheckBox.Checked;

			_useVisualWeighting = UseVisualWeightingCheckBox.Checked;

			_ditherMode =
				  Ordered2x2RadioButton.Checked ? DitherMode.Ordered2x2
				: Ordered4x4RadioButton.Checked ? DitherMode.Ordered4x4
				: Ordered8x8RadioButton.Checked ? DitherMode.Ordered8x8
				: FloydSteinbergDitherRadioButton.Checked ? DitherMode.FloydSteinberg
				: AtkinsonDitherRadioButton.Checked ? DitherMode.Atkinson
				: StuckiDitherRadioButton.Checked ? DitherMode.Stucki
				: BurkesDitherRadioButton.Checked ? DitherMode.Burkes
				: JarvisDitherRadioButton.Checked ? DitherMode.Jarvis
				: OutputPaletteRadioButton.Checked ? null
				: DitherMode.Nearest;

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
