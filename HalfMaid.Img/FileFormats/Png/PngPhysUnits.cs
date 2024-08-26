namespace HalfMaid.Img.FileFormats.Png
{
    /// <summary>
    /// Supported physical units, per the PNG standard.  Realistically, there
    /// are only two options:  Unknown, and meters.  (PNG does not support
    /// English-system measurements like DPI.)
    /// </summary>
    public enum PngPhysUnits : byte
    {
        /// <summary>
        /// An unknown physical-unit measurement.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// This image's dimensions are scaled in pixels-per-meter.
        /// </summary>
        PerMeter = 1,
    }
}
