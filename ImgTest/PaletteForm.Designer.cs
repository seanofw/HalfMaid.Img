namespace ImgTest
{
	partial class PaletteForm
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
			label1 = new Label();
			WebSafePaletteRadioButton = new RadioButton();
			label2 = new Label();
			CgaPaletteRadioButton = new RadioButton();
			Grayscale256PaletteRadioButton = new RadioButton();
			Grayscale64PaletteRadioButton = new RadioButton();
			Grayscale16PaletteRadioButton = new RadioButton();
			CgaAltPaletteRadioButton = new RadioButton();
			label3 = new Label();
			Ega64PaletteRadioButton = new RadioButton();
			Commodore64_16PaletteRadioButton = new RadioButton();
			Nes54PaletteRadioButton = new RadioButton();
			Nes64PaletteRadioButton = new RadioButton();
			MedianCutExistingRadioButton = new RadioButton();
			MedianCutAdaptiveRadioButton = new RadioButton();
			ColorCountTextBox = new TextBox();
			label4 = new Label();
			groupBox1 = new GroupBox();
			IncludeAlphaCheckBox = new CheckBox();
			BWPaletteRadioButton = new RadioButton();
			groupBox2 = new GroupBox();
			OutputPaletteRadioButton = new RadioButton();
			UseVisualWeightingCheckBox = new CheckBox();
			JarvisDitherRadioButton = new RadioButton();
			BurkesDitherRadioButton = new RadioButton();
			FloydSteinbergDitherRadioButton = new RadioButton();
			AtkinsonDitherRadioButton = new RadioButton();
			label7 = new Label();
			label6 = new Label();
			label5 = new Label();
			StuckiDitherRadioButton = new RadioButton();
			Ordered8x8RadioButton = new RadioButton();
			Ordered4x4RadioButton = new RadioButton();
			Ordered2x2RadioButton = new RadioButton();
			NoDitheringRadioButton = new RadioButton();
			OKButton = new Button();
			CancelButton = new Button();
			groupBox1.SuspendLayout();
			groupBox2.SuspendLayout();
			SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			label1.Location = new Point(16, 22);
			label1.Name = "label1";
			label1.Size = new Size(111, 15);
			label1.TabIndex = 0;
			label1.Text = "Optimized palette:";
			// 
			// WebSafePaletteRadioButton
			// 
			WebSafePaletteRadioButton.AutoSize = true;
			WebSafePaletteRadioButton.Location = new Point(208, 46);
			WebSafePaletteRadioButton.Name = "WebSafePaletteRadioButton";
			WebSafePaletteRadioButton.Size = new Size(167, 19);
			WebSafePaletteRadioButton.TabIndex = 7;
			WebSafePaletteRadioButton.Text = "Web-safe 216-color palette";
			WebSafePaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			label2.Location = new Point(208, 22);
			label2.Name = "label2";
			label2.Size = new Size(108, 15);
			label2.TabIndex = 6;
			label2.Text = "Standard palettes:";
			// 
			// CgaPaletteRadioButton
			// 
			CgaPaletteRadioButton.AutoSize = true;
			CgaPaletteRadioButton.Location = new Point(396, 46);
			CgaPaletteRadioButton.Name = "CgaPaletteRadioButton";
			CgaPaletteRadioButton.Size = new Size(180, 19);
			CgaPaletteRadioButton.TabIndex = 13;
			CgaPaletteRadioButton.Text = "CGA 16-color palette (brown)";
			CgaPaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// Grayscale256PaletteRadioButton
			// 
			Grayscale256PaletteRadioButton.AutoSize = true;
			Grayscale256PaletteRadioButton.Location = new Point(208, 71);
			Grayscale256PaletteRadioButton.Name = "Grayscale256PaletteRadioButton";
			Grayscale256PaletteRadioButton.Size = new Size(166, 19);
			Grayscale256PaletteRadioButton.TabIndex = 8;
			Grayscale256PaletteRadioButton.Text = "256-color grayscale palette";
			Grayscale256PaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// Grayscale64PaletteRadioButton
			// 
			Grayscale64PaletteRadioButton.AutoSize = true;
			Grayscale64PaletteRadioButton.Location = new Point(208, 96);
			Grayscale64PaletteRadioButton.Name = "Grayscale64PaletteRadioButton";
			Grayscale64PaletteRadioButton.Size = new Size(160, 19);
			Grayscale64PaletteRadioButton.TabIndex = 9;
			Grayscale64PaletteRadioButton.Text = "64-color grayscale palette";
			Grayscale64PaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// Grayscale16PaletteRadioButton
			// 
			Grayscale16PaletteRadioButton.AutoSize = true;
			Grayscale16PaletteRadioButton.Location = new Point(208, 121);
			Grayscale16PaletteRadioButton.Name = "Grayscale16PaletteRadioButton";
			Grayscale16PaletteRadioButton.Size = new Size(160, 19);
			Grayscale16PaletteRadioButton.TabIndex = 10;
			Grayscale16PaletteRadioButton.Text = "16-color grayscale palette";
			Grayscale16PaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// CgaAltPaletteRadioButton
			// 
			CgaAltPaletteRadioButton.AutoSize = true;
			CgaAltPaletteRadioButton.Location = new Point(396, 71);
			CgaAltPaletteRadioButton.Name = "CgaAltPaletteRadioButton";
			CgaAltPaletteRadioButton.Size = new Size(206, 19);
			CgaAltPaletteRadioButton.TabIndex = 14;
			CgaAltPaletteRadioButton.Text = "CGA 16-color palette (dark yellow)";
			CgaAltPaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			label3.Location = new Point(396, 22);
			label3.Name = "label3";
			label3.Size = new Size(110, 15);
			label3.TabIndex = 12;
			label3.Text = "Historical palettes:";
			// 
			// Ega64PaletteRadioButton
			// 
			Ega64PaletteRadioButton.AutoSize = true;
			Ega64PaletteRadioButton.Location = new Point(396, 96);
			Ega64PaletteRadioButton.Name = "Ega64PaletteRadioButton";
			Ega64PaletteRadioButton.Size = new Size(133, 19);
			Ega64PaletteRadioButton.TabIndex = 15;
			Ega64PaletteRadioButton.Text = "EGA 64-color palette";
			Ega64PaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// Commodore64_16PaletteRadioButton
			// 
			Commodore64_16PaletteRadioButton.AutoSize = true;
			Commodore64_16PaletteRadioButton.Location = new Point(396, 121);
			Commodore64_16PaletteRadioButton.Name = "Commodore64_16PaletteRadioButton";
			Commodore64_16PaletteRadioButton.Size = new Size(194, 19);
			Commodore64_16PaletteRadioButton.TabIndex = 16;
			Commodore64_16PaletteRadioButton.Text = "Commodore 64 16-color palette";
			Commodore64_16PaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// Nes54PaletteRadioButton
			// 
			Nes54PaletteRadioButton.AutoSize = true;
			Nes54PaletteRadioButton.Location = new Point(396, 146);
			Nes54PaletteRadioButton.Name = "Nes54PaletteRadioButton";
			Nes54PaletteRadioButton.Size = new Size(132, 19);
			Nes54PaletteRadioButton.TabIndex = 17;
			Nes54PaletteRadioButton.Text = "NES 54-color palette";
			Nes54PaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// Nes64PaletteRadioButton
			// 
			Nes64PaletteRadioButton.AutoSize = true;
			Nes64PaletteRadioButton.Location = new Point(396, 171);
			Nes64PaletteRadioButton.Name = "Nes64PaletteRadioButton";
			Nes64PaletteRadioButton.Size = new Size(132, 19);
			Nes64PaletteRadioButton.TabIndex = 18;
			Nes64PaletteRadioButton.Text = "NES 64-color palette";
			Nes64PaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// MedianCutExistingRadioButton
			// 
			MedianCutExistingRadioButton.AutoSize = true;
			MedianCutExistingRadioButton.Location = new Point(17, 46);
			MedianCutExistingRadioButton.Name = "MedianCutExistingRadioButton";
			MedianCutExistingRadioButton.Size = new Size(149, 19);
			MedianCutExistingRadioButton.TabIndex = 1;
			MedianCutExistingRadioButton.Text = "Use only existing colors";
			MedianCutExistingRadioButton.UseVisualStyleBackColor = true;
			// 
			// MedianCutAdaptiveRadioButton
			// 
			MedianCutAdaptiveRadioButton.AutoSize = true;
			MedianCutAdaptiveRadioButton.Checked = true;
			MedianCutAdaptiveRadioButton.Location = new Point(17, 71);
			MedianCutAdaptiveRadioButton.Name = "MedianCutAdaptiveRadioButton";
			MedianCutAdaptiveRadioButton.Size = new Size(119, 19);
			MedianCutAdaptiveRadioButton.TabIndex = 2;
			MedianCutAdaptiveRadioButton.TabStop = true;
			MedianCutAdaptiveRadioButton.Text = "Create new colors";
			MedianCutAdaptiveRadioButton.UseVisualStyleBackColor = true;
			// 
			// ColorCountTextBox
			// 
			ColorCountTextBox.Location = new Point(125, 103);
			ColorCountTextBox.Name = "ColorCountTextBox";
			ColorCountTextBox.Size = new Size(50, 23);
			ColorCountTextBox.TabIndex = 4;
			ColorCountTextBox.Text = "256";
			// 
			// label4
			// 
			label4.AutoSize = true;
			label4.Location = new Point(16, 108);
			label4.Name = "label4";
			label4.Size = new Size(103, 15);
			label4.TabIndex = 3;
			label4.Text = "Number of colors:";
			// 
			// groupBox1
			// 
			groupBox1.Controls.Add(IncludeAlphaCheckBox);
			groupBox1.Controls.Add(BWPaletteRadioButton);
			groupBox1.Controls.Add(label1);
			groupBox1.Controls.Add(label4);
			groupBox1.Controls.Add(WebSafePaletteRadioButton);
			groupBox1.Controls.Add(ColorCountTextBox);
			groupBox1.Controls.Add(label2);
			groupBox1.Controls.Add(MedianCutAdaptiveRadioButton);
			groupBox1.Controls.Add(CgaPaletteRadioButton);
			groupBox1.Controls.Add(MedianCutExistingRadioButton);
			groupBox1.Controls.Add(Grayscale256PaletteRadioButton);
			groupBox1.Controls.Add(Nes64PaletteRadioButton);
			groupBox1.Controls.Add(Grayscale64PaletteRadioButton);
			groupBox1.Controls.Add(Nes54PaletteRadioButton);
			groupBox1.Controls.Add(Grayscale16PaletteRadioButton);
			groupBox1.Controls.Add(Commodore64_16PaletteRadioButton);
			groupBox1.Controls.Add(CgaAltPaletteRadioButton);
			groupBox1.Controls.Add(Ega64PaletteRadioButton);
			groupBox1.Controls.Add(label3);
			groupBox1.Location = new Point(12, 12);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new Size(611, 205);
			groupBox1.TabIndex = 0;
			groupBox1.TabStop = false;
			groupBox1.Text = "Palette";
			// 
			// IncludeAlphaCheckBox
			// 
			IncludeAlphaCheckBox.AutoSize = true;
			IncludeAlphaCheckBox.Location = new Point(16, 146);
			IncludeAlphaCheckBox.Name = "IncludeAlphaCheckBox";
			IncludeAlphaCheckBox.Size = new Size(142, 19);
			IncludeAlphaCheckBox.TabIndex = 5;
			IncludeAlphaCheckBox.Text = "Include alpha channel";
			IncludeAlphaCheckBox.UseVisualStyleBackColor = true;
			// 
			// BWPaletteRadioButton
			// 
			BWPaletteRadioButton.AutoSize = true;
			BWPaletteRadioButton.Location = new Point(208, 146);
			BWPaletteRadioButton.Name = "BWPaletteRadioButton";
			BWPaletteRadioButton.Size = new Size(133, 19);
			BWPaletteRadioButton.TabIndex = 11;
			BWPaletteRadioButton.Text = "2-color B&&W palette";
			BWPaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			groupBox2.Controls.Add(OutputPaletteRadioButton);
			groupBox2.Controls.Add(UseVisualWeightingCheckBox);
			groupBox2.Controls.Add(JarvisDitherRadioButton);
			groupBox2.Controls.Add(BurkesDitherRadioButton);
			groupBox2.Controls.Add(FloydSteinbergDitherRadioButton);
			groupBox2.Controls.Add(AtkinsonDitherRadioButton);
			groupBox2.Controls.Add(label7);
			groupBox2.Controls.Add(label6);
			groupBox2.Controls.Add(label5);
			groupBox2.Controls.Add(StuckiDitherRadioButton);
			groupBox2.Controls.Add(Ordered8x8RadioButton);
			groupBox2.Controls.Add(Ordered4x4RadioButton);
			groupBox2.Controls.Add(Ordered2x2RadioButton);
			groupBox2.Controls.Add(NoDitheringRadioButton);
			groupBox2.Location = new Point(12, 223);
			groupBox2.Name = "groupBox2";
			groupBox2.Size = new Size(611, 185);
			groupBox2.TabIndex = 1;
			groupBox2.TabStop = false;
			groupBox2.Text = "Dithering";
			// 
			// OutputPaletteRadioButton
			// 
			OutputPaletteRadioButton.AutoSize = true;
			OutputPaletteRadioButton.Location = new Point(82, 72);
			OutputPaletteRadioButton.Name = "OutputPaletteRadioButton";
			OutputPaletteRadioButton.Size = new Size(97, 19);
			OutputPaletteRadioButton.TabIndex = 2;
			OutputPaletteRadioButton.Text = "Palette Image";
			OutputPaletteRadioButton.UseVisualStyleBackColor = true;
			// 
			// UseVisualWeightingCheckBox
			// 
			UseVisualWeightingCheckBox.AutoSize = true;
			UseVisualWeightingCheckBox.Location = new Point(82, 148);
			UseVisualWeightingCheckBox.Name = "UseVisualWeightingCheckBox";
			UseVisualWeightingCheckBox.Size = new Size(134, 19);
			UseVisualWeightingCheckBox.TabIndex = 13;
			UseVisualWeightingCheckBox.Text = "Use visual weighting";
			UseVisualWeightingCheckBox.UseVisualStyleBackColor = true;
			// 
			// JarvisDitherRadioButton
			// 
			JarvisDitherRadioButton.AutoSize = true;
			JarvisDitherRadioButton.Location = new Point(396, 147);
			JarvisDitherRadioButton.Name = "JarvisDitherRadioButton";
			JarvisDitherRadioButton.Size = new Size(87, 19);
			JarvisDitherRadioButton.TabIndex = 12;
			JarvisDitherRadioButton.Text = "Jarvis dither";
			JarvisDitherRadioButton.UseVisualStyleBackColor = true;
			// 
			// BurkesDitherRadioButton
			// 
			BurkesDitherRadioButton.AutoSize = true;
			BurkesDitherRadioButton.Location = new Point(396, 122);
			BurkesDitherRadioButton.Name = "BurkesDitherRadioButton";
			BurkesDitherRadioButton.Size = new Size(94, 19);
			BurkesDitherRadioButton.TabIndex = 11;
			BurkesDitherRadioButton.Text = "Burkes dither";
			BurkesDitherRadioButton.UseVisualStyleBackColor = true;
			// 
			// FloydSteinbergDitherRadioButton
			// 
			FloydSteinbergDitherRadioButton.AutoSize = true;
			FloydSteinbergDitherRadioButton.Checked = true;
			FloydSteinbergDitherRadioButton.Location = new Point(396, 47);
			FloydSteinbergDitherRadioButton.Name = "FloydSteinbergDitherRadioButton";
			FloydSteinbergDitherRadioButton.Size = new Size(143, 19);
			FloydSteinbergDitherRadioButton.TabIndex = 8;
			FloydSteinbergDitherRadioButton.TabStop = true;
			FloydSteinbergDitherRadioButton.Text = "Floyd-Steinberg dither";
			FloydSteinbergDitherRadioButton.UseVisualStyleBackColor = true;
			// 
			// AtkinsonDitherRadioButton
			// 
			AtkinsonDitherRadioButton.AutoSize = true;
			AtkinsonDitherRadioButton.Location = new Point(396, 72);
			AtkinsonDitherRadioButton.Name = "AtkinsonDitherRadioButton";
			AtkinsonDitherRadioButton.Size = new Size(106, 19);
			AtkinsonDitherRadioButton.TabIndex = 9;
			AtkinsonDitherRadioButton.Text = "Atkinson dither";
			AtkinsonDitherRadioButton.UseVisualStyleBackColor = true;
			// 
			// label7
			// 
			label7.AutoSize = true;
			label7.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			label7.Location = new Point(395, 24);
			label7.Name = "label7";
			label7.Size = new Size(142, 15);
			label7.TabIndex = 7;
			label7.Text = "Error-diffused dithering:";
			// 
			// label6
			// 
			label6.AutoSize = true;
			label6.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			label6.Location = new Point(208, 24);
			label6.Name = "label6";
			label6.Size = new Size(111, 15);
			label6.TabIndex = 3;
			label6.Text = "Ordered dithering:";
			// 
			// label5
			// 
			label5.AutoSize = true;
			label5.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			label5.Location = new Point(82, 24);
			label5.Name = "label5";
			label5.Size = new Size(80, 15);
			label5.TabIndex = 0;
			label5.Text = "No dithering:";
			// 
			// StuckiDitherRadioButton
			// 
			StuckiDitherRadioButton.AutoSize = true;
			StuckiDitherRadioButton.Location = new Point(396, 97);
			StuckiDitherRadioButton.Name = "StuckiDitherRadioButton";
			StuckiDitherRadioButton.Size = new Size(91, 19);
			StuckiDitherRadioButton.TabIndex = 10;
			StuckiDitherRadioButton.Text = "Stucki dither";
			StuckiDitherRadioButton.UseVisualStyleBackColor = true;
			// 
			// Ordered8x8RadioButton
			// 
			Ordered8x8RadioButton.AutoSize = true;
			Ordered8x8RadioButton.Location = new Point(208, 97);
			Ordered8x8RadioButton.Name = "Ordered8x8RadioButton";
			Ordered8x8RadioButton.Size = new Size(123, 19);
			Ordered8x8RadioButton.TabIndex = 6;
			Ordered8x8RadioButton.Text = "Ordered 8x8 dither";
			Ordered8x8RadioButton.UseVisualStyleBackColor = true;
			// 
			// Ordered4x4RadioButton
			// 
			Ordered4x4RadioButton.AutoSize = true;
			Ordered4x4RadioButton.Location = new Point(208, 72);
			Ordered4x4RadioButton.Name = "Ordered4x4RadioButton";
			Ordered4x4RadioButton.Size = new Size(123, 19);
			Ordered4x4RadioButton.TabIndex = 5;
			Ordered4x4RadioButton.Text = "Ordered 4x4 dither";
			Ordered4x4RadioButton.UseVisualStyleBackColor = true;
			// 
			// Ordered2x2RadioButton
			// 
			Ordered2x2RadioButton.AutoSize = true;
			Ordered2x2RadioButton.Location = new Point(208, 47);
			Ordered2x2RadioButton.Name = "Ordered2x2RadioButton";
			Ordered2x2RadioButton.Size = new Size(123, 19);
			Ordered2x2RadioButton.TabIndex = 4;
			Ordered2x2RadioButton.Text = "Ordered 2x2 dither";
			Ordered2x2RadioButton.UseVisualStyleBackColor = true;
			// 
			// NoDitheringRadioButton
			// 
			NoDitheringRadioButton.AutoSize = true;
			NoDitheringRadioButton.Location = new Point(82, 47);
			NoDitheringRadioButton.Name = "NoDitheringRadioButton";
			NoDitheringRadioButton.Size = new Size(95, 19);
			NoDitheringRadioButton.TabIndex = 1;
			NoDitheringRadioButton.Text = "Nearest color";
			NoDitheringRadioButton.UseVisualStyleBackColor = true;
			// 
			// OKButton
			// 
			OKButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			OKButton.Location = new Point(465, 414);
			OKButton.Name = "OKButton";
			OKButton.Size = new Size(75, 23);
			OKButton.TabIndex = 2;
			OKButton.Text = "OK";
			OKButton.UseVisualStyleBackColor = true;
			OKButton.Click += OkButton_Click;
			// 
			// CancelButton
			// 
			CancelButton.Location = new Point(548, 414);
			CancelButton.Name = "CancelButton";
			CancelButton.Size = new Size(75, 23);
			CancelButton.TabIndex = 3;
			CancelButton.Text = "Cancel";
			CancelButton.UseVisualStyleBackColor = true;
			CancelButton.Click += CancelButton_Click;
			// 
			// PaletteForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(635, 449);
			Controls.Add(CancelButton);
			Controls.Add(OKButton);
			Controls.Add(groupBox2);
			Controls.Add(groupBox1);
			Name = "PaletteForm";
			Text = "Convert to 8-bit paletted";
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			groupBox2.ResumeLayout(false);
			groupBox2.PerformLayout();
			ResumeLayout(false);
		}

		#endregion

		private Label label1;
		private RadioButton WebSafePaletteRadioButton;
		private Label label2;
		private RadioButton CgaPaletteRadioButton;
		private RadioButton Grayscale256PaletteRadioButton;
		private RadioButton Grayscale64PaletteRadioButton;
		private RadioButton Grayscale16PaletteRadioButton;
		private RadioButton CgaAltPaletteRadioButton;
		private Label label3;
		private RadioButton Ega64PaletteRadioButton;
		private RadioButton Commodore64_16PaletteRadioButton;
		private RadioButton Nes54PaletteRadioButton;
		private RadioButton Nes64PaletteRadioButton;
		private RadioButton MedianCutExistingRadioButton;
		private RadioButton MedianCutAdaptiveRadioButton;
		private TextBox ColorCountTextBox;
		private Label label4;
		private GroupBox groupBox1;
		private GroupBox groupBox2;
		private RadioButton FloydSteinbergDitherRadioButton;
		private RadioButton AtkinsonDitherRadioButton;
		private Label label7;
		private Label label6;
		private Label label5;
		private RadioButton StuckiDitherRadioButton;
		private RadioButton Ordered8x8RadioButton;
		private RadioButton Ordered4x4RadioButton;
		private RadioButton Ordered2x2RadioButton;
		private RadioButton NoDitheringRadioButton;
		private Button OKButton;
		private Button CancelButton;
		private CheckBox IncludeAlphaCheckBox;
		private RadioButton BWPaletteRadioButton;
		private RadioButton BurkesDitherRadioButton;
		private RadioButton JarvisDitherRadioButton;
		private CheckBox UseVisualWeightingCheckBox;
		private RadioButton OutputPaletteRadioButton;
	}
}