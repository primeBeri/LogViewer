using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// Canonical options POCO for LogViewer, registered as <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>
    /// by <see cref="BaseLoggerLoggingBuilderExtensions.AddLogViewer(Microsoft.Extensions.Logging.ILoggingBuilder)"/>.
    /// Configure via <c>AddLogViewer(o => { ... })</c> or via <c>appsettings.json</c> after calling
    /// <c>builder.Services.Configure&lt;LogViewerOptions&gt;(configuration.GetSection("LogViewer"))</c>.
    /// </summary>
    public class LogViewerOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of log entries to display in the LogViewer UI.
        /// Oldest entries are dropped once this limit is reached.
        /// Default is 10,000.
        /// </summary>
        public int MaxLogQueueSize { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the timestamp format string used to render <c>LogDateTimeFormatted</c> on each
        /// log entry. Follows standard .NET date/time format patterns.
        /// Default is <c>"yyyy-MM-dd HH:mm:ss.fff (zzz)"</c>.
        /// </summary>
        public string LogDateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff (zzz)";

        /// <summary>
        /// Gets or sets a value indicating whether log entry timestamps should use UTC rather than local time.
        /// Default is <see langword="false"/> (local time).
        /// </summary>
        public bool LogUTCTime { get; set; }

        /// <summary>
        /// Gets or sets the collection of characters that are stripped from logger category names when
        /// forming the display handle. Default is <c>['.', '-', ' ']</c>.
        /// </summary>
        public ICollection<char> ExcludeCharsFromHandle { get; set; } = ['.', '-', ' '];

        /// <summary>
        /// Gets or sets the format string used when exporting log entries as plain text.
        /// Supported placeholders: <c>{timestamp}</c>, <c>{loglevel}</c>, <c>{threadid}</c>,
        /// <c>{color}</c>, <c>{handle}</c>, <c>{message}</c>.
        /// Default is <c>"{timestamp}|{loglevel}|{threadid}|{handle}|{message}"</c>.
        /// </summary>
        public string LogExportFormat { get; set; } = BaseLogger.DefaultLogExportFormat;

        /// <summary>
        /// Gets or sets a dictionary that maps category names to platform-neutral display colours.
        /// Lookups are case-insensitive (uses <see cref="StringComparer.OrdinalIgnoreCase"/>).
        /// </summary>
        public Dictionary<string, LogColor> CategoryColors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the minimum <see cref="LogLevel"/> that LogViewer will process and display.
        /// Events below this level are silently discarded. Default is <see cref="LogLevel.Trace"/>.
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

        /// <summary>
        /// Gets or sets a value indicating whether the namespace prefix is stripped from a logger category
        /// name when forming the display handle. Default is <see langword="true"/>.
        /// </summary>
        public bool StripNamespaceFromCategory { get; set; } = true;
    }
}
