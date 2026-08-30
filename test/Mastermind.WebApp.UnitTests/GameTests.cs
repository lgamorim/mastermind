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
