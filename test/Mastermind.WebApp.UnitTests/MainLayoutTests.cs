using Bunit;
using FluentAssertions;
using Mastermind.WebApp.Layout;
using Mastermind.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class MainLayoutTests : BunitContext
{
    [Fact]
    public void Should_ShowTheAppVersion_When_Rendered()
    {
        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddContent(0, "The page"))));

        cut.Find(".app-version").TextContent.Should().Be($"Version {AppVersion.Current}");
    }

    [Fact]
    public void Should_RenderThePage_When_Rendered()
    {
        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddContent(0, "The page"))));

        cut.Markup.Should().Contain("The page");
    }
}
