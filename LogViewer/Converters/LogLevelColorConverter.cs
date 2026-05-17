using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using LogViewer;
using Microsoft.Extensions.Logging;

namespace LogViewer.Converters
{
    /// <summary>
    /// Converts a <see cref="LogLevel"/> value to a corresponding <see cref="System.Windows.Media.SolidColorBrush"/> for WPF data binding.
    /// </summary>
    /// <remarks>
    /// This converter is used in XAML to visually distinguish log messages by their severity.
    /// </remarks>
    public class LogLevelColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a <see cref="LogLevel"/> to a <see cref="System.Windows.Media.SolidColorBrush"/> with a color representing the log level.
        /// Uses <see cref="LogColor"/> values internally for platform-neutral color mapping.
        /// </summary>
        /// <param name="value">The value produced by the binding source. Expected to be a <see cref="LogLevel"/>.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter to use in the converter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A <see cref="System.Windows.Media.SolidColorBrush"/> with a color mapped to the specified <see cref="LogLevel"/>.
        /// Returns a black brush if the value is not a <see cref="LogLevel"/>.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is LogLevel logLevel)
            {
                LogColor logColor = logLevel switch
                {
                    LogLevel.Trace    => LogColor.FromRgb(128, 128, 128),
                    LogLevel.Debug    => LogColor.FromRgb(0, 0, 255),
                    LogLevel.Warning  => LogColor.FromRgb(255, 165, 0),
                    LogLevel.Error    => LogColor.FromRgb(255, 0, 0),
                    LogLevel.Critical => LogColor.FromRgb(139, 0, 0),
                    _                 => LogColor.Black
                };
                return logColor.ToSolidColorBrush();
            }
            return LogColor.Black.ToSolidColorBrush();
        }

        /// <summary>
        /// Not implemented. Conversion from a brush back to <see cref="LogLevel"/> is not supported.
        /// </summary>
        /// <param name="value">The value produced by the binding target (not used).</param>
        /// <param name="targetType">The type to convert to (not used).</param>
        /// <param name="parameter">Optional parameter to use in the converter (not used).</param>
        /// <param name="culture">The culture to use in the converter (not used).</param>
        /// <returns>
        /// Always returns <see cref="Binding.DoNothing"/>, indicating that no conversion is performed.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing; // No conversion back needed
        }
    }
}
