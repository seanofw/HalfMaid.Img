namespace ImgTest
{
	partial class BrightnessContrast
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
			BrtTextBox = new TextBox();
			BrtTrackBar = new TrackBar();
			BrtLabel = new Label();
			ContTextBox = new TextBox();
			ContTrackBar = new TrackBar();
			ContLabel = new Label();
			OKButton = new Button();
			CancelButton = new Button();
			((System.ComponentModel.ISupportInitialize)BrtTrackBar).BeginInit();
			((System.ComponentModel.ISupportInitialize)ContTrackBar).BeginInit();
			SuspendLayout();
			// 
			// BrtTextBox
			// 
			BrtTextBox.Location = new Point(326, 12);
			BrtTextBox.Name = "BrtTextBox";
			BrtTextBox.Size = new Size(61, 23);
			BrtTextBox.TabIndex = 2;
			BrtTextBox.Text = "0.0";
			BrtTextBox.TextAlign = HorizontalAlignment.Right;
			BrtTextBox.TextChanged += BrtTextBox_TextChanged;
			// 
			// BrtTrackBar
			// 
			BrtTrackBar.LargeChange = 100;
			BrtTrackBar.Location = new Point(69, 12);
			BrtTrackBar.Maximum = 2000;
			BrtTrackBar.Name = "BrtTrackBar";
			BrtTrackBar.Size = new Size(251, 45);
			BrtTrackBar.SmallChange = 5;
			BrtTrackBar.TabIndex = 1;
			BrtTrackBar.TickFrequency = 100;
			BrtTrackBar.Scroll += BrtTrackBar_Scroll;
			// 
			// BrtLabel
			// 
			BrtLabel.AutoSize = true;
			BrtLabel.Location = new Point(9, 17);
			BrtLabel.Name = "BrtLabel";
			BrtLabel.Size = new Size(65, 15);
			BrtLabel.TabIndex = 0;
			BrtLabel.Text = "Brightness:";
			// 
			// ContTextBox
			// 
			ContTextBox.Location = new Point(326, 52);
			ContTextBox.Name = "ContTextBox";
			ContTextBox.Size = new Size(61, 23);
			ContTextBox.TabIndex = 5;
			ContTextBox.Text = "0.0";
			ContTextBox.TextAlign = HorizontalAlignment.Right;
			ContTextBox.TextChanged += ContTextBox_TextChanged;
			// 
			// ContTrackBar
			// 
			ContTrackBar.LargeChange = 100;
			ContTrackBar.Location = new Point(69, 52);
			ContTrackBar.Maximum = 2000;
			ContTrackBar.Name = "ContTrackBar";
			ContTrackBar.Size = new Size(251, 45);
			ContTrackBar.SmallChange = 5;
			ContTrackBar.TabIndex = 4;
			ContTrackBar.TickFrequency = 100;
			ContTrackBar.Scroll += ContTrackBar_Scroll;
			// 
			// ContLabel
			// 
			ContLabel.AutoSize = true;
			ContLabel.Location = new Point(9, 57);
			ContLabel.Name = "ContLabel";
			ContLabel.Size = new Size(55, 15);
			ContLabel.TabIndex = 3;
			ContLabel.Text = "Contrast:";
			// 
			// OKButton
			// 
			OKButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
			OKButton.Location = new Point(231, 103);
			OKButton.Name = "OKButton";
			OKButton.Size = new Size(75, 23);
			OKButton.TabIndex = 6;
			OKButton.Text = "OK";
			OKButton.UseVisualStyleBackColor = true;
			OKButton.Click += OKButton_Click;
			// 
			// CancelButton
			// 
			CancelButton.Location = new Point(312, 103);
			CancelButton.Name = "CancelButton";
			CancelButton.Size = new Size(75, 23);
			CancelButton.TabIndex = 7;
			CancelButton.Text = "Cancel";
			CancelButton.UseVisualStyleBackColor = true;
			CancelButton.Click += CancelButton_Click;
			// 
			// BrightnessContrast
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(405, 140);
			Controls.Add(BrtTextBox);
			Controls.Add(BrtTrackBar);
			Controls.Add(BrtLabel);
			Controls.Add(ContTextBox);
			Controls.Add(ContTrackBar);
			Controls.Add(ContLabel);
			Controls.Add(OKButton);
			Controls.Add(CancelButton);
			Name = "BrightnessContrast";
			Text = "Adjust Brightness/Contrast";
			((System.ComponentModel.ISupportInitialize)BrtTrackBar).EndInit();
			((System.ComponentModel.ISupportInitialize)ContTrackBar).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Button OKButton;
		private Button CancelButton;
		private TextBox BrtTextBox;
		private TrackBar BrtTrackBar;
		private Label BrtLabel;
		private TextBox ContTextBox;
		private TrackBar ContTrackBar;
		private Label ContLabel;
	}
}