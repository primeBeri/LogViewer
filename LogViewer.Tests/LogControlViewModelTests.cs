using System.Text.RegularExpressions;
using FluentAssertions;
using LogViewer;
using Xunit;

namespace LogViewer.Tests
{
    /// <summary>
    /// Tests for the static helpers on <see cref="LogControlViewModel"/> that don't
    /// require a Dispatcher. Instance-level VM tests are deferred to v0.4.0 where
    /// the VM lifecycle changes (DataContext-injected) make testing straightforward.
    /// </summary>
    public class LogControlViewModelTests
    {
        // -------- WildcardToRegex --------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void WildcardToRegex_NullOrWhitespace_ReturnsMatchAll(string? input)
        {
            LogControlViewModel.WildcardToRegex(input).Should().Be(".*");
        }

        [Fact]
        public void WildcardToRegex_StarWildcard_MatchesAnySequence()
        {
            var pattern = LogControlViewModel.WildcardToRegex("*Service*");
            var regex = new Regex(pattern);

            regex.IsMatch("MyService").Should().BeTrue();
            regex.IsMatch("Service").Should().BeTrue();
            regex.IsMatch("MyServiceX").Should().BeTrue();
            regex.IsMatch("Other").Should().BeFalse();
        }

        [Fact]
        public void WildcardToRegex_QuestionWildcard_MatchesSingleChar()
        {
            var pattern = LogControlViewModel.WildcardToRegex("foo?bar");
            var regex = new Regex(pattern);

            regex.IsMatch("fooXbar").Should().BeTrue();
            regex.IsMatch("foo1bar").Should().BeTrue();
            regex.IsMatch("foobar").Should().BeFalse();   // missing single char
            regex.IsMatch("fooXXbar").Should().BeFalse(); // two chars, not one
        }

        [Fact]
        public void WildcardToRegex_DotIsLiteral()
        {
            // A regex would treat '.' as any char, but in wildcard mode it must be literal.
            var pattern = LogControlViewModel.WildcardToRegex("foo.bar");
            var regex = new Regex(pattern);

            regex.IsMatch("foo.bar").Should().BeTrue();
            regex.IsMatch("fooXbar").Should().BeFalse();
        }

        [Fact]
        public void WildcardToRegex_PipeAsAlternation()
        {
            var pattern = LogControlViewModel.WildcardToRegex("*Service*|*Handler*");
            var regex = new Regex(pattern);

            regex.IsMatch("MyService").Should().BeTrue();
            regex.IsMatch("MyHandler").Should().BeTrue();
            regex.IsMatch("Other").Should().BeFalse();
        }

        [Fact]
        public void WildcardToRegex_SpecialRegexCharsAreEscaped()
        {
            // Brackets, parens, braces are regex metacharacters that must be escaped.
            var pattern = LogControlViewModel.WildcardToRegex("a[b]c");
            var regex = new Regex(pattern);

            regex.IsMatch("a[b]c").Should().BeTrue();
            regex.IsMatch("ab").Should().BeFalse();
        }

        [Fact]
        public void WildcardToRegex_ResultIsAnchored()
        {
            // The wildcard converter anchors with ^...$, so partial matches must fail.
            var pattern = LogControlViewModel.WildcardToRegex("Foo");
            var regex = new Regex(pattern);

            regex.IsMatch("Foo").Should().BeTrue();
            regex.IsMatch("FooBar").Should().BeFalse();
            regex.IsMatch("BarFoo").Should().BeFalse();
        }
    }
}
