using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogViewer.Tests
{
    [Collection(GlobalStateCollection.Name)]
    public class BaseLoggerLoggingBuilderExtensionsTests
    {
        [Fact]
        public void AddLogViewer_RegistersProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddLogViewer());

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Test");

            // Assert
            logger.Should().NotBeNull();
        }

        [Fact]
        public void AddLogViewer_RegistersSink()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddLogViewer());

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var sink = serviceProvider.GetService<IBaseLoggerSink>();

            // Assert
            sink.Should().NotBeNull();
            sink.Should().BeSameAs(BaseLoggerSink.Instance);
        }

        [Fact]
        public void AddLogViewer_WithOptions_AppliesMaxQueueSize()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddLogViewer(options =>
            {
                options.MaxLogQueueSize = 5000;
            }));

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var sink = serviceProvider.GetRequiredService<IBaseLoggerSink>();

            // Assert
            sink.MaxQueueSize.Should().Be(5000);
        }

        [Fact]
        public void AddLogViewer_WithOptions_AppliesMinimumLevel()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddLogViewer(options =>
            {
                options.MinimumLevel = LogLevel.Warning;
            }));

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Test");

            // Assert
            // Trace level should not be enabled when minimum is Warning
            logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
            logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
        }

        [Fact]
        public void AddLogViewer_WithCategoryColors_SetsColors()
        {
            // Arrange - use a TestBaseLoggerSink to isolate this test
            var testSink = new TestBaseLoggerSink();
            var provider = new BaseLoggerProvider(testSink, null);
            provider.SetCategoryColor("ColorTestCategory", LogColor.FromRgb(128, 0, 128));

            // Act
            var logger = provider.CreateLogger("ColorTestCategory");
            logger.LogInformation("Test message");

            // Assert
            testSink.ReceivedEvents.Should().HaveCount(1);
            testSink.ReceivedEvents[0].LogColor.Should().Be(LogColor.FromRgb(128, 0, 128));
        }

        [Fact]
        public void AddLogViewer_WithDateTimeFormat_AppliesFormat()
        {
            // Arrange
            var services = new ServiceCollection();
            var customFormat = "HH:mm:ss";
            services.AddLogging(builder => builder.AddLogViewer(options =>
            {
                options.LogDateTimeFormat = customFormat;
            }));

            // Act
            services.BuildServiceProvider();

            // Assert — DI path stores format on sink.Options, not on the static
            BaseLoggerSink.Instance.Options?.LogDateTimeFormat.Should().Be(customFormat);
        }

        [Fact]
        public void AddLogViewer_WithUtcTime_AppliesUtcSetting()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddLogViewer(options =>
            {
                options.LogUTCTime = true;
            }));

            // Act
            services.BuildServiceProvider();

            // Assert — DI path stores UTC flag on sink.Options, not on the static
            BaseLoggerSink.Instance.Options?.LogUTCTime.Should().BeTrue();
        }

        [Fact]
        public void AddLogViewer_WithNullBuilder_Throws()
        {
            // Arrange
            ILoggingBuilder? builder = null;

            // Act
            var act = () => builder!.AddLogViewer();

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddLogViewer_WithNullConfigure_Throws()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () => services.AddLogging(builder => builder.AddLogViewer((Action<LogViewerOptions>)null!));

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
