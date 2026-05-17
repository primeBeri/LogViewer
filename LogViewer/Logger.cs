using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// Represents a logger that provides logging functionality with customizable output settings.
    /// </summary>
    /// <remarks>This logger allows specifying a handle, color, and log level for log messages. It extends the
    /// functionality of <see cref="BaseLogger"/> and implements the <see cref="ILoggable"/> interface.</remarks>
    /// <param name="handle"></param>
    /// <param name="color"></param>
    /// <param name="logLevel"></param>
    internal sealed class Logger(string? handle = null, LogColor? color = null, LogLevel logLevel = LogLevel.Information) : BaseLogger(handle, color ?? LogColor.Black, logLevel), ILoggable, ILogger
    {
    }
}
