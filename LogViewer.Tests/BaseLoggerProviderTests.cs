using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogViewer.Tests
{
    public class BaseLoggerProviderTests
    {
        private readonly TestBaseLoggerSink _sink = new();

        [Fact]
        public void CreateLogger_ReturnsLogViewerLogger()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act
            var logger = provider.CreateLogger("TestCategory");

            // Assert
            logger.Should().BeOfType<BaseLogger>();
        }

        [Fact]
        public void CreateLogger_CachesLoggersByCategory()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act
            var logger1 = provider.CreateLogger("TestCategory");
            var logger2 = provider.CreateLogger("TestCategory");

            // Assert
            logger1.Should().BeSameAs(logger2);
        }

        [Fact]
        public void CreateLogger_DifferentCategoriesReturnDifferentLoggers()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act
            var logger1 = provider.CreateLogger("Category1");
            var logger2 = provider.CreateLogger("Category2");

            // Assert
            logger1.Should().NotBeSameAs(logger2);
        }

        [Fact]
        public void SetCategoryColor_UpdatesColorMapping()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);
            provider.SetCategoryColor("TestCategory", LogColor.FromRgb(0, 0, 255));

            // Act
            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("Test");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
            _sink.ReceivedEvents[0].LogColor.Should().Be(LogColor.FromRgb(0, 0, 255));
        }

        [Fact]
        public void SetCategoryColor_Generic_UsesTypeName()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);
            provider.SetCategoryColor<BaseLoggerProviderTests>(LogColor.FromRgb(0, 128, 0));

            // Act
            var logger = provider.CreateLogger(nameof(BaseLoggerProviderTests));
            logger.LogInformation("Test");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
            _sink.ReceivedEvents[0].LogColor.Should().Be(LogColor.FromRgb(0, 128, 0));
        }

        [Fact]
        public void SetCategoryColors_SetsMultipleColors()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);
            var colors = new Dictionary<string, LogColor>
            {
                { "Category1", LogColor.FromRgb(255, 0, 0) },
                { "Category2", LogColor.FromRgb(0, 0, 255) }
            };

            // Act
            provider.SetCategoryColors(colors);
            var logger1 = provider.CreateLogger("Category1");
            var logger2 = provider.CreateLogger("Category2");
            logger1.LogInformation("Test1");
            logger2.LogInformation("Test2");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(2);
            _sink.ReceivedEvents[0].LogColor.Should().Be(LogColor.FromRgb(255, 0, 0));
            _sink.ReceivedEvents[1].LogColor.Should().Be(LogColor.FromRgb(0, 0, 255));
        }

        [Fact]
        public void DefaultColor_IsBlack()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act
            var logger = provider.CreateLogger("UnknownCategory");
            logger.LogInformation("Test");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
            _sink.ReceivedEvents[0].LogColor.Should().Be(LogColor.Black);
        }

        [Fact]
        public void MinimumLevel_DefaultsToTrace()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Assert
            provider.MinimumLevel.Should().Be(LogLevel.Trace);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act
            var act = () =>
            {
                provider.Dispose();
                provider.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SetCategoryColor_WithNullOrEmpty_Throws()
        {
            // Arrange
            var provider = new BaseLoggerProvider(_sink, null);

            // Act & Assert
            var act1 = () => provider.SetCategoryColor(null!, LogColor.FromRgb(0, 0, 255));
            var act2 = () => provider.SetCategoryColor("", LogColor.FromRgb(0, 0, 255));
            var act3 = () => provider.SetCategoryColor("   ", LogColor.FromRgb(0, 0, 255));

            act1.Should().Throw<ArgumentException>();
            act2.Should().Throw<ArgumentException>();
            act3.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_WithInnerFactory_CreatesWrappedLoggers()
        {
            // Arrange
            var innerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace));
            var provider = new BaseLoggerProvider(_sink, innerFactory);

            // Act
            var logger = provider.CreateLogger("TestCategory");
            logger.LogInformation("Test message");

            // Assert
            _sink.ReceivedEvents.Should().HaveCount(1);
        }
    }
}
