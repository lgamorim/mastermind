using System.Linq;
using Bunit;
using FluentAssertions;
using Mastermind.Core;
using Mastermind.WebApp.Pages;
using Mastermind.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class GameTests : BunitContext
{
    private static readonly CodePeg[] SecretCode = [CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Green];

    public GameTests()
    {
        var secretCodeGenerator = Substitute.For<ISecretCodeGenerator>();
        secretCodeGenerator.Generate(Arg.Any<int>()).Returns(SecretCode);
        Services.AddSingleton(secretCodeGenerator);
        Services.AddSingleton<GameStateService>();
    }

    [Fact]
    public void Should_ShowThePlaceholder_When_NoGameHasStarted()
    {
        var cut = Render<Game>();

        cut.FindAll(".board-placeholder").Should().HaveCount(1);
        cut.FindAll(".board").Should().BeEmpty();
    }

    [Fact]
    public void Should_RenderEveryBoardRow_When_ANewGameStarts()
    {
        var cut = Render<Game>();

        cut.Find(".btn-new-game").Click();

        cut.FindAll(".board-row").Should().HaveCount(10);
        cut.FindAll(".drop-zone").Should().HaveCount(4);
    }

    [Fact]
    public void Should_FillTheSlot_When_APaletteColorIsDropped()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        cut.FindAll(".palette-peg")[0].DragStart();
        cut.FindAll(".drop-zone")[0].Drop();

        cut.FindAll(".drop-zone")[0].ClassList.Should().Contain("peg--red");
    }

    [Fact]
    public void Should_KeepTheHighlight_When_TheDragMovesOnToTheNextSlot()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].DragStart();

        // Browsers fire dragenter on the slot being entered before dragleave on
        // the one being left, so the stale leave must not clear the new slot.
        cut.FindAll(".drop-zone")[0].DragEnter();
        cut.FindAll(".drop-zone")[1].DragEnter();
        cut.FindAll(".drop-zone")[0].DragLeave();

        cut.FindAll(".drop-zone")[1].ClassList.Should().Contain("drag-over");
    }

    [Fact]
    public void Should_DropTheHighlight_When_TheDragLeavesTheHighlightedSlot()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].DragStart();
        cut.FindAll(".drop-zone")[0].DragEnter();

        cut.FindAll(".drop-zone")[0].DragLeave();

        cut.FindAll(".drop-zone")[0].ClassList.Should().NotContain("drag-over");
    }

    [Fact]
    public void Should_ClearTheSlot_When_AFilledSlotIsClicked()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].DragStart();
        cut.FindAll(".drop-zone")[0].Drop();

        cut.FindAll(".drop-zone")[0].Click();

        cut.FindAll(".drop-zone")[0].ClassList.Should().NotContain("peg--red");
    }

    [Fact]
    public void Should_RenderSlotsAndPaletteAsButtons_When_AGameIsInProgress()
    {
        var cut = Render<Game>();

        cut.Find(".btn-new-game").Click();

        // Buttons are focusable and activate on Enter/Space, so the board stays
        // operable without a mouse -- and without a drag-capable pointer.
        cut.FindAll(".drop-zone").Should().OnlyContain(slot => slot.NodeName == "BUTTON");
        cut.FindAll(".palette-peg").Should().OnlyContain(peg => peg.NodeName == "BUTTON");
    }

    [Fact]
    public void Should_FillTheSlot_When_AColorIsSelectedAndTheSlotIsTapped()
    {
        // Touch browsers never emit HTML5 drag events, so tapping has to be a
        // complete path from palette to board on its own.
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        cut.FindAll(".palette-peg")[0].Click();
        cut.FindAll(".drop-zone")[0].Click();

        cut.FindAll(".drop-zone")[0].ClassList.Should().Contain("peg--red");
    }

    [Fact]
    public void Should_KeepTheColorSelected_When_FillingEverySlotByTapping()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        cut.FindAll(".palette-peg")[0].Click();
        for (var slot = 0; slot < SecretCode.Length; slot++)
        {
            cut.FindAll(".drop-zone")[slot].Click();
        }

        cut.Find(".btn-submit").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Should_MarkThePaletteColorAsPressed_When_ItIsSelected()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        cut.FindAll(".palette-peg")[0].Click();

        cut.FindAll(".palette-peg")[0].GetAttribute("aria-pressed").Should().Be("true");
        cut.FindAll(".palette-peg")[1].GetAttribute("aria-pressed").Should().Be("false");
    }

    [Fact]
    public void Should_DeselectTheColor_When_TheSelectedPaletteColorIsTappedAgain()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].Click();

        cut.FindAll(".palette-peg")[0].Click();

        // Deselecting puts tapping back into clear-the-slot mode.
        cut.FindAll(".palette-peg")[0].GetAttribute("aria-pressed").Should().Be("false");
    }

    [Fact]
    public void Should_ReplaceTheColor_When_AFilledSlotIsTappedWithAColorSelected()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].Click();
        cut.FindAll(".drop-zone")[0].Click();

        cut.FindAll(".palette-peg")[1].Click();
        cut.FindAll(".drop-zone")[0].Click();

        cut.FindAll(".drop-zone")[0].ClassList.Should().Contain("peg--blue");
        cut.FindAll(".drop-zone")[0].ClassList.Should().NotContain("peg--red");
    }

    [Fact]
    public void Should_LabelEachSlotWithItsContents_When_ThePegsAreRead()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].Click();

        cut.FindAll(".drop-zone")[0].Click();

        cut.FindAll(".drop-zone")[0].GetAttribute("aria-label").Should().Contain("Red");
        cut.FindAll(".drop-zone")[1].GetAttribute("aria-label").Should().Contain("empty");
    }

    [Fact]
    public void Should_DisableSubmit_When_NotEverySlotIsFilled()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        cut.FindAll(".palette-peg")[0].DragStart();
        cut.FindAll(".drop-zone")[0].Drop();

        cut.Find(".btn-submit").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Should_EnableSubmit_When_EverySlotIsFilled()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        cut.FindAll(".palette-peg")[0].DragStart();
        for (var slot = 0; slot < SecretCode.Length; slot++)
        {
            cut.FindAll(".drop-zone")[slot].Drop();
        }

        cut.Find(".btn-submit").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Should_ReportAnErrorInsteadOfThrowing_When_SubmitRunsWithAnEmptySlot()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].DragStart();
        cut.FindAll(".drop-zone")[0].Drop(); // only one of the four slots is filled

        cut.Find(".btn-submit").Click();

        cut.Find(".submit-error").TextContent.Should().NotBeNullOrWhiteSpace();
        cut.FindAll(".board-row.played").Should().BeEmpty();
    }

    [Fact]
    public void Should_MarkTheSubmitErrorAsAnAlert_When_ItIsShown()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].DragStart();
        cut.FindAll(".drop-zone")[0].Drop();

        cut.Find(".btn-submit").Click();

        // role="alert" makes a screen reader announce the message when it
        // appears; sighted users already see it next to the button.
        cut.Find(".submit-error").GetAttribute("role").Should().Be("alert");
    }

    [Fact]
    public void Should_ClearTheSubmitError_When_AColorIsDroppedOnTheBoard()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].DragStart();
        cut.FindAll(".drop-zone")[0].Drop();
        cut.Find(".btn-submit").Click(); // "fill every hole" error appears

        cut.FindAll(".drop-zone")[1].Drop();

        // The player is fixing the board, so the stale message must not sit
        // next to a Submit that may already be enabled again.
        cut.FindAll(".submit-error").Should().BeEmpty();
    }

    [Fact]
    public void Should_ClearTheSubmitError_When_ASlotIsTapped()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].Click();
        cut.FindAll(".drop-zone")[0].Click();
        cut.Find(".btn-submit").Click(); // "fill every hole" error appears

        cut.FindAll(".drop-zone")[1].Click();

        cut.FindAll(".submit-error").Should().BeEmpty();
    }

    [Fact]
    public void Should_DeselectThePaletteColor_When_AGuessIsSubmitted()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].Click();
        for (var slot = 0; slot < SecretCode.Length; slot++)
        {
            cut.FindAll(".drop-zone")[slot].Click();
        }

        cut.Find(".btn-submit").Click();

        // Submit ends the guess, so the next row starts with a clean slate: no
        // armed color silently turning "tap a hole" into "place last row's color".
        cut.FindAll(".palette-peg")[0].GetAttribute("aria-pressed").Should().Be("false");
        cut.FindAll(".drop-zone")[0].Click();
        cut.FindAll(".drop-zone")[0].ClassList.Should().NotContain("peg--red");
    }

    [Fact]
    public void Should_MarkTheRowPlayedAndOfferTheNext_When_AGuessIsSubmitted()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].DragStart();
        for (var slot = 0; slot < SecretCode.Length; slot++)
        {
            cut.FindAll(".drop-zone")[slot].Drop();
        }

        cut.Find(".btn-submit").Click();

        cut.FindAll(".board-row.played").Should().HaveCount(1);
        // The next row takes over as the active one, so its slots are droppable and empty again.
        cut.FindAll(".drop-zone").Should().HaveCount(4);
        cut.FindAll(".drop-zone").Should().OnlyContain(slot => !slot.ClassList.Contains("peg--red"));
    }

    [Fact]
    public void Should_OrderKeyPegs_BlackThenWhiteThenEmpty_When_AGuessIsScored()
    {
        // Secret is Red, Blue, Yellow, Green. Guessing Red, Red, Blue, Blue puts Red in the right
        // place once and Blue in the wrong place once — one black peg, one white, two empty.
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        cut.FindAll(".palette-peg")[0].DragStart();
        cut.FindAll(".drop-zone")[0].Drop();
        cut.FindAll(".drop-zone")[1].Drop();
        cut.FindAll(".palette-peg")[1].DragStart();
        cut.FindAll(".drop-zone")[2].Drop();
        cut.FindAll(".drop-zone")[3].Drop();
        cut.Find(".btn-submit").Click();

        var dots = cut.FindAll(".board-row.played .key-peg-dot")
            .Select(dot => dot.ClassList.Single(name => name != "key-peg-dot"));
        dots.Should().Equal("key-black", "key-white", "key-empty", "key-empty");
    }

    [Fact]
    public void Should_RevealTheSecretCode_When_DebugIsRequested()
    {
        // Debug arrives as a query parameter, so it is set by navigating rather than by passing
        // the parameter directly.
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("debug", true));
        var cut = Render<Game>();

        cut.Find(".btn-new-game").Click();

        var shield = cut.FindAll(".shield-hole")
            .Select(hole => hole.ClassList.Single(name => name != "shield-hole"));
        shield.Should().Equal("peg--red", "peg--blue", "peg--yellow", "peg--green");
    }

    [Fact]
    public void Should_HideTheSecretCode_When_DebugIsNotRequested()
    {
        var cut = Render<Game>();

        cut.Find(".btn-new-game").Click();

        cut.FindAll(".shield-hole").Should().HaveCount(4);
        cut.FindAll(".shield-hole").Should().OnlyContain(hole => hole.ClassList.Count() == 1);
    }
}
