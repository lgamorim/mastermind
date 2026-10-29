using System;
using FluentAssertions;
using Mastermind.WebApp.Services;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class AppVersionTests
{
    [Fact]
    public void Should_ShowMajorMinorAndPatch_When_TheVersionHasFourParts()
    {
        var formatted = AppVersion.Format(new Version(1, 3, 0, 0));

        formatted.Should().Be("1.3.0");
    }

    [Fact]
    public void Should_ShowMajorMinorAndPatch_When_TheVersionHasNoRevision()
    {
        var formatted = AppVersion.Format(new Version(2, 10, 4));

        formatted.Should().Be("2.10.4");
    }

    [Fact]
    public void Should_ShowAZeroPatch_When_TheVersionHasNoPatch()
    {
        var formatted = AppVersion.Format(new Version(1, 3));

        formatted.Should().Be("1.3.0");
    }

    [Fact]
    public void Should_ReportTheVersionTheBuildWasStampedWith_When_Read()
    {
        // Directory.Build.props stamps every project with the same <Version>, this one included.
        // Every SDK-built assembly carries a version, so it is never null here.
        var stamped = typeof(AppVersionTests).Assembly.GetName().Version!;

        AppVersion.Current.Should().Be(AppVersion.Format(stamped));
    }
}
