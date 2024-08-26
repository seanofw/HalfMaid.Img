namespace ImgTest
{
	partial class MainForm
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			ImageBox = new PictureBox();
			MenuStrip = new MenuStrip();
			File_Menu = new ToolStripMenuItem();
			File_Open = new ToolStripMenuItem();
			File_SaveAs = new ToolStripMenuItem();
			File_Separator1 = new ToolStripSeparator();
			File_Exit = new ToolStripMenuItem();
			Edit_Menu = new ToolStripMenuItem();
			Edit_Undo = new ToolStripMenuItem();
			Edit_Redo = new ToolStripMenuItem();
			toolStripMenuItem6 = new ToolStripSeparator();
			Edit_Copy = new ToolStripMenuItem();
			Edit_Paste = new ToolStripMenuItem();
			Orientation_Menu = new ToolStripMenuItem();
			Orientation_FlipHorizontally = new ToolStripMenuItem();
			Orientation_FlipHorizontallyInPlace = new ToolStripMenuItem();
			Orientation_FlipVertically = new ToolStripMenuItem();
			Orientation_FlipVerticallyInPlace = new ToolStripMenuItem();
			Orientation_Separator1 = new ToolStripSeparator();
			Orientation_Rotate90Clockwise = new ToolStripMenuItem();
			Orientation_Rotate90Counterclockwise = new ToolStripMenuItem();
			Orientation_Rotate180 = new ToolStripMenuItem();
			Orientation_Rotate180InPlace = new ToolStripMenuItem();
			Orientation_Separator2 = new ToolStripSeparator();
			colorToolStripMenuItem = new ToolStripMenuItem();
			Color_RGBA = new ToolStripMenuItem();
			Color_Paletted = new ToolStripMenuItem();
			toolStripMenuItem1 = new ToolStripSeparator();
			Color_SwapRG = new ToolStripMenuItem();
			Color_SwapRB = new ToolStripMenuItem();
			Color_SwapGB = new ToolStripMenuItem();
			toolStripMenuItem2 = new ToolStripSeparator();
			Color_SplitChannelR = new ToolStripMenuItem();
			Color_SplitChannelG = new ToolStripMenuItem();
			Color_SplitChannelB = new ToolStripMenuItem();
			Color_SplitChannelA = new ToolStripMenuItem();
			Effects_Menu = new ToolStripMenuItem();
			Effects_Invert = new ToolStripMenuItem();
			toolStripMenuItem5 = new ToolStripSeparator();
			Effects_HueSaturationBrightness = new ToolStripMenuItem();
			toolStripMenuItem7 = new ToolStripSeparator();
			Effects_Grayscale = new ToolStripMenuItem();
			Effects_Desaturate = new ToolStripMenuItem();
			Effects_Sepia = new ToolStripMenuItem();
			toolStripMenuItem4 = new ToolStripSeparator();
			Effects_Gamma = new ToolStripMenuItem();
			toolStripMenuItem3 = new ToolStripSeparator();
			Effects_BoxBlur = new ToolStripMenuItem();
			Effects_ApproximateGaussianBlur = new ToolStripMenuItem();
			Effects_Sharpen = new ToolStripMenuItem();
			Effects_Emboss = new ToolStripMenuItem();
			Effects_EdgeDetect = new ToolStripMenuItem();
			View_Menu = new ToolStripMenuItem();
			Help_Menu = new ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)ImageBox).BeginInit();
			MenuStrip.SuspendLayout();
			SuspendLayout();
			// 
			// ImageBox
			// 
			ImageBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			ImageBox.BackColor = Color.Black;
			ImageBox.Location = new Point(0, 27);
			ImageBox.Name = "ImageBox";
			ImageBox.Size = new Size(622, 491);
			ImageBox.TabIndex = 0;
			ImageBox.TabStop = false;
			// 
			// MenuStrip
			// 
			MenuStrip.Items.AddRange(new ToolStripItem[] { File_Menu, Edit_Menu, Orientation_Menu, colorToolStripMenuItem, Effects_Menu, View_Menu, Help_Menu });
			MenuStrip.Location = new Point(0, 0);
			MenuStrip.Name = "MenuStrip";
			MenuStrip.Size = new Size(622, 24);
			MenuStrip.TabIndex = 1;
			MenuStrip.Text = "MenuStrip";
			// 
			// File_Menu
			// 
			File_Menu.DropDownItems.AddRange(new ToolStripItem[] { File_Open, File_SaveAs, File_Separator1, File_Exit });
			File_Menu.Name = "File_Menu";
			File_Menu.Size = new Size(37, 20);
			File_Menu.Text = "&File";
			// 
			// File_Open
			// 
			File_Open.Name = "File_Open";
			File_Open.ShortcutKeys = Keys.Control | Keys.O;
			File_Open.Size = new Size(193, 22);
			File_Open.Text = "&Open...";
			File_Open.Click += Open_Click;
			// 
			// File_SaveAs
			// 
			File_SaveAs.Name = "File_SaveAs";
			File_SaveAs.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
			File_SaveAs.Size = new Size(193, 22);
			File_SaveAs.Text = "S&ave as...";
			File_SaveAs.Click += SaveAs_Click;
			// 
			// File_Separator1
			// 
			File_Separator1.Name = "File_Separator1";
			File_Separator1.Size = new Size(190, 6);
			// 
			// File_Exit
			// 
			File_Exit.Name = "File_Exit";
			File_Exit.ShortcutKeys = Keys.Alt | Keys.F4;
			File_Exit.Size = new Size(193, 22);
			File_Exit.Text = "E&xit";
			File_Exit.Click += Exit_Click;
			// 
			// Edit_Menu
			// 
			Edit_Menu.DropDownItems.AddRange(new ToolStripItem[] { Edit_Undo, Edit_Redo, toolStripMenuItem6, Edit_Copy, Edit_Paste });
			Edit_Menu.Name = "Edit_Menu";
			Edit_Menu.Size = new Size(39, 20);
			Edit_Menu.Text = "&Edit";
			Edit_Menu.DropDownOpening += Edit_Menu_Opening;
			// 
			// Edit_Undo
			// 
			Edit_Undo.Enabled = false;
			Edit_Undo.Name = "Edit_Undo";
			Edit_Undo.ShortcutKeys = Keys.Control | Keys.Z;
			Edit_Undo.Size = new Size(174, 22);
			Edit_Undo.Text = "&Undo";
			Edit_Undo.Click += Edit_Undo_Click;
			// 
			// Edit_Redo
			// 
			Edit_Redo.Enabled = false;
			Edit_Redo.Name = "Edit_Redo";
			Edit_Redo.ShortcutKeys = Keys.Control | Keys.Shift | Keys.Z;
			Edit_Redo.Size = new Size(174, 22);
			Edit_Redo.Text = "&Redo";
			Edit_Redo.Click += Edit_Redo_Click;
			// 
			// toolStripMenuItem6
			// 
			toolStripMenuItem6.Name = "toolStripMenuItem6";
			toolStripMenuItem6.Size = new Size(171, 6);
			// 
			// Edit_Copy
			// 
			Edit_Copy.Name = "Edit_Copy";
			Edit_Copy.ShortcutKeys = Keys.Control | Keys.C;
			Edit_Copy.Size = new Size(174, 22);
			Edit_Copy.Text = "&Copy";
			Edit_Copy.Click += Edit_Copy_Click;
			// 
			// Edit_Paste
			// 
			Edit_Paste.Name = "Edit_Paste";
			Edit_Paste.ShortcutKeys = Keys.Control | Keys.V;
			Edit_Paste.Size = new Size(174, 22);
			Edit_Paste.Text = "&Paste";
			Edit_Paste.Click += Edit_Paste_Click;
			// 
			// Orientation_Menu
			// 
			Orientation_Menu.DropDownItems.AddRange(new ToolStripItem[] { Orientation_FlipHorizontally, Orientation_FlipHorizontallyInPlace, Orientation_FlipVertically, Orientation_FlipVerticallyInPlace, Orientation_Separator1, Orientation_Rotate90Clockwise, Orientation_Rotate90Counterclockwise, Orientation_Rotate180, Orientation_Rotate180InPlace, Orientation_Separator2 });
			Orientation_Menu.Name = "Orientation_Menu";
			Orientation_Menu.Size = new Size(79, 20);
			Orientation_Menu.Text = "&Orientation";
			// 
			// Orientation_FlipHorizontally
			// 
			Orientation_FlipHorizontally.Name = "Orientation_FlipHorizontally";
			Orientation_FlipHorizontally.Size = new Size(225, 22);
			Orientation_FlipHorizontally.Text = "Flip &Horizontally";
			Orientation_FlipHorizontally.Click += Orientation_FlipHorizontally_Click;
			// 
			// Orientation_FlipHorizontallyInPlace
			// 
			Orientation_FlipHorizontallyInPlace.Name = "Orientation_FlipHorizontallyInPlace";
			Orientation_FlipHorizontallyInPlace.Size = new Size(225, 22);
			Orientation_FlipHorizontallyInPlace.Text = "Flip Horizontally (inplace)";
			Orientation_FlipHorizontallyInPlace.Click += Orientation_FlipHorizontallyInPlace_Click;
			// 
			// Orientation_FlipVertically
			// 
			Orientation_FlipVertically.Name = "Orientation_FlipVertically";
			Orientation_FlipVertically.Size = new Size(225, 22);
			Orientation_FlipVertically.Text = "Flip &Vertically";
			Orientation_FlipVertically.Click += Orientation_FlipVertically_Click;
			// 
			// Orientation_FlipVerticallyInPlace
			// 
			Orientation_FlipVerticallyInPlace.Name = "Orientation_FlipVerticallyInPlace";
			Orientation_FlipVerticallyInPlace.Size = new Size(225, 22);
			Orientation_FlipVerticallyInPlace.Text = "Flip Vertically (inplace)";
			Orientation_FlipVerticallyInPlace.Click += Orientation_FlipVerticallyInPlace_Click;
			// 
			// Orientation_Separator1
			// 
			Orientation_Separator1.Name = "Orientation_Separator1";
			Orientation_Separator1.Size = new Size(222, 6);
			// 
			// Orientation_Rotate90Clockwise
			// 
			Orientation_Rotate90Clockwise.Name = "Orientation_Rotate90Clockwise";
			Orientation_Rotate90Clockwise.Size = new Size(225, 22);
			Orientation_Rotate90Clockwise.Text = "Rotate 90° Clockwise";
			Orientation_Rotate90Clockwise.Click += Orientation_Rotate90Clockwise_Click;
			// 
			// Orientation_Rotate90Counterclockwise
			// 
			Orientation_Rotate90Counterclockwise.Name = "Orientation_Rotate90Counterclockwise";
			Orientation_Rotate90Counterclockwise.Size = new Size(225, 22);
			Orientation_Rotate90Counterclockwise.Text = "Rotate 90° Counterclockwise";
			Orientation_Rotate90Counterclockwise.Click += Orientation_Rotate90Counterclockwise_Click;
			// 
			// Orientation_Rotate180
			// 
			Orientation_Rotate180.Name = "Orientation_Rotate180";
			Orientation_Rotate180.Size = new Size(225, 22);
			Orientation_Rotate180.Text = "Rotate 180°";
			Orientation_Rotate180.Click += Orientation_Rotate180_Click;
			// 
			// Orientation_Rotate180InPlace
			// 
			Orientation_Rotate180InPlace.Name = "Orientation_Rotate180InPlace";
			Orientation_Rotate180InPlace.Size = new Size(225, 22);
			Orientation_Rotate180InPlace.Text = "Rotate 180° (inplace)";
			Orientation_Rotate180InPlace.Click += Orientation_Rotate180InPlace_Click;
			// 
			// Orientation_Separator2
			// 
			Orientation_Separator2.Name = "Orientation_Separator2";
			Orientation_Separator2.Size = new Size(222, 6);
			// 
			// colorToolStripMenuItem
			// 
			colorToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { Color_RGBA, Color_Paletted, toolStripMenuItem1, Color_SwapRG, Color_SwapRB, Color_SwapGB, toolStripMenuItem2, Color_SplitChannelR, Color_SplitChannelG, Color_SplitChannelB, Color_SplitChannelA });
			colorToolStripMenuItem.Name = "colorToolStripMenuItem";
			colorToolStripMenuItem.Size = new Size(48, 20);
			colorToolStripMenuItem.Text = "&Color";
			// 
			// Color_RGBA
			// 
			Color_RGBA.Name = "Color_RGBA";
			Color_RGBA.Size = new Size(155, 22);
			Color_RGBA.Text = "32-bit RGBA";
			// 
			// Color_Paletted
			// 
			Color_Paletted.Name = "Color_Paletted";
			Color_Paletted.Size = new Size(155, 22);
			Color_Paletted.Text = "8-bit Paletted...";
			Color_Paletted.Click += Color_Paletted_Click;
			// 
			// toolStripMenuItem1
			// 
			toolStripMenuItem1.Name = "toolStripMenuItem1";
			toolStripMenuItem1.Size = new Size(152, 6);
			// 
			// Color_SwapRG
			// 
			Color_SwapRG.Name = "Color_SwapRG";
			Color_SwapRG.Size = new Size(155, 22);
			Color_SwapRG.Text = "Swap R <-> G";
			Color_SwapRG.Click += Color_SwapRG_Click;
			// 
			// Color_SwapRB
			// 
			Color_SwapRB.Name = "Color_SwapRB";
			Color_SwapRB.Size = new Size(155, 22);
			Color_SwapRB.Text = "Swap R <-> B";
			Color_SwapRB.Click += Color_SwapRB_Click;
			// 
			// Color_SwapGB
			// 
			Color_SwapGB.Name = "Color_SwapGB";
			Color_SwapGB.Size = new Size(155, 22);
			Color_SwapGB.Text = "Swap G <-> B";
			Color_SwapGB.Click += Color_SwapGB_Click;
			// 
			// toolStripMenuItem2
			// 
			toolStripMenuItem2.Name = "toolStripMenuItem2";
			toolStripMenuItem2.Size = new Size(152, 6);
			// 
			// Color_SplitChannelR
			// 
			Color_SplitChannelR.Name = "Color_SplitChannelR";
			Color_SplitChannelR.Size = new Size(155, 22);
			Color_SplitChannelR.Text = "Split Channel R";
			Color_SplitChannelR.Click += Color_SplitChannelR_Click;
			// 
			// Color_SplitChannelG
			// 
			Color_SplitChannelG.Name = "Color_SplitChannelG";
			Color_SplitChannelG.Size = new Size(155, 22);
			Color_SplitChannelG.Text = "Split Channel G";
			Color_SplitChannelG.Click += Color_SplitChannelG_Click;
			// 
			// Color_SplitChannelB
			// 
			Color_SplitChannelB.Name = "Color_SplitChannelB";
			Color_SplitChannelB.Size = new Size(155, 22);
			Color_SplitChannelB.Text = "Split Channel B";
			Color_SplitChannelB.Click += Color_SplitChannelB_Click;
			// 
			// Color_SplitChannelA
			// 
			Color_SplitChannelA.Name = "Color_SplitChannelA";
			Color_SplitChannelA.Size = new Size(155, 22);
			Color_SplitChannelA.Text = "Split Channel A";
			Color_SplitChannelA.Click += Color_SplitChannelA_Click;
			// 
			// Effects_Menu
			// 
			Effects_Menu.DropDownItems.AddRange(new ToolStripItem[] { Effects_Invert, toolStripMenuItem5, Effects_HueSaturationBrightness, toolStripMenuItem7, Effects_Grayscale, Effects_Desaturate, Effects_Sepia, toolStripMenuItem4, Effects_Gamma, toolStripMenuItem3, Effects_BoxBlur, Effects_ApproximateGaussianBlur, Effects_Sharpen, Effects_Emboss, Effects_EdgeDetect });
			Effects_Menu.Name = "Effects_Menu";
			Effects_Menu.Size = new Size(54, 20);
			Effects_Menu.Text = "E&ffects";
			// 
			// Effects_Invert
			// 
			Effects_Invert.Name = "Effects_Invert";
			Effects_Invert.Size = new Size(224, 22);
			Effects_Invert.Text = "&Invert";
			Effects_Invert.Click += Effects_Invert_Click;
			// 
			// toolStripMenuItem5
			// 
			toolStripMenuItem5.Name = "toolStripMenuItem5";
			toolStripMenuItem5.Size = new Size(221, 6);
			// 
			// Effects_HueSaturationBrightness
			// 
			Effects_HueSaturationBrightness.Name = "Effects_HueSaturationBrightness";
			Effects_HueSaturationBrightness.Size = new Size(224, 22);
			Effects_HueSaturationBrightness.Text = "&Hue/Saturation/Brightness...";
			Effects_HueSaturationBrightness.Click += Effects_HueSaturationBrightness_Click;
			// 
			// toolStripMenuItem7
			// 
			toolStripMenuItem7.Name = "toolStripMenuItem7";
			toolStripMenuItem7.Size = new Size(221, 6);
			// 
			// Effects_Grayscale
			// 
			Effects_Grayscale.Name = "Effects_Grayscale";
			Effects_Grayscale.Size = new Size(224, 22);
			Effects_Grayscale.Text = "Gra&yscale";
			Effects_Grayscale.Click += Effects_Grayscale_Click;
			// 
			// Effects_Desaturate
			// 
			Effects_Desaturate.Name = "Effects_Desaturate";
			Effects_Desaturate.Size = new Size(224, 22);
			Effects_Desaturate.Text = "&Desaturate...";
			Effects_Desaturate.Click += Effects_Desaturate_Click;
			// 
			// Effects_Sepia
			// 
			Effects_Sepia.Name = "Effects_Sepia";
			Effects_Sepia.Size = new Size(224, 22);
			Effects_Sepia.Text = "Se&pia...";
			Effects_Sepia.Click += Effects_Sepia_Click;
			// 
			// toolStripMenuItem4
			// 
			toolStripMenuItem4.Name = "toolStripMenuItem4";
			toolStripMenuItem4.Size = new Size(221, 6);
			// 
			// Effects_Gamma
			// 
			Effects_Gamma.Name = "Effects_Gamma";
			Effects_Gamma.Size = new Size(224, 22);
			Effects_Gamma.Text = "&Gamma...";
			Effects_Gamma.Click += Effects_Gamma_Click;
			// 
			// toolStripMenuItem3
			// 
			toolStripMenuItem3.Name = "toolStripMenuItem3";
			toolStripMenuItem3.Size = new Size(221, 6);
			// 
			// Effects_BoxBlur
			// 
			Effects_BoxBlur.Name = "Effects_BoxBlur";
			Effects_BoxBlur.Size = new Size(224, 22);
			Effects_BoxBlur.Text = "&Box Blur...";
			Effects_BoxBlur.Click += Effects_BoxBlur_Click;
			// 
			// Effects_ApproximateGaussianBlur
			// 
			Effects_ApproximateGaussianBlur.Name = "Effects_ApproximateGaussianBlur";
			Effects_ApproximateGaussianBlur.Size = new Size(224, 22);
			Effects_ApproximateGaussianBlur.Text = "&Approx. Gaussian Blur...";
			Effects_ApproximateGaussianBlur.Click += Effects_ApproximateGaussianBlur_Click;
			// 
			// Effects_Sharpen
			// 
			Effects_Sharpen.Name = "Effects_Sharpen";
			Effects_Sharpen.Size = new Size(224, 22);
			Effects_Sharpen.Text = "&Sharpen...";
			Effects_Sharpen.Click += Effects_Sharpen_Click;
			// 
			// Effects_Emboss
			// 
			Effects_Emboss.Name = "Effects_Emboss";
			Effects_Emboss.Size = new Size(224, 22);
			Effects_Emboss.Text = "&Emboss...";
			Effects_Emboss.Click += Effects_Emboss_Click;
			// 
			// Effects_EdgeDetect
			// 
			Effects_EdgeDetect.Name = "Effects_EdgeDetect";
			Effects_EdgeDetect.Size = new Size(224, 22);
			Effects_EdgeDetect.Text = "Edge De&tect...";
			Effects_EdgeDetect.Click += Effects_EdgeDetect_Click;
			// 
			// View_Menu
			// 
			View_Menu.Name = "View_Menu";
			View_Menu.Size = new Size(44, 20);
			View_Menu.Text = "&View";
			// 
			// Help_Menu
			// 
			Help_Menu.Name = "Help_Menu";
			Help_Menu.Size = new Size(44, 20);
			Help_Menu.Text = "&Help";
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(622, 515);
			Controls.Add(ImageBox);
			Controls.Add(MenuStrip);
			MainMenuStrip = MenuStrip;
			Name = "MainForm";
			Text = "Image Viewer";
			SizeChanged += MainForm_SizeChanged;
			((System.ComponentModel.ISupportInitialize)ImageBox).EndInit();
			MenuStrip.ResumeLayout(false);
			MenuStrip.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private PictureBox ImageBox;
		private MenuStrip MenuStrip;
		private ToolStripMenuItem File_Menu;
		private ToolStripMenuItem Edit_Menu;
		private ToolStripMenuItem View_Menu;
		private ToolStripMenuItem Help_Menu;
		private ToolStripMenuItem File_Open;
		private ToolStripMenuItem File_SaveAs;
		private ToolStripSeparator File_Separator1;
		private ToolStripMenuItem File_Exit;
		private ToolStripMenuItem Edit_Undo;
		private ToolStripMenuItem Edit_Redo;
		private ToolStripMenuItem Orientation_Menu;
		private ToolStripMenuItem Orientation_FlipHorizontally;
		private ToolStripMenuItem Orientation_FlipVertically;
		private ToolStripSeparator Orientation_Separator1;
		private ToolStripMenuItem Orientation_Rotate90Clockwise;
		private ToolStripMenuItem Orientation_Rotate90Counterclockwise;
		private ToolStripMenuItem Orientation_Rotate180;
		private ToolStripSeparator Orientation_Separator2;
		private ToolStripMenuItem Effects_Invert;
		private ToolStripMenuItem Effects_Grayscale;
		private ToolStripMenuItem Effects_Sepia;
		private ToolStripMenuItem Orientation_FlipHorizontallyInPlace;
		private ToolStripMenuItem Orientation_FlipVerticallyInPlace;
		private ToolStripMenuItem Orientation_Rotate180InPlace;
		private ToolStripMenuItem Effects_Menu;
		private ToolStripMenuItem colorToolStripMenuItem;
		private ToolStripMenuItem Color_RGBA;
		private ToolStripMenuItem Color_Paletted;
		private ToolStripSeparator toolStripMenuItem1;
		private ToolStripMenuItem Color_SwapRG;
		private ToolStripMenuItem Color_SwapRB;
		private ToolStripMenuItem Color_SwapGB;
		private ToolStripSeparator toolStripMenuItem2;
		private ToolStripMenuItem Color_SplitChannelR;
		private ToolStripMenuItem Color_SplitChannelG;
		private ToolStripMenuItem Color_SplitChannelB;
		private ToolStripMenuItem Color_SplitChannelA;
		private ToolStripMenuItem Effects_Desaturate;
		private ToolStripMenuItem Effects_Gamma;
		private ToolStripMenuItem Effects_BoxBlur;
		private ToolStripMenuItem Effects_Sharpen;
		private ToolStripMenuItem Effects_Emboss;
		private ToolStripSeparator toolStripMenuItem5;
		private ToolStripSeparator toolStripMenuItem4;
		private ToolStripSeparator toolStripMenuItem3;
		private ToolStripMenuItem Effects_ApproximateGaussianBlur;
		private ToolStripMenuItem Effects_EdgeDetect;
		private ToolStripSeparator toolStripMenuItem6;
		private ToolStripMenuItem Edit_Copy;
		private ToolStripMenuItem Edit_Paste;
		private ToolStripMenuItem Effects_HueSaturationBrightness;
		private ToolStripSeparator toolStripMenuItem7;
	}
}