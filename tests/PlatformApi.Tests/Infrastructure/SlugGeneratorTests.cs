using AgentPort.PlatformApi.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AgentPort.PlatformApi.Tests.Infrastructure;

public sealed class SlugGeneratorTests
{
    [Fact]
    public void FromName_LowercasesAndHyphenatesWhitespace()
    {
        SlugGenerator.FromName("Support Policy").Should().Be("support-policy");
    }

    [Fact]
    public void FromName_CollapsesConsecutiveSeparatorsIntoSingleHyphen()
    {
        SlugGenerator.FromName("Refund   &&   Returns").Should().Be("refund-returns");
    }

    [Fact]
    public void FromName_TreatsUnderscoreDotAndHyphenAsSeparators()
    {
        SlugGenerator.FromName("my_file.name-here").Should().Be("my-file-name-here");
    }

    [Fact]
    public void FromName_TrimsLeadingAndTrailingSeparators()
    {
        SlugGenerator.FromName("  --Hello World--  ").Should().Be("hello-world");
    }

    [Fact]
    public void FromName_DropsNonAsciiAndPunctuationCharacters()
    {
        SlugGenerator.FromName("Cafe!! #Menu?").Should().Be("cafe-menu");
    }

    [Fact]
    public void FromName_KeepsDigits()
    {
        SlugGenerator.FromName("Phase 1.1 Gate").Should().Be("phase-1-1-gate");
    }

    [Fact]
    public void FromName_ReturnsDefaultFallback_WhenNoUsableCharacters()
    {
        SlugGenerator.FromName("!!!").Should().Be("item");
        SlugGenerator.FromName("   ").Should().Be("item");
        SlugGenerator.FromName("").Should().Be("item");
    }

    [Fact]
    public void FromName_ReturnsCustomFallback_WhenProvidedAndNoUsableCharacters()
    {
        SlugGenerator.FromName("###", fallback: "agent").Should().Be("agent");
    }

    [Fact]
    public void FromName_IsIdempotent_OnAlreadySluggedInput()
    {
        var slug = SlugGenerator.FromName("Some Name");

        SlugGenerator.FromName(slug).Should().Be(slug);
    }
}
