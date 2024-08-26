namespace HalfMaid.Img.FileFormats.Jpeg.LibJpegTurbo
{
	/// <summary>
	/// A parameter that can be supplied to TurboJpeg to configure its behavior.
	/// </summary>
	internal enum Param
	{
		/// <summary>
		/// Error handling behavior
		///
		/// **Value**
		/// - `0` *[default]* Allow the current compression/decompression/transform
		/// operation to complete unless a fatal error is encountered.
		/// - `1` Immediately discontinue the current
		/// compression/decompression/transform operation if a warning (non-fatal
		/// error) occurs.
		/// </summary>
		StopOnWarning = 0,

		/// <summary>
		/// Row order in packed-pixel source/destination images
		///
		/// **Value**
		/// - `0` *[default]* top-down (X11) order
		/// - `1` bottom-up (Windows, OpenGL) order
		/// </summary>
		BottomUp = 1,

		/// <summary>
		/// JPEG destination buffer (re)allocation [compression, lossless
		/// transformation]
		///
		/// **Value**
		/// - `0` *[default]* Attempt to allocate or reallocate the JPEG destination
		/// buffer as needed.
		/// - `1` Generate an error if the JPEG destination buffer is invalid or too
		/// small.
		/// </summary>
		NoRealloc = 2,

		/// <summary>
		/// Perceptual quality of lossy JPEG images [compression only]
		///
		/// **Value**
		/// - `1`-`100` (`1` = worst quality but best compression, `100` = best
		/// quality but worst compression) *[no default; must be explicitly
		/// specified]*
		/// </summary>
		Quality = 3,

		/// <summary>
		/// Chrominance subsampling level
		///
		/// The JPEG or YUV image uses (decompression, decoding) or will use (lossy
		/// compression, encoding) the specified level of chrominance subsampling.
		///
		/// **Value**
		/// - One of the @ref TJSAMP "chrominance subsampling options" *[no default;
		/// must be explicitly specified for lossy compression, encoding, and
		/// decoding]*
		/// </summary>
		SubSamp = 4,

		/// <summary>
		/// JPEG width (in pixels) [decompression only, read-only]
		/// </summary>
		JpegWidth = 5,

		/// <summary>
		/// JPEG height (in pixels) [decompression only, read-only]
		/// </summary>
		JpegHeight = 6,

		/// <summary>
		/// JPEG data precision (bits per sample) [decompression only, read-only]
		///
		/// The JPEG image uses the specified number of bits per sample.
		///
		/// **Value**
		/// - `8`, `12`, or `16`
		///
		/// 12-bit data precision implies #TJPARAM_OPTIMIZE unless #TJPARAM_ARITHMETIC
		/// is set.
		/// </summary>
		Precision = 7,

		/// <summary>
		/// JPEG colorspace
		///
		/// The JPEG image uses (decompression) or will use (lossy compression) the
		/// specified colorspace.
		///
		/// **Value**
		/// - One of the @ref TJCS "JPEG colorspaces" *[default for lossy compression:
		/// automatically selected based on the subsampling level and pixel format]*
		/// </summary>
		ColorSpace = 8,

		/// <summary>
		/// Chrominance upsampling algorithm [lossy decompression only]
		///
		/// **Value**
		/// - `0` *[default]* Use smooth upsampling when decompressing a JPEG image
		/// that was compressed using chrominance subsampling.  This creates a smooth
		/// transition between neighboring chrominance components in order to reduce
		/// upsampling artifacts in the decompressed image.
		/// - `1` Use the fastest chrominance upsampling algorithm available, which
		/// may combine upsampling with color conversion.
		/// </summary>
		FastUpsample = 9,

		/// <summary>
		/// DCT/IDCT algorithm [lossy compression and decompression]
		///
		/// **Value**
		/// - `0` *[default]* Use the most accurate DCT/IDCT algorithm available.
		/// - `1` Use the fastest DCT/IDCT algorithm available.
		///
		/// This parameter is provided mainly for backward compatibility with libjpeg,
		/// which historically implemented several different DCT/IDCT algorithms
		/// because of performance limitations with 1990s CPUs.  In the libjpeg-turbo
		/// implementation of the TurboJPEG API:
		/// - The "fast" and "accurate" DCT/IDCT algorithms perform similarly on
		/// modern x86/x86-64 CPUs that support AVX2 instructions.
		/// - The "fast" algorithm is generally only about 5-15% faster than the
		/// "accurate" algorithm on other types of CPUs.
		/// - The difference in accuracy between the "fast" and "accurate" algorithms
		/// is the most pronounced at JPEG quality levels above 90 and tends to be
		/// more pronounced with decompression than with compression.
		/// - The "fast" algorithm degrades and is not fully accelerated for JPEG
		/// quality levels above 97, so it will be slower than the "accurate"
		/// algorithm.
		/// </summary>
		FastDct = 10,

		/// <summary>
		/// Optimized baseline entropy coding [lossy compression only]
		///
		/// **Value**
		/// - `0` *[default]* The JPEG image will use the default Huffman tables.
		/// - `1` Optimal Huffman tables will be computed for the JPEG image.  For
		/// lossless transformation, this can also be specified using
		/// #TJXOPT_OPTIMIZE.
		///
		/// Optimized baseline entropy coding will improve compression slightly
		/// (generally 5% or less), but it will reduce compression performance
		/// considerably.
		/// </summary>
		Optimize = 11,

		/// <summary>
		/// Progressive entropy coding
		///
		/// **Value**
		/// - `0` *[default for compression, lossless transformation]* The lossy JPEG
		/// image uses (decompression) or will use (compression, lossless
		/// transformation) baseline entropy coding.
		/// - `1` The lossy JPEG image uses (decompression) or will use (compression,
		/// lossless transformation) progressive entropy coding.  For lossless
		/// transformation, this can also be specified using #TJXOPT_PROGRESSIVE.
		///
		/// Progressive entropy coding will generally improve compression relative to
		/// baseline entropy coding, but it will reduce compression and decompression
		/// performance considerably.  Can be combined with #TJPARAM_ARITHMETIC.
		/// Implies #TJPARAM_OPTIMIZE unless #TJPARAM_ARITHMETIC is also set.
		/// </summary>
		Progressive = 12,

		/// <summary>
		/// Progressive JPEG scan limit for lossy JPEG images [decompression, lossless
		/// transformation]
		///
		/// Setting this parameter will cause the decompression and transform
		/// functions to return an error if the number of scans in a progressive JPEG
		/// image exceeds the specified limit.  The primary purpose of this is to
		/// allow security-critical applications to guard against an exploit of the
		/// progressive JPEG format described in
		/// <a href="https://libjpeg-turbo.org/pmwiki/uploads/About/TwoIssueswiththeJPEGStandard.pdf" target="_blank">this report</a>.
		///
		/// **Value**
		/// - maximum number of progressive JPEG scans that the decompression and
		/// transform functions will process *[default: `0` (no limit)]*
		///
		/// @see #TJPARAM_PROGRESSIVE
		/// </summary>
		ScanLimit = 13,

		/// <summary>
		/// Arithmetic entropy coding
		///
		/// **Value**
		/// - `0` *[default for compression, lossless transformation]* The lossy JPEG
		/// image uses (decompression) or will use (compression, lossless
		/// transformation) Huffman entropy coding.
		/// - `1` The lossy JPEG image uses (decompression) or will use (compression,
		/// lossless transformation) arithmetic entropy coding.  For lossless
		/// transformation, this can also be specified using #TJXOPT_ARITHMETIC.
		///
		/// Arithmetic entropy coding will generally improve compression relative to
		/// Huffman entropy coding, but it will reduce compression and decompression
		/// performance considerably.  Can be combined with #TJPARAM_PROGRESSIVE.
		/// </summary>
		Arithmetic = 14,

		/// <summary>
		/// Lossless JPEG
		///
		/// **Value**
		/// - `0` *[default for compression]* The JPEG image is (decompression) or
		/// will be (compression) lossy/DCT-based.
		/// - `1` The JPEG image is (decompression) or will be (compression)
		/// lossless/predictive.
		///
		/// In most cases, compressing and decompressing lossless JPEG images is
		/// considerably slower than compressing and decompressing lossy JPEG images,
		/// and lossless JPEG images are much larger than lossy JPEG images.  Thus,
		/// lossless JPEG images are typically used only for applications that require
		/// mathematically lossless compression.  Also note that the following
		/// features are not available with lossless JPEG images:
		/// - Colorspace conversion (lossless JPEG images always use #TJCS_RGB,
		/// #TJCS_GRAY, or #TJCS_CMYK, depending on the pixel format of the source
		/// image)
		/// - Chrominance subsampling (lossless JPEG images always use #TJSAMP_444)
		/// - JPEG quality selection
		/// - DCT/IDCT algorithm selection
		/// - Progressive entropy coding
		/// - Arithmetic entropy coding
		/// - Compression from/decompression to planar YUV images
		/// - Decompression scaling
		/// - Lossless transformation
		///
		/// @see #TJPARAM_LOSSLESSPSV, #TJPARAM_LOSSLESSPT
		/// </summary>
		Lossless = 15,

		/// <summary>
		/// Lossless JPEG predictor selection value (PSV)
		///
		/// **Value**
		/// - `1`-`7` *[default for compression: `1`]*
		///
		/// Lossless JPEG compression shares no algorithms with lossy JPEG
		/// compression.  Instead, it uses differential pulse-code modulation (DPCM),
		/// an algorithm whereby each sample is encoded as the difference between the
		/// sample's value and a "predictor", which is based on the values of
		/// neighboring samples.  If Ra is the sample immediately to the left of the
		/// current sample, Rb is the sample immediately above the current sample, and
		/// Rc is the sample diagonally to the left and above the current sample, then
		/// the relationship between the predictor selection value and the predictor
		/// is as follows:
		///
		/// PSV | Predictor
		/// ----|----------
		/// 1   | Ra
		/// 2   | Rb
		/// 3   | Rc
		/// 4   | Ra + Rb – Rc
		/// 5   | Ra + (Rb – Rc) / 2
		/// 6   | Rb + (Ra – Rc) / 2
		/// 7   | (Ra + Rb) / 2
		///
		/// Predictors 1-3 are 1-dimensional predictors, whereas Predictors 4-7 are
		/// 2-dimensional predictors.  The best predictor for a particular image
		/// depends on the image.
		///
		/// @see #TJPARAM_LOSSLESS
		/// </summary>
		LosslessPsv = 16,

		/// <summary>
		/// Lossless JPEG point transform (Pt)
		///
		/// **Value**
		/// - `0` through ***precision*** *- 1*, where ***precision*** is the JPEG
		/// data precision in bits *[default for compression: `0`]*
		///
		/// A point transform value of `0` is necessary in order to generate a fully
		/// lossless JPEG image.  (A non-zero point transform value right-shifts the
		/// input samples by the specified number of bits, which is effectively a form
		/// of lossy color quantization.)
		///
		/// @see #TJPARAM_LOSSLESS, #TJPARAM_PRECISION
		/// </summary>
		LosslessPt = 17,

		/// <summary>
		/// JPEG restart marker interval in MCU blocks (lossy) or samples (lossless)
		/// [compression only]
		///
		/// The nature of entropy coding is such that a corrupt JPEG image cannot
		/// be decompressed beyond the point of corruption unless it contains restart
		/// markers.  A restart marker stops and restarts the entropy coding algorithm
		/// so that, if a JPEG image is corrupted, decompression can resume at the
		/// next marker.  Thus, adding more restart markers improves the fault
		/// tolerance of the JPEG image, but adding too many restart markers can
		/// adversely affect the compression ratio and performance.
		///
		/// **Value**
		/// - the number of MCU blocks or samples between each restart marker
		/// *[default: `0` (no restart markers)]*
		///
		/// Setting this parameter to a non-zero value sets #TJPARAM_RESTARTROWS to 0.
		/// </summary>
		RestartBlocks = 18,

		/// <summary>
		/// JPEG restart marker interval in MCU rows (lossy) or sample rows (lossless)
		/// [compression only]
		///
		/// See #TJPARAM_RESTARTBLOCKS for a description of restart markers.
		///
		/// **Value**
		/// - the number of MCU rows or sample rows between each restart marker
		/// *[default: `0` (no restart markers)]*
		///
		/// Setting this parameter to a non-zero value sets #TJPARAM_RESTARTBLOCKS to
		/// 0.
		/// </summary>
		RestartRows = 19,

		/// <summary>
		/// JPEG horizontal pixel density
		///
		/// **Value**
		/// - The JPEG image has (decompression) or will have (compression) the
		/// specified horizontal pixel density *[default for compression: `1`]*.
		///
		/// This value is stored in or read from the JPEG header.  It does not affect
		/// the contents of the JPEG image.  Note that this parameter is set by
		/// #tj3LoadImage8() when loading a Windows BMP file that contains pixel
		/// density information, and the value of this parameter is stored to a
		/// Windows BMP file by #tj3SaveImage8() if the value of #TJPARAM_DENSITYUNITS
		/// is `2`.
		///
		/// @see TJPARAM_DENSITYUNITS
		/// </summary>
		XDensity = 20,

		/// <summary>
		/// JPEG vertical pixel density
		///
		/// **Value**
		/// - The JPEG image has (decompression) or will have (compression) the
		/// specified vertical pixel density *[default for compression: `1`]*.
		///
		/// This value is stored in or read from the JPEG header.  It does not affect
		/// the contents of the JPEG image.  Note that this parameter is set by
		/// #tj3LoadImage8() when loading a Windows BMP file that contains pixel
		/// density information, and the value of this parameter is stored to a
		/// Windows BMP file by #tj3SaveImage8() if the value of #TJPARAM_DENSITYUNITS
		/// is `2`.
		///
		/// @see TJPARAM_DENSITYUNITS
		/// </summary>
		YDensity = 21,

		/// <summary>
		/// JPEG pixel density units
		///
		/// **Value**
		/// - `0` *[default for compression]* The pixel density of the JPEG image is
		/// expressed (decompression) or will be expressed (compression) in unknown
		/// units.
		/// - `1` The pixel density of the JPEG image is expressed (decompression) or
		/// will be expressed (compression) in units of pixels/inch.
		/// - `2` The pixel density of the JPEG image is expressed (decompression) or
		/// will be expressed (compression) in units of pixels/cm.
		///
		/// This value is stored in or read from the JPEG header.  It does not affect
		/// the contents of the JPEG image.  Note that this parameter is set by
		/// #tj3LoadImage8() when loading a Windows BMP file that contains pixel
		/// density information, and the value of this parameter is stored to a
		/// Windows BMP file by #tj3SaveImage8() if the value is `2`.
		///
		/// @see TJPARAM_XDENSITY, TJPARAM_YDENSITY
		/// </summary>
		DensityUnits = 22,

		/// <summary>
		/// Memory limit for intermediate buffers
		///
		/// **Value**
		/// - the maximum amount of memory (in megabytes) that will be allocated for
		/// intermediate buffers, which are used with progressive JPEG compression and
		/// decompression, optimized baseline entropy coding, lossless JPEG
		/// compression, and lossless transformation *[default: `0` (no limit)]*
		/// </summary>
		MaxMemory = 23,

		/// <summary>
		/// Image size limit [decompression, lossless transformation, packed-pixel
		/// image loading]
		///
		/// Setting this parameter will cause the decompression, transform, and image
		/// loading functions to return an error if the number of pixels in the source
		/// image exceeds the specified limit.  This allows security-critical
		/// applications to guard against excessive memory consumption.
		///
		/// **Value**
		/// - maximum number of pixels that the decompression, transform, and image
		/// loading functions will process *[default: `0` (no limit)]*
		/// </summary>
		MaxPixels = 24,
	}
}
