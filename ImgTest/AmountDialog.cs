
namespace ImgTest
{
	public partial class AmountDialog : Form
	{
		private double _value;
		private bool _useWeighting;
		private bool _exponentialMode;

		private AmountDialog(string title, double value, bool allowWeighting, bool exponentialMode)
		{
			InitializeComponent();

			StartPosition = FormStartPosition.CenterParent;

			Text = title;

			_exponentialMode = exponentialMode;

			_value = value;
			AmountTextBox.Text = value.ToString("0.000");

			UseWeightingCheckBox.Visible = allowWeighting;
		}

		public static AmountResult? Show(IWin32Window ownerWindow, string title,
			double initialValue = 0.0, bool allowWeighting = false, bool exponentialMode = false)
		{
			AmountDialog amountDialog = new AmountDialog(title, initialValue, allowWeighting, exponentialMode);
			DialogResult result = amountDialog.ShowDialog(ownerWindow);

			return result == DialogResult.OK
				? new AmountResult(amountDialog._value, amountDialog._useWeighting)
				: null;
		}

		private void AmountTrackBar_Scroll(object sender, EventArgs e)
		{
			_value = AmountTrackBar.Value / 1000.0;
			if (_exponentialMode)
				_value = Math.Pow(2.0, (_value - 0.5) * 4.0);
			AmountTextBox.Text = _value.ToString("0.000");
		}

		private void AmountTextBox_TextChanged(object sender, EventArgs e)
		{
			_value = double.TryParse(AmountTextBox.Text, out double v) ? v : 0;
			if (_exponentialMode)
			{
				double v2 = (Math.Log2(_value) / 4.0) + 0.5;
				AmountTrackBar.Value = Math.Max(Math.Min((int)(v2 * 1000), AmountTrackBar.Maximum), AmountTrackBar.Minimum);
			}
			else
			{
				_value = Math.Min(Math.Max(_value, 0.0), 1.0);
				AmountTrackBar.Value = Math.Max(Math.Min((int)(_value * 1000), AmountTrackBar.Maximum), AmountTrackBar.Minimum);
			}
		}

		private void UseWeightingCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			_useWeighting = UseWeightingCheckBox.Checked;
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
