namespace ImgTest
{
	public partial class BrightnessContrast : Form
	{
		public double Cont { get; private set; }
		public double Brt { get; private set; }

		public BrightnessContrast()
		{
			InitializeComponent();

			ContTrackBar.Value = 1000;
			BrtTrackBar.Value = 1000;
		}

		private void ContTrackBar_Scroll(object sender, EventArgs e)
		{
			Cont = ((double)ContTrackBar.Value - 1000) / 1000;
			ContTextBox.Text = $"{(Cont * 100):+0.0;-0.0;0.0}";
		}

		private void ContTextBox_TextChanged(object sender, EventArgs e)
		{
			if (double.TryParse(ContTextBox.Text, out double value))
			{
				Cont = Math.Max(Math.Min(value / 100, 1), -1);
				ContTrackBar.Value = (int)(Cont * 1000 + 0.5) + 1000;
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
