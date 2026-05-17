using System;

namespace LogViewer
{
    /// <summary>
    /// Represents a platform-neutral color value with alpha, red, green, and blue byte components.
    /// Used in logging interfaces (<see cref="ILoggable"/>, <see cref="LogEventArgs"/>) to decouple
    /// the logging abstractions from WPF-specific types such as <c>System.Windows.Media.Color</c>.
    /// </summary>
    /// <remarks>
    /// To convert a <see cref="LogColor"/> to a WPF <c>SolidColorBrush</c>, use the
    /// <c>LogColorWpfExtensions.ToSolidColorBrush</c> extension method (defined in the same assembly).
    /// </remarks>
    public readonly struct LogColor : IEquatable<LogColor>
    {
        /// <summary>
        /// Gets the alpha (opacity) channel of the color, in the range 0 (transparent) to 255 (opaque).
        /// </summary>
        public byte A { get; }

        /// <summary>
        /// Gets the red channel of the color, in the range 0 to 255.
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Gets the green channel of the color, in the range 0 to 255.
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Gets the blue channel of the color, in the range 0 to 255.
        /// </summary>
        public byte B { get; }

        private LogColor(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Creates a <see cref="LogColor"/> from the specified alpha, red, green, and blue byte values.
        /// </summary>
        /// <param name="a">The alpha (opacity) component, 0–255.</param>
        /// <param name="r">The red component, 0–255.</param>
        /// <param name="g">The green component, 0–255.</param>
        /// <param name="b">The blue component, 0–255.</param>
        /// <returns>A new <see cref="LogColor"/> with the specified ARGB values.</returns>
        public static LogColor FromArgb(byte a, byte r, byte g, byte b) => new(a, r, g, b);

        /// <summary>
        /// Creates a fully opaque <see cref="LogColor"/> from the specified red, green, and blue byte values.
        /// The alpha channel is set to 255 (fully opaque).
        /// </summary>
        /// <param name="r">The red component, 0–255.</param>
        /// <param name="g">The green component, 0–255.</param>
        /// <param name="b">The blue component, 0–255.</param>
        /// <returns>A new <see cref="LogColor"/> with A=255 and the specified RGB values.</returns>
        public static LogColor FromRgb(byte r, byte g, byte b) => FromArgb(255, r, g, b);

        /// <summary>
        /// Gets a predefined <see cref="LogColor"/> representing opaque black (A=255, R=0, G=0, B=0).
        /// Used as the default logger color.
        /// </summary>
        public static readonly LogColor Black = FromArgb(255, 0, 0, 0);

        /// <summary>
        /// Returns a string representation of the color in <c>#AARRGGBB</c> uppercase hex format,
        /// for example <c>#FF000000</c> for opaque black.
        /// This format is used by <see cref="LogEventArgs.FormatLogMessage"/> for the <c>{color}</c> placeholder.
        /// </summary>
        /// <returns>A string in <c>#AARRGGBB</c> format.</returns>
        public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

        /// <summary>
        /// Determines whether this <see cref="LogColor"/> is equal to another <see cref="LogColor"/>
        /// by comparing all four ARGB channel values.
        /// </summary>
        /// <param name="other">The other <see cref="LogColor"/> to compare against.</param>
        /// <returns><see langword="true"/> if all four channels are equal; otherwise <see langword="false"/>.</returns>
        public bool Equals(LogColor other) => A == other.A && R == other.R && G == other.G && B == other.B;

        /// <summary>
        /// Determines whether the specified object is equal to this <see cref="LogColor"/>.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="LogColor"/> with identical ARGB values; otherwise <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => obj is LogColor other && Equals(other);

        /// <summary>
        /// Returns a hash code for this <see cref="LogColor"/> derived from all four ARGB channel values.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(A, R, G, B);

        /// <summary>
        /// Returns <see langword="true"/> if two <see cref="LogColor"/> values have identical ARGB channels.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if equal; otherwise <see langword="false"/>.</returns>
        public static bool operator ==(LogColor left, LogColor right) => left.Equals(right);

        /// <summary>
        /// Returns <see langword="true"/> if two <see cref="LogColor"/> values differ in at least one ARGB channel.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if not equal; otherwise <see langword="false"/>.</returns>
        public static bool operator !=(LogColor left, LogColor right) => !left.Equals(right);
    }
}
