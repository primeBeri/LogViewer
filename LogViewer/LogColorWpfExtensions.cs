using System.Windows.Media;

namespace LogViewer
{
    /// <summary>
    /// WPF-specific extensions for <see cref="LogColor"/>.
    /// Not for use outside WPF assemblies — references <c>System.Windows.Media</c>.
    /// </summary>
    /// <remarks>
    /// These extension methods live in the same <c>LogViewer</c> assembly as the core types because
    /// the library is WPF-only by design (<c>net*-windows</c> TFMs). The separation between
    /// <see cref="LogColor"/> and this class keeps the struct itself free of any WPF dependency,
    /// enabling headless unit testing of code that works with <see cref="LogColor"/> values.
    /// </remarks>
    public static class LogColorWpfExtensions
    {
        /// <summary>
        /// Converts a <see cref="LogColor"/> to a <see cref="SolidColorBrush"/> suitable for WPF data binding.
        /// </summary>
        /// <param name="logColor">The <see cref="LogColor"/> to convert.</param>
        /// <returns>
        /// A <see cref="SolidColorBrush"/> whose color is constructed from the alpha, red, green,
        /// and blue channels of <paramref name="logColor"/>.
        /// </returns>
        public static SolidColorBrush ToSolidColorBrush(this LogColor logColor)
        {
            var brush = new SolidColorBrush(Color.FromArgb(logColor.A, logColor.R, logColor.G, logColor.B));
            brush.Freeze();
            return brush;
        }
    }
}
