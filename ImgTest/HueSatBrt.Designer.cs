namespace ImgTest
{
	partial class HueSatBrt
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
			HueTextBox = new TextBox();
			HueTrackBar = new TrackBar();
			HueLabel = new Label();
			OKButton = new Button();
			CancelButton = new Button();
			SatTextBox = new TextBox();
			SatTrackBar = new TrackBar();
			SatLabel = new Label();
			BrtTextBox = new TextBox();
			BrtTrackBar = new TrackBar();
			BrtLabel = new Label();
			((System.ComponentModel.ISupportInitialize)HueTrackBar).BeginInit();
			((System.ComponentModel.ISupportInitialize)SatTrackBar).BeginInit();
			((System.ComponentModel.ISupportInitialize)BrtTrackBar).BeginInit();
			SuspendLayout();
			// 
			// HueTextBox
			// 
			HueTextBox.Location = new Point(326, 12);
			HueTextBox.Name = "HueTextBox";
			HueTextBox.Size = new Size(61, 23);
			HueTextBox.TabIndex = 2;
			HueTextBox.Text = "0.0";
			HueTextBox.TextAlign = HorizontalAlignment.Right;
			HueTextBox.TextChanged += HueTextBox_TextChanged;
			// 
			// HueTrackBar
			// 
			HueTrackBar.LargeChange = 100;
			HueTrackBar.Location = new Point(69, 12);
			HueTrackBar.Maximum = 2000;
			HueTrackBar.Name = "HueTrackBar";
			HueTrackBar.Size = new Size(251, 45);
			HueTrackBar.SmallChange = 5;
			HueTrackBar.TabIndex = 1;
			HueTrackBar.TickFrequency = 100;
			HueTrackBar.Scroll += HueTrackBar_Scroll;
			// 
			// HueLabel
			// 
			HueLabel.AutoSize = true;
			HueLabel.Location = new Point(9, 17);
			HueLabel.Name = "HueLabel";
			HueLabel.Size = new Size(32, 15);
			HueLabel.TabIndex = 0;
			HueLabel.Text = "Hue:";
			// 
			// OKButton
			// 
			OKButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			OKButton.Location = new Point(231, 165);
			OKButton.Name = "OKButton";
			OKButton.Size = new Size(75, 23);
			OKButton.TabIndex = 9;
			OKButton.Text = "OK";
			OKButton.UseVisualStyleBackColor = true;
			OKButton.Click += OKButton_Click;
			// 
			// CancelButton
			// 
			CancelButton.Location = new Point(312, 165);
			CancelButton.Name = "CancelButton";
			CancelButton.Size = new Size(75, 23);
			CancelButton.TabIndex = 10;
			CancelButton.Text = "Cancel";
			CancelButton.UseVisualStyleBackColor = true;
			CancelButton.Click += CancelButton_Click;
			// 
			// SatTextBox
			// 
			SatTextBox.Location = new Point(326, 63);
			SatTextBox.Name = "SatTextBox";
			SatTextBox.Size = new Size(61, 23);
			SatTextBox.TabIndex = 5;
			SatTextBox.Text = "0.0";
			SatTextBox.TextAlign = HorizontalAlignment.Right;
			SatTextBox.TextChanged += SatTextBox_TextChanged;
			// 
			// SatTrackBar
			// 
			SatTrackBar.LargeChange = 100;
			SatTrackBar.Location = new Point(69, 63);
			SatTrackBar.Maximum = 2000;
			SatTrackBar.Name = "SatTrackBar";
			SatTrackBar.Size = new Size(251, 45);
			SatTrackBar.SmallChange = 5;
			SatTrackBar.TabIndex = 4;
			SatTrackBar.TickFrequency = 100;
			SatTrackBar.Scroll += SatTrackBar_Scroll;
			// 
			// SatLabel
			// 
			SatLabel.AutoSize = true;
			SatLabel.Location = new Point(9, 68);
			SatLabel.Name = "SatLabel";
			SatLabel.Size = new Size(64, 15);
			SatLabel.TabIndex = 3;
			SatLabel.Text = "Saturation:";
			// 
			// BrtTextBox
			// 
			BrtTextBox.Location = new Point(326, 114);
			BrtTextBox.Name = "BrtTextBox";
			BrtTextBox.Size = new Size(61, 23);
			BrtTextBox.TabIndex = 8;
			BrtTextBox.Text = "0.0";
			BrtTextBox.TextAlign = HorizontalAlignment.Right;
			BrtTextBox.TextChanged += BrtTextBox_TextChanged;
			// 
			// BrtTrackBar
			// 
			BrtTrackBar.LargeChange = 100;
			BrtTrackBar.Location = new Point(69, 114);
			BrtTrackBar.Maximum = 2000;
			BrtTrackBar.Name = "BrtTrackBar";
			BrtTrackBar.Size = new Size(251, 45);
			BrtTrackBar.SmallChange = 5;
			BrtTrackBar.TabIndex = 7;
			BrtTrackBar.TickFrequency = 100;
			BrtTrackBar.Scroll += BrtTrackBar_Scroll;
			// 
			// BrtLabel
			// 
			BrtLabel.AutoSize = true;
			BrtLabel.Location = new Point(9, 119);
			BrtLabel.Name = "BrtLabel";
			BrtLabel.Size = new Size(60, 15);
			BrtLabel.TabIndex = 6;
			BrtLabel.Text = "Brightness:";
			// 
			// HueSatLit
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(405, 205);
			Controls.Add(BrtTextBox);
			Controls.Add(BrtTrackBar);
			Controls.Add(BrtLabel);
			Controls.Add(SatTextBox);
			Controls.Add(SatTrackBar);
			Controls.Add(SatLabel);
			Controls.Add(HueTextBox);
			Controls.Add(HueTrackBar);
			Controls.Add(HueLabel);
			Controls.Add(OKButton);
			Controls.Add(CancelButton);
			Name = "HueSatBrt";
			Text = "Adjust Hue/Saturation/Brightness";
			((System.ComponentModel.ISupportInitialize)HueTrackBar).EndInit();
			((System.ComponentModel.ISupportInitialize)SatTrackBar).EndInit();
			((System.ComponentModel.ISupportInitialize)BrtTrackBar).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private TextBox HueTextBox;
		private TrackBar HueTrackBar;
		private Label HueLabel;
		private Button OKButton;
		private Button CancelButton;
		private TextBox SatTextBox;
		private TrackBar SatTrackBar;
		private Label SatLabel;
		private TextBox BrtTextBox;
		private TrackBar BrtTrackBar;
		private Label BrtLabel;
	}
}