namespace ImgTest
{
	partial class AmountDialog
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			CancelButton = new Button();
			OKButton = new Button();
			UseWeightingCheckBox = new CheckBox();
			AmountLabel = new Label();
			AmountTrackBar = new TrackBar();
			AmountTextBox = new TextBox();
			((System.ComponentModel.ISupportInitialize)AmountTrackBar).BeginInit();
			SuspendLayout();
			// 
			// CancelButton
			// 
			CancelButton.Location = new Point(315, 114);
			CancelButton.Name = "CancelButton";
			CancelButton.Size = new Size(75, 23);
			CancelButton.TabIndex = 0;
			CancelButton.Text = "Cancel";
			CancelButton.UseVisualStyleBackColor = true;
			CancelButton.Click += CancelButton_Click;
			// 
			// OKButton
			// 
			OKButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
			OKButton.Location = new Point(234, 114);
			OKButton.Name = "OKButton";
			OKButton.Size = new Size(75, 23);
			OKButton.TabIndex = 1;
			OKButton.Text = "OK";
			OKButton.UseVisualStyleBackColor = true;
			OKButton.Click += OKButton_Click;
			// 
			// UseWeightingCheckBox
			// 
			UseWeightingCheckBox.AutoSize = true;
			UseWeightingCheckBox.Location = new Point(136, 74);
			UseWeightingCheckBox.Name = "UseWeightingCheckBox";
			UseWeightingCheckBox.Size = new Size(137, 19);
			UseWeightingCheckBox.TabIndex = 2;
			UseWeightingCheckBox.Text = "Use Visual Weighting";
			UseWeightingCheckBox.UseVisualStyleBackColor = true;
			UseWeightingCheckBox.CheckedChanged += UseWeightingCheckBox_CheckedChanged;
			// 
			// AmountLabel
			// 
			AmountLabel.AutoSize = true;
			AmountLabel.Location = new Point(12, 30);
			AmountLabel.Name = "AmountLabel";
			AmountLabel.Size = new Size(54, 15);
			AmountLabel.TabIndex = 3;
			AmountLabel.Text = "Amount:";
			// 
			// AmountTrackBar
			// 
			AmountTrackBar.LargeChange = 100;
			AmountTrackBar.Location = new Point(72, 25);
			AmountTrackBar.Maximum = 1000;
			AmountTrackBar.Name = "AmountTrackBar";
			AmountTrackBar.Size = new Size(251, 45);
			AmountTrackBar.SmallChange = 5;
			AmountTrackBar.TabIndex = 4;
			AmountTrackBar.TickFrequency = 100;
			AmountTrackBar.Scroll += AmountTrackBar_Scroll;
			// 
			// AmountTextBox
			// 
			AmountTextBox.Location = new Point(329, 25);
			AmountTextBox.Name = "AmountTextBox";
			AmountTextBox.Size = new Size(61, 23);
			AmountTextBox.TabIndex = 5;
			AmountTextBox.Text = "0.0";
			AmountTextBox.TextAlign = HorizontalAlignment.Right;
			AmountTextBox.TextChanged += AmountTextBox_TextChanged;
			// 
			// AmountDialog
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(406, 149);
			Controls.Add(AmountTextBox);
			Controls.Add(AmountTrackBar);
			Controls.Add(AmountLabel);
			Controls.Add(UseWeightingCheckBox);
			Controls.Add(OKButton);
			Controls.Add(CancelButton);
			Name = "AmountDialog";
			Text = "AmountDialog";
			((System.ComponentModel.ISupportInitialize)AmountTrackBar).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Button CancelButton;
		private Button OKButton;
		private CheckBox UseWeightingCheckBox;
		private Label AmountLabel;
		private TrackBar AmountTrackBar;
		private TextBox AmountTextBox;
	}
}