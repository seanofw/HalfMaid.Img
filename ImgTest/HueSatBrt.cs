namespace ImgTest
{
	public partial class HueSatBrt : Form
	{
		public double Hue { get; private set; }
		public double Sat { get; private set; }
		public double Brt { get; private set; }

		public HueSatBrt()
		{
			InitializeComponent();

			HueTrackBar.Value = 1000;
			SatTrackBar.Value = 1000;
			BrtTrackBar.Value = 1000;
		}

		private void HueTrackBar_Scroll(object sender, EventArgs e)
		{
			Hue = ((double)HueTrackBar.Value - 1000) / 1000 * 180;
			HueTextBox.Text = $"{Hue:+0.0;-0.0;0.0}";
		}

		private void HueTextBox_TextChanged(object sender, EventArgs e)
		{
			if (double.TryParse(HueTextBox.Text, out double value))
			{
				Hue = value % 360.0;
				if (Hue < -180) Hue += 360;
				if (Hue > +180) Hue -= 360;
				HueTrackBar.Value = (int)(Hue / 180.0 * 1000 + 0.5) + 1000;
			}
		}

		private void SatTrackBar_Scroll(object sender, EventArgs e)
		{
			Sat = ((double)SatTrackBar.Value - 1000) / 1000;
			SatTextBox.Text = $"{(Sat * 100):+0.0;-0.0;0.0}";
		}

		private void SatTextBox_TextChanged(object sender, EventArgs e)
		{
			if (double.TryParse(SatTextBox.Text, out double value))
			{
				Sat = Math.Max(Math.Min(value / 100, 1), -1);
				SatTrackBar.Value = (int)(Sat * 1000 + 0.5) + 1000;
			}
		}

		private void BrtTrackBar_Scroll(object sender, EventArgs e)
		{
			Brt = ((double)BrtTrackBar.Value - 1000) / 1000;
			BrtTextBox.Text = $"{(Brt * 100):+0.0;-0.0;0.0}";
		}

		private void BrtTextBox_TextChanged(object sender, EventArgs e)
		{
			if (double.TryParse(BrtTextBox.Text, out double value))
			{
				Brt = Math.Max(Math.Min(value / 100, 1), -1);
				BrtTrackBar.Value = (int)(Brt * 1000 + 0.5) + 1000;
			}
		}

		private void OKButton_Click(object sender, EventArgs e)
		{
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
