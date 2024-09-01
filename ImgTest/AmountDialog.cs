
namespace ImgTest
{
	public partial class AmountDialog : Form
	{
		private double _value;
		private bool _useWeighting;
		private bool _exponentialMode;
		private double _min;
		private double _max;
		private string _textFormat;

		private AmountDialog(string title, double value, bool allowWeighting,
			bool exponentialMode, double min, double max, string textFormat)
		{
			InitializeComponent();

			StartPosition = FormStartPosition.CenterParent;

			Text = title;

			_exponentialMode = exponentialMode;

			_value = value;
			_min = min;
			_max = max;
			_textFormat = textFormat;

			AmountTextBox.Text = value.ToString(textFormat);

			UseWeightingCheckBox.Visible = allowWeighting;
		}

		public static AmountResult? Show(IWin32Window ownerWindow, string title,
			double initialValue = 0.0, bool allowWeighting = false,
			bool exponentialMode = false, double min = 0.0, double max = 1.0,
			string textFormat = "0.000")
		{
			AmountDialog amountDialog = new AmountDialog(title, initialValue, allowWeighting,
				exponentialMode, min, max, textFormat);
			DialogResult result = amountDialog.ShowDialog(ownerWindow);

			return result == DialogResult.OK
				? new AmountResult(amountDialog._value, amountDialog._useWeighting)
				: null;
		}

		private void AmountTrackBar_Scroll(object sender, EventArgs e)
		{
			_value = AmountTrackBar.Value / 1000.0;
			if (_exponentialMode)
				_value = Math.Pow(_max, (_value - 0.5) * 2.0);
			else
				_value = ((_max - _min) * _value) + _min;
			AmountTextBox.Text = _value.ToString(_textFormat);
		}

		private void AmountTextBox_TextChanged(object sender, EventArgs e)
		{
			_value = double.TryParse(AmountTextBox.Text, out double v) ? v : 0;
			if (_exponentialMode)
			{
				double v2 = ((Math.Log(_value) / Math.Log(_max)) * 0.5) + 0.5;
				AmountTrackBar.Value = Math.Max(Math.Min((int)(v2 * 1000), AmountTrackBar.Maximum), AmountTrackBar.Minimum);
			}
			else
			{
				_value = Math.Min(Math.Max(_value, _min), _max);
				AmountTrackBar.Value = Math.Max(Math.Min((int)((_value - _min) / (_max - _min) * 1000), AmountTrackBar.Maximum), AmountTrackBar.Minimum);
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
