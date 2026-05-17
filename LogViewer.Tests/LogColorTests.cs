using FluentAssertions;
using LogViewer;
using Xunit;

namespace LogViewer.Tests
{
    /// <summary>
    /// Tests for the <see cref="LogColor"/> value type.
    /// </summary>
    public class LogColorTests
    {
        [Fact]
        public void FromArgb_SetsAllChannelsCorrectly()
        {
            var color = LogColor.FromArgb(255, 0, 0, 0);

            color.A.Should().Be(255);
            color.R.Should().Be(0);
            color.G.Should().Be(0);
            color.B.Should().Be(0);
        }

        [Fact]
        public void FromRgb_SetsRgbChannels_AndAlphaIs255()
        {
            var color = LogColor.FromRgb(255, 0, 0);

            color.A.Should().Be(255);
            color.R.Should().Be(255);
            color.G.Should().Be(0);
            color.B.Should().Be(0);
        }

        [Fact]
        public void Black_EqualsFromArgb_255_0_0_0()
        {
            LogColor.Black.Should().Be(LogColor.FromArgb(255, 0, 0, 0));
        }

        [Fact]
        public void Black_ToString_ReturnsHexFF000000()
        {
            LogColor.Black.ToString().Should().Be("#FF000000");
        }

        [Fact]
        public void ToString_ReturnsCorrectAaRrGgBbFormat()
        {
            var color = LogColor.FromArgb(255, 128, 64, 32);

            color.ToString().Should().Be("#FF804020");
        }

        [Fact]
        public void Equals_ReturnsTrueForIdenticalArgbValues()
        {
            var a = LogColor.FromArgb(100, 10, 20, 30);
            var b = LogColor.FromArgb(100, 10, 20, 30);

            a.Should().Be(b);
            (a == b).Should().BeTrue();
        }

        [Fact]
        public void Equals_ReturnsFalseForDifferentArgbValues()
        {
            var a = LogColor.FromArgb(100, 10, 20, 30);
            var b = LogColor.FromArgb(200, 10, 20, 30);

            a.Should().NotBe(b);
            (a != b).Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_SameForEqualColors()
        {
            var a = LogColor.FromArgb(255, 128, 64, 32);
            var b = LogColor.FromArgb(255, 128, 64, 32);

            a.GetHashCode().Should().Be(b.GetHashCode());
        }
    }
}
