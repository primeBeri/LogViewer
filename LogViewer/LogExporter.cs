using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogViewer
{
    /// <summary>
    /// Pure-function helpers that serialize a collection of <see cref="LogEventArgs"/>
    /// into JSON, CSV, or formatted text. No file I/O or UI; the orchestrating caller
    /// (e.g. <see cref="LogControlViewModel.ExportLogsAsync"/>) is responsible for
    /// choosing a destination and writing the bytes.
    /// </summary>
    internal static class LogExporter
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>
        /// Serializes a collection of <see cref="LogEventArgs"/> as indented JSON.
        /// </summary>
        /// <param name="logEvents">The events to serialize. Cannot be null.</param>
        public static async Task<StringBuilder> GetLogsAsJsonTextAsync(IEnumerable<LogEventArgs> logEvents)
        {
            ArgumentNullException.ThrowIfNull(logEvents, paramName: nameof(logEvents));

            return await Task.Run(() => new StringBuilder(JsonSerializer.Serialize(logEvents, _jsonOptions)));
        }

        /// <summary>
        /// Serializes a collection of <see cref="LogEventArgs"/> as text using the
        /// supplied format string, one line per event. If <paramref name="logExportFormat"/>
        /// is null, <see cref="LogEventArgs.FormatLogMessage"/> falls back to the
        /// global <see cref="BaseLogger.LogExportFormat"/>.
        /// </summary>
        /// <param name="logEvents">The events to serialize. Cannot be null.</param>
        /// <param name="logExportFormat">Optional format string applied to each event.</param>
        public static async Task<StringBuilder> GetLogsAsTextAsync(IEnumerable<LogEventArgs> logEvents, string? logExportFormat = null)
        {
            ArgumentNullException.ThrowIfNull(logEvents, paramName: nameof(logEvents));

            return await Task.Run(() =>
            {
                StringBuilder sb = new();
                foreach (var logEvent in logEvents)
                {
                    sb.AppendLine(logEvent.FormatLogMessage(logExportFormat));
                }
                return sb;
            });
        }

        /// <summary>
        /// Serializes a collection of <see cref="LogEventArgs"/> as CSV using
        /// <see cref="LogEventArgsMap"/> for the column layout.
        /// </summary>
        /// <param name="logEvents">The events to serialize. Cannot be null.</param>
        public static async Task<StringBuilder> GetLogsAsCSVTextAsync(IEnumerable<LogEventArgs> logEvents)
        {
            ArgumentNullException.ThrowIfNull(logEvents, paramName: nameof(logEvents));

            StringBuilder sb = new();

            await using var writer = new StringWriter(sb);
            await using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<LogEventArgsMap>();
            await csv.WriteRecordsAsync(logEvents);

            return sb;
        }
    }
}
