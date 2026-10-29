using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Mastermind.Core;
using Mastermind.WebApp.Components;
using Mastermind.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class HelpDialogTests : BunitContext
{
    public HelpDialogTests()
    {
        JSInterop.SetupVoid("mastermindDialog.showModal", _ => true);
    }

    private IRenderedComponent<HelpDialog> RenderFor(GameMode mode) =>
        Render<HelpDialog>(parameters => parameters.Add(help => help.Mode, mode));

    [Fact]
    public void Should_GiveTheHelpButtonAnAccessibleName_When_Rendered()
    {
        var cut = RenderFor(GameMode.FreePlay);

        cut.Find(".btn-help").GetAttribute("aria-label").Should().Be("How to play");
    }

    [Fact]
    public void Should_OpenTheDialogAsAModal_When_TheHelpButtonIsClicked()
    {
        var cut = RenderFor(GameMode.FreePlay);

        cut.Find(".btn-help").Click();

        var open = JSInterop.VerifyInvoke("mastermindDialog.showModal");
        open.Arguments.Should().ContainSingle().Which.Should().BeOfType<ElementReference>();
    }

    [Fact]
    public void Should_LabelTheDialogByItsHeading_When_Rendered()
    {
        var cut = RenderFor(GameMode.FreePlay);

        var dialog = cut.Find("dialog");
        var heading = cut.Find($"#{dialog.GetAttribute("aria-labelledby")}");
        heading.TextContent.Should().Be("How to play");
    }

    [Fact]
    public void Should_PutTheCloseButtonInADialogForm_When_Rendered()
    {
        var cut = RenderFor(GameMode.FreePlay);

        // A button in a method="dialog" form closes the dialog natively, as Esc does.
        var close = cut.Find("dialog form[method=dialog] button");
        close.GetAttribute("aria-label").Should().Be("Close");
    }

    [Theory]
    [InlineData(GameMode.FreePlay, "Free play", "Free play")]
    [InlineData(GameMode.Daily, "Daily puzzle", "Daily")]
    [InlineData(GameMode.Evil, "Evil Mastermind", "Evil")]
    public void Should_ExplainTheModeOnScreen_When_Opened(GameMode mode, string heading, string button)
    {
        var cut = RenderFor(mode);

        cut.Find(".btn-help").Click();

        cut.Find(".help-mode-title").TextContent.Should().Be(heading);
        cut.Find(".help-modes [aria-pressed=true]").TextContent.Should().Be(button);
    }

    [Fact]
    public void Should_ExplainAnotherMode_When_ItIsChosenInTheDialog()
    {
        var cut = RenderFor(GameMode.FreePlay);
        cut.Find(".btn-help").Click();

        cut.Find(".help-mode-evil").Click();

        cut.Find(".help-mode-title").TextContent.Should().Be("Evil Mastermind");
        cut.Find(".help-mode-evil").GetAttribute("aria-pressed").Should().Be("true");
        cut.Find(".help-mode-free").GetAttribute("aria-pressed").Should().Be("false");
    }

    [Fact]
    public void Should_OpenOnTheModeOnScreenAgain_When_ReopenedAfterBrowsingAnother()
    {
        var cut = RenderFor(GameMode.Daily);
        cut.Find(".btn-help").Click();
        cut.Find(".help-mode-evil").Click();

        cut.Find(".btn-help").Click();

        cut.Find(".help-mode-title").TextContent.Should().Be("Daily puzzle");
    }

    [Fact]
    public void Should_ExplainTheCoach_When_Opened()
    {
        var cut = RenderFor(GameMode.Evil);

        cut.Find(".btn-help").Click();

        cut.Find(".help-coach h3").TextContent.Should().Be("Coach");
        cut.Find(".help-coach").TextContent.Should().Contain("1,296");
    }

    [Fact]
    public void Should_ShowTheExampleFeedbackTheGameWouldGive_When_Rendered()
    {
        var cut = RenderFor(GameMode.FreePlay);

        var code = PegColors(cut.FindAll(".help-example-code .help-peg"));
        var guess = PegColors(cut.FindAll(".help-example-guess .help-peg"));
        var dots = cut.FindAll(".help-example .key-peg-dot")
            .Select(dot => dot.ClassList.Single(name => name != "key-peg-dot"))
            .ToList();

        var response = Response.Score(code, guess);
        dots.Should().HaveCount(4);
        dots.Count(dot => dot == "key-black").Should().Be(response.BlackKeyPegs);
        dots.Count(dot => dot == "key-white").Should().Be(response.WhiteKeyPegs);
        // The text walks through one black, two whites and a color that earns nothing.
        response.Should().Be(new Response(1, 2));
    }

    // "peg--green" back to CodePeg.Green.
    private static CodePeg[] PegColors(IEnumerable<IElement> pegs) =>
        [.. pegs.Select(peg => Enum.Parse<CodePeg>(
            peg.ClassList.Single(name => name.StartsWith("peg--", StringComparison.Ordinal))["peg--".Length..],
            ignoreCase: true))];
}
