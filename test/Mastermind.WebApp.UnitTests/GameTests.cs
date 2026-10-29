using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Mastermind.Core;
using Mastermind.WebApp.Pages;
using Mastermind.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using Xunit;
using Game = Mastermind.WebApp.Pages.Game;

namespace Mastermind.WebApp.UnitTests;

public class GameTests : BunitContext
{
    private const string SavedGameKey = "mastermind.savedGame.v1";
    private const string StatsKey = "mastermind.stats.v1";
    private const string DailyKey = "mastermind.daily.v1";

    // 3 November 2026: daily puzzle #9, counting 26 October as #1.
    private static readonly DateOnly Today = new(2026, 11, 3);

    private static readonly CodePeg[] SecretCode = [CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Green];

    // Production by default, as the published site runs: a static host sends no
    // Blazor-Environment header, so WebAssembly falls back to Production.
    private readonly FakeHostEnvironment _hostEnvironment = new() { Environment = "Production" };

    private readonly FixedTimeProvider _clock = new()
    {
        UtcNow = new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero),
        Zone = TimeZoneInfo.Utc,
    };

    public GameTests()
    {
        var secretCodeGenerator = Substitute.For<ISecretCodeGenerator>();
        secretCodeGenerator.Generate(Arg.Any<int>()).Returns(SecretCode);
        Services.AddSingleton(secretCodeGenerator);
        Services.AddSingleton<GameStateService>();
        Services.AddSingleton<IWebAssemblyHostEnvironment>(_hostEnvironment);
        Services.AddSingleton<GameStorage>();
        Services.AddSingleton<TimeProvider>(_clock);

        // Loose: unplanned localStorage calls succeed with defaults, so getItem
        // finds no save and setItem is accepted. Persistence tests set up the
        // calls they care about explicitly.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Should_OpenABoardReadyToPlay_When_ThePageFirstRenders()
    {
        var cut = Render<Game>();

        cut.FindAll(".board-row").Should().HaveCount(10);
        cut.FindAll(".board-row.played").Should().BeEmpty();
        cut.FindAll(".drop-zone").Should().HaveCount(4);
        cut.Find(".mode-free").GetAttribute("aria-pressed").Should().Be("true");
    }

    [Fact]
    public void Should_NotSaveTheFirstGame_When_ThePageOpensWithNoSavedGame()
    {
        var cut = Render<Game>();

        // Saving the board the page opened with would overwrite a save still loading.
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("localStorage.getItem", calledTimes: 4));
        JSInterop.Invocations["localStorage.setItem"].Should().BeEmpty();
    }

    [Fact]
    public async Task Should_KeepTheFirstGame_When_TheSavedGameArrivesAfterAGuessInIt()
    {
        var pendingLoad = JSInterop.Setup<string?>("localStorage.getItem", SavedGameKey);
        var cut = Render<Game>();
        PlayGuess(cut, [CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White]);

        pendingLoad.SetResult("""{"secretCode":["Red","Blue","Yellow","Green"],"guesses":[["Red","Yellow","White","White"],["Red","Red","Red","Red"]],"revealSecretFromStart":false}""");
        // Queue behind the page's resumed load, as in the New Game race test.
        await cut.InvokeAsync(() => { });

        cut.FindAll(".board-row.played").Should().ContainSingle();
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
    public void Should_RevealTheSecretCode_When_DebugIsRequestedInDevelopment()
    {
        _hostEnvironment.Environment = "Development";
        RequestDebugMode();
        var cut = Render<Game>();

        cut.Find(".btn-new-game").Click();

        var shield = cut.FindAll(".shield-hole")
            .Select(hole => hole.ClassList.Single(name => name != "shield-hole"));
        shield.Should().Equal("peg--red", "peg--blue", "peg--yellow", "peg--green");
    }

    [Fact]
    public void Should_RevealTheFirstGamesSecretCode_When_DebugIsRequestedInDevelopment()
    {
        _hostEnvironment.Environment = "Development";
        RequestDebugMode();

        var cut = Render<Game>();

        var shield = cut.FindAll(".shield-hole")
            .Select(hole => hole.ClassList.Single(name => name != "shield-hole"));
        shield.Should().Equal("peg--red", "peg--blue", "peg--yellow", "peg--green");
    }

    [Fact]
    public void Should_HideTheFirstGamesSecretCode_When_DebugIsRequestedInProduction()
    {
        RequestDebugMode();

        var cut = Render<Game>();

        cut.FindAll(".shield-hole").Should().HaveCount(4);
        cut.FindAll(".shield-hole").Should().OnlyContain(hole => hole.ClassList.Count() == 1);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Should_HideTheSecretCode_When_DebugIsRequestedOutsideDevelopment(string environment)
    {
        _hostEnvironment.Environment = environment;
        RequestDebugMode();
        var cut = Render<Game>();

        cut.Find(".btn-new-game").Click();

        cut.FindAll(".shield-hole").Should().HaveCount(4);
        cut.FindAll(".shield-hole").Should().OnlyContain(hole => hole.ClassList.Count() == 1);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public void Should_HideTheSecretCode_When_DebugIsNotRequested(string environment)
    {
        _hostEnvironment.Environment = environment;
        var cut = Render<Game>();

        cut.Find(".btn-new-game").Click();

        cut.FindAll(".shield-hole").Should().HaveCount(4);
        cut.FindAll(".shield-hole").Should().OnlyContain(hole => hole.ClassList.Count() == 1);
    }

    [Fact]
    public void Should_RestoreTheSavedGame_When_ThePageFirstRenders()
    {
        JSInterop.Setup<string?>("localStorage.getItem", SavedGameKey)
            .SetResult("""{"secretCode":["Red","Blue","Yellow","Green"],"guesses":[["Red","Yellow","White","White"]],"revealSecretFromStart":false}""");

        var cut = Render<Game>();

        cut.WaitForAssertion(() => cut.FindAll(".board-row.played").Should().HaveCount(1));
        cut.FindAll(".board-row.played .peg-hole").Select(peg => peg.ClassList.Last())
            .Should().Equal("peg--red", "peg--yellow", "peg--white", "peg--white");
        cut.FindAll(".drop-zone").Should().HaveCount(4);
    }

    [Fact]
    public void Should_KeepTheFirstGame_When_TheSavedGameIsInvalid()
    {
        // Two guesses of the secret: the game ended at the first, so the save cannot be replayed.
        JSInterop.Setup<string?>("localStorage.getItem", SavedGameKey)
            .SetResult("""{"secretCode":["Red","Blue","Yellow","Green"],"guesses":[["Red","Blue","Yellow","Green"],["Red","Blue","Yellow","Green"]],"revealSecretFromStart":false}""");

        var cut = Render<Game>();

        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("localStorage.getItem", calledTimes: 4));
        cut.FindAll(".board-row.played").Should().BeEmpty();
        cut.FindAll(".drop-zone").Should().HaveCount(4);
    }

    [Fact]
    public void Should_KeepTheFirstGame_When_TheSavedGameHasANullGuess()
    {
        JSInterop.Setup<string?>("localStorage.getItem", SavedGameKey)
            .SetResult("""{"secretCode":["Red","Blue","Yellow","Green"],"guesses":[null],"revealSecretFromStart":false}""");

        var cut = Render<Game>();

        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("localStorage.getItem", calledTimes: 4));
        cut.FindAll(".board-row.played").Should().BeEmpty();
        cut.FindAll(".drop-zone").Should().HaveCount(4);
    }

    [Fact]
    public async Task Should_KeepTheNewGame_When_TheSavedGameArrivesAfterItStarted()
    {
        var pendingLoad = JSInterop.Setup<string?>("localStorage.getItem", SavedGameKey);
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        pendingLoad.SetResult("""{"secretCode":["Red","Blue","Yellow","Green"],"guesses":[["Red","Yellow","White","White"]],"revealSecretFromStart":false}""");
        // The page resumes after the load on the renderer's dispatcher; queue behind
        // it so the assertion sees the outcome rather than racing it.
        await cut.InvokeAsync(() => { });

        cut.FindAll(".board-row.played").Should().BeEmpty();
    }

    [Fact]
    public void Should_SaveTheGame_When_ANewGameStarts()
    {
        var cut = Render<Game>();

        cut.Find(".btn-new-game").Click();

        var save = JSInterop.VerifyInvoke("localStorage.setItem");
        save.Arguments[0].Should().Be(SavedGameKey);
        save.Arguments[1].Should().Be("""{"secretCode":["Red","Blue","Yellow","Green"],"guesses":[],"revealSecretFromStart":false}""");
    }

    [Fact]
    public void Should_SaveTheGuess_When_AGuessIsSubmitted()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.FindAll(".palette-peg")[0].DragStart();
        for (var slot = 0; slot < SecretCode.Length; slot++)
        {
            cut.FindAll(".drop-zone")[slot].Drop();
        }

        cut.Find(".btn-submit").Click();

        var saves = JSInterop.Invocations["localStorage.setItem"];
        saves.Should().HaveCount(2);
        saves[^1].Arguments[1].Should().Be("""{"secretCode":["Red","Blue","Yellow","Green"],"guesses":[["Red","Red","Red","Red"]],"revealSecretFromStart":false}""");
    }

    [Fact]
    public void Should_NotSave_When_AGuessIsRejected()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        cut.Find(".btn-submit").Click(); // empty holes: "fill every hole" error, nothing played

        JSInterop.Invocations["localStorage.setItem"].Should().HaveCount(1);
    }

    [Fact]
    public void Should_ShowTheStatsPanel_When_ThePageFirstRenders()
    {
        var cut = Render<Game>();

        cut.FindAll(".stats").Should().HaveCount(1);
        PlayedTile(cut).Should().Be("0");
    }

    [Fact]
    public void Should_CountTheWinAndHighlightItsGuessCount_When_TheCodeIsCracked()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        PlayGuess(cut, SecretCode);

        PlayedTile(cut).Should().Be("1");
        cut.Find(".distribution-row.highlighted").TextContent.Should().Contain("1 guess");
    }

    [Fact]
    public void Should_SaveTheStats_When_AGameEnds()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        PlayGuess(cut, SecretCode);

        var statsSave = JSInterop.Invocations["localStorage.setItem"].Single(call => (string?)call.Arguments[0] == StatsKey);
        statsSave.Arguments[1].Should().Be("""{"gamesPlayed":1,"wins":1,"currentStreak":1,"maxStreak":1,"winsByGuessCount":[1,0,0,0,0,0,0,0,0,0]}""");
    }

    [Fact]
    public void Should_SaveALoss_When_ANewGameAbandonsOneInProgress()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        PlayGuess(cut, [CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White]);

        cut.Find(".btn-new-game").Click();

        var statsSave = JSInterop.Invocations["localStorage.setItem"].Single(call => (string?)call.Arguments[0] == StatsKey);
        statsSave.Arguments[1].Should().Be("""{"gamesPlayed":1,"wins":0,"currentStreak":0,"maxStreak":0,"winsByGuessCount":[0,0,0,0,0,0,0,0,0,0]}""");
        PlayedTile(cut).Should().Be("1");
    }

    [Fact]
    public void Should_NotSaveTheStats_When_AGuessLeavesTheGameInProgress()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        PlayGuess(cut, [CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White]);

        JSInterop.Invocations["localStorage.setItem"].Should().NotContain(call => (string?)call.Arguments[0] == StatsKey);
    }

    [Fact]
    public void Should_ShowTheStoredStats_When_ThePageFirstRenders()
    {
        JSInterop.Setup<string?>("localStorage.getItem", StatsKey)
            .SetResult("""{"gamesPlayed":7,"wins":5,"currentStreak":2,"maxStreak":3,"winsByGuessCount":[0,1,2,1,1,0,0,0,0,0]}""");

        var cut = Render<Game>();

        cut.WaitForAssertion(() => PlayedTile(cut).Should().Be("7"));
    }

    [Fact]
    public async Task Should_KeepTheResultJustPlayed_When_TheStoredStatsArriveAfterIt()
    {
        var pendingLoad = JSInterop.Setup<string?>("localStorage.getItem", StatsKey);
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        PlayGuess(cut, SecretCode);

        pendingLoad.SetResult("""{"gamesPlayed":7,"wins":5,"currentStreak":2,"maxStreak":3,"winsByGuessCount":[0,1,2,1,1,0,0,0,0,0]}""");
        // The page resumes after the load on the renderer's dispatcher; queue behind
        // it so the assertion sees the outcome rather than racing it.
        await cut.InvokeAsync(() => { });

        PlayedTile(cut).Should().Be("1");
    }

    [Fact]
    public void Should_IgnoreTheStoredStats_When_TheyAreInconsistent()
    {
        // Five wins out of two games cannot have come from playing.
        JSInterop.Setup<string?>("localStorage.getItem", StatsKey)
            .SetResult("""{"gamesPlayed":2,"wins":5,"currentStreak":0,"maxStreak":0,"winsByGuessCount":[5,0,0,0,0,0,0,0,0,0]}""");

        var cut = Render<Game>();

        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("localStorage.getItem", calledTimes: 4));
        PlayedTile(cut).Should().Be("0");
    }

    [Fact]
    public void Should_KeepTheCoachOff_When_AGameStarts()
    {
        var cut = Render<Game>();

        cut.Find(".btn-new-game").Click();

        cut.Find(".coach-toggle input").HasAttribute("checked").Should().BeFalse();
        cut.FindAll(".coach").Should().BeEmpty();
    }

    [Fact]
    public void Should_ShowTheCoach_When_ItIsSwitchedOn()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();

        cut.Find(".coach-toggle input").Change(true);

        cut.Find(".coach-count").TextContent.Should().Be("1,296 codes are possible.");
    }

    [Fact]
    public void Should_FlagTheRowAndExplain_When_ACoachedGuessContradictsEarlierFeedback()
    {
        // Secret is Red Blue Yellow Green: four Reds score one black, so four Blues cannot be the code.
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.Find(".coach-toggle input").Change(true);
        PlayGuess(cut, [CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red]);

        PlayGuess(cut, [CodePeg.Blue, CodePeg.Blue, CodePeg.Blue, CodePeg.Blue]);

        var flagged = cut.FindAll(".board-row.contradicted");
        flagged.Should().ContainSingle();
        // Read into a variable first: a missing row number yields null, which fails the
        // assertion instead of skipping it the way ?.Should() would.
        var rowNumber = flagged[0].QuerySelector(".row-number")?.TextContent;
        rowNumber.Should().Contain("2").And.Contain("contradicts guess 1");
        cut.Find(".coach-warning").TextContent.Should().Contain("Guess 2 couldn't have been the code");
    }

    [Fact]
    public void Should_FlagNothing_When_TheCoachIsOff()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        PlayGuess(cut, [CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red]);

        PlayGuess(cut, [CodePeg.Blue, CodePeg.Blue, CodePeg.Blue, CodePeg.Blue]);

        cut.FindAll(".board-row.contradicted").Should().BeEmpty();
        cut.FindAll(".coach").Should().BeEmpty();
    }

    [Fact]
    public void Should_HideTheCoach_When_ItIsSwitchedOffAgain()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        cut.Find(".coach-toggle input").Change(true);

        cut.Find(".coach-toggle input").Change(false);

        cut.FindAll(".coach").Should().BeEmpty();
    }

    private static string? PlayedTile(IRenderedComponent<Game> cut) =>
        cut.FindAll(".stat").Single(tile => tile.QuerySelector("dt")?.TextContent == "Played").QuerySelector("dd")?.TextContent;

    // Fills the active row by tapping a palette color and then each hole, and submits it.
    private static void PlayGuess(IRenderedComponent<Game> cut, CodePeg[] guess)
    {
        for (var slot = 0; slot < guess.Length; slot++)
        {
            var palette = cut.FindAll(".palette-peg")[(int)guess[slot]];
            if (palette.GetAttribute("aria-pressed") != "true") palette.Click();
            cut.FindAll(".drop-zone")[slot].Click();
        }

        cut.Find(".btn-submit").Click();
    }

    private IRenderedComponent<Game> RenderAndOpenTheDailyPuzzle()
    {
        var cut = Render<Game>();
        cut.Find(".mode-daily").Click();
        return cut;
    }

    [Fact]
    public void Should_OfferTodaysDailyPuzzleByNumber_When_ThePageRenders()
    {
        var cut = Render<Game>();

        cut.Find(".mode-daily").TextContent.Should().Be("Daily #9");
        cut.Find(".mode-free").GetAttribute("aria-pressed").Should().Be("true");
        cut.Find(".mode-daily").GetAttribute("aria-pressed").Should().Be("false");
    }

    [Fact]
    public void Should_FollowThePlayersLocalDate_When_UtcHasAlreadyMovedOn()
    {
        // 03:00 UTC on 4 November is still 22:00 on 3 November five hours west of UTC.
        _clock.UtcNow = new DateTimeOffset(2026, 11, 4, 3, 0, 0, TimeSpan.Zero);
        _clock.Zone = TimeZoneInfo.CreateCustomTimeZone("UTC-5", TimeSpan.FromHours(-5), "UTC-5", "UTC-5");

        var cut = Render<Game>();

        cut.Find(".mode-daily").TextContent.Should().Be("Daily #9");
    }

    [Fact]
    public void Should_LeaveOutTheDailyPuzzle_When_TheClockIsBeforeTheFirstPuzzle()
    {
        _clock.UtcNow = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var cut = Render<Game>();

        cut.FindAll(".mode-daily").Should().BeEmpty();
    }

    [Fact]
    public void Should_ShowTheDailyBoardAndHideTheStats_When_TheDailyPuzzleIsOpened()
    {
        var cut = RenderAndOpenTheDailyPuzzle();

        cut.FindAll(".board-row").Should().HaveCount(10);
        cut.Find(".mode-daily").GetAttribute("aria-pressed").Should().Be("true");
        cut.Find(".stats-area").HasAttribute("hidden").Should().BeTrue();
    }

    [Fact]
    public void Should_AnnounceTheDailyWin_When_TheDaysCodeIsCracked()
    {
        var cut = RenderAndOpenTheDailyPuzzle();

        PlayGuess(cut, DailyPuzzle.SecretCodeFor(Today));

        cut.Find(".result-banner").TextContent.Trim().Should().Be("You cracked Daily #9 in 1 guess!");
    }

    [Fact]
    public void Should_AnnounceTheDailyLoss_When_EveryRowIsUsed()
    {
        var code = DailyPuzzle.SecretCodeFor(Today);
        var missing = Enum.GetValues<CodePeg>().First(color => !code.Contains(color));
        var cut = RenderAndOpenTheDailyPuzzle();

        for (var row = 0; row < 10; row++)
        {
            PlayGuess(cut, [missing, missing, missing, missing]);
        }

        cut.Find(".result-banner").TextContent.Trim().Should().Be("Daily #9 kept its secret. A new puzzle starts at midnight.");
    }

    [Fact]
    public void Should_NotOfferToShare_When_TheDailyPuzzleIsInProgress()
    {
        var cut = RenderAndOpenTheDailyPuzzle();

        cut.FindAll(".btn-share").Should().BeEmpty();
    }

    [Fact]
    public void Should_CopyTheShareText_When_ShareIsClicked()
    {
        JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true).SetVoidResult();
        var cut = RenderAndOpenTheDailyPuzzle();
        PlayGuess(cut, DailyPuzzle.SecretCodeFor(Today));

        cut.Find(".btn-share").Click();

        var copied = (string?)JSInterop.VerifyInvoke("navigator.clipboard.writeText").Arguments[0];
        copied.Should().StartWith("Mastermind Daily #9 1/10\n\n🟩🟩🟩🟩");
        cut.Find(".share-status").TextContent.Should().Be("Copied to your clipboard.");
        cut.FindAll(".share-text").Should().BeEmpty();
    }

    [Fact]
    public void Should_ShowTheTextToCopyByHand_When_TheClipboardRefuses()
    {
        JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true).SetException(new JSException("Clipboard blocked."));
        var cut = RenderAndOpenTheDailyPuzzle();
        PlayGuess(cut, DailyPuzzle.SecretCodeFor(Today));

        cut.Find(".btn-share").Click();

        cut.Find(".share-status").TextContent.Should().Contain("copy it from the box below");
        cut.Find(".share-text").TextContent.Should().StartWith("Mastermind Daily #9 1/10");
    }

    [Fact]
    public void Should_SaveTheDailyGameUnderItsOwnKey_When_ADailyGuessIsSubmitted()
    {
        var cut = RenderAndOpenTheDailyPuzzle();

        PlayGuess(cut, [CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White]);

        var saves = JSInterop.Invocations["localStorage.setItem"];
        saves.Should().ContainSingle(call => (string?)call.Arguments[0] == DailyKey)
            .Which.Arguments[1].Should().Be("""{"date":"2026-11-03","guesses":[["White","White","White","White"]]}""");
        saves.Should().NotContain(call => (string?)call.Arguments[0] == SavedGameKey);
    }

    [Fact]
    public void Should_KeepTheFreePlayGame_When_SwitchingToTheDailyPuzzleAndBack()
    {
        var cut = Render<Game>();
        cut.Find(".btn-new-game").Click();
        PlayGuess(cut, [CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White]);
        cut.Find(".mode-daily").Click();

        cut.Find(".mode-free").Click();

        cut.FindAll(".board-row.played").Should().ContainSingle();
        cut.Find(".stats-area").HasAttribute("hidden").Should().BeFalse();
    }

    private void MoveTheClockToTheNextDay() =>
        _clock.UtcNow = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Should_AnnounceTheNewPuzzle_When_MidnightPassesWithYesterdaysPuzzleOpen()
    {
        var cut = RenderAndOpenTheDailyPuzzle();
        MoveTheClockToTheNextDay();

        cut.Render();

        cut.Find(".daily-new").TextContent.Should().Contain("A new puzzle is out: Daily #10");
        cut.Find(".mode-daily").TextContent.Should().Be("Daily #10");
        cut.Find(".mode-daily").GetAttribute("aria-pressed").Should().Be("false");
    }

    [Fact]
    public void Should_LetYesterdaysPuzzleBeFinished_When_MidnightPassesMidGame()
    {
        var cut = RenderAndOpenTheDailyPuzzle();
        MoveTheClockToTheNextDay();

        PlayGuess(cut, DailyPuzzle.SecretCodeFor(Today));

        cut.Find(".result-banner").TextContent.Trim().Should().Be("You cracked Daily #9 in 1 guess!");
    }

    [Fact]
    public void Should_NotSaveYesterdaysPuzzle_When_ItIsPlayedAfterMidnight()
    {
        var cut = RenderAndOpenTheDailyPuzzle();
        MoveTheClockToTheNextDay();

        PlayGuess(cut, [CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White]);

        JSInterop.Invocations["localStorage.setItem"].Should().NotContain(call => (string?)call.Arguments[0] == DailyKey);
    }

    [Fact]
    public void Should_OpenTheNewDaysPuzzle_When_TheDailyButtonIsClickedAfterMidnight()
    {
        var cut = RenderAndOpenTheDailyPuzzle();
        PlayGuess(cut, [CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White]);
        MoveTheClockToTheNextDay();

        cut.Find(".mode-daily").Click();

        cut.FindAll(".board-row.played").Should().BeEmpty();
        cut.FindAll(".daily-new").Should().BeEmpty();
        cut.Find(".mode-daily").GetAttribute("aria-pressed").Should().Be("true");
    }

    private const string EvilKey = "mastermind.evil.v1";

    // Always guessing the first code that still fits beats the evil code maker in 8 guesses.
    private static readonly CodePeg[][] FirstFitEvilWin =
    [
        [CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red],
        [CodePeg.Blue, CodePeg.Blue, CodePeg.Blue, CodePeg.Blue],
        [CodePeg.Yellow, CodePeg.Yellow, CodePeg.Yellow, CodePeg.Yellow],
        [CodePeg.Yellow, CodePeg.Green, CodePeg.Green, CodePeg.Green],
        [CodePeg.White, CodePeg.Yellow, CodePeg.White, CodePeg.White],
        [CodePeg.White, CodePeg.Black, CodePeg.Yellow, CodePeg.Black],
        [CodePeg.White, CodePeg.Black, CodePeg.Black, CodePeg.Yellow],
        [CodePeg.Black, CodePeg.Black, CodePeg.Yellow, CodePeg.White],
    ];

    private IRenderedComponent<Game> RenderAndOpenEvilMode()
    {
        var cut = Render<Game>();
        cut.Find(".mode-evil").Click();
        return cut;
    }

    [Fact]
    public void Should_ExplainFreePlay_When_HowToPlayIsOpenedOnArrival()
    {
        var cut = Render<Game>();

        cut.Find(".btn-help").Click();

        cut.Find(".help-mode-title").TextContent.Should().Be("Free play");
    }

    [Fact]
    public void Should_ExplainTheModeOnScreen_When_HowToPlayIsOpenedInAnotherMode()
    {
        var cut = RenderAndOpenEvilMode();

        cut.Find(".btn-help").Click();

        cut.Find(".help-mode-title").TextContent.Should().Be("Evil Mastermind");
    }

    [Fact]
    public void Should_ShowTheEvilBoardAndHideTheStats_When_EvilModeIsOpened()
    {
        var cut = RenderAndOpenEvilMode();

        cut.FindAll(".board-row").Should().HaveCount(10);
        cut.Find(".mode-evil").GetAttribute("aria-pressed").Should().Be("true");
        cut.Find(".mode-free").GetAttribute("aria-pressed").Should().Be("false");
        cut.Find(".stats-area").HasAttribute("hidden").Should().BeTrue();
        cut.FindAll(".shield-hole").Should().OnlyContain(hole => hole.ClassList.Count() == 1);
    }

    [Fact]
    public void Should_AnswerAsTheEvilCodeMaker_When_AGuessIsPlayedInEvilMode()
    {
        // The fixed secret Red Blue Yellow Green would score this one black and one white.
        var cut = RenderAndOpenEvilMode();

        PlayGuess(cut, [CodePeg.Red, CodePeg.Red, CodePeg.Blue, CodePeg.Blue]);

        cut.FindAll(".board-row.played .key-peg-dot").Should().OnlyContain(dot => dot.ClassList.Contains("key-empty"));
    }

    [Fact]
    public void Should_AnnounceTheWinAndOfferAnotherGame_When_TheEvilCodeMakerIsBeaten()
    {
        var cut = RenderAndOpenEvilMode();

        foreach (var guess in FirstFitEvilWin)
        {
            PlayGuess(cut, guess);
        }

        cut.Find(".result-banner").TextContent.Trim().Should().Be("You beat the Evil Code Maker in 8 guesses!");
        cut.FindAll(".btn-try-again").Should().ContainSingle();
    }

    [Fact]
    public void Should_ShowTheCodeItSettledOn_When_TheEvilCodeMakerWins()
    {
        var cut = RenderAndOpenEvilMode();

        for (var row = 0; row < 10; row++)
        {
            PlayGuess(cut, [CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red]);
        }

        cut.Find(".result-banner").TextContent.Trim().Should().Be("The Evil Code Maker got away. It settled on the code shown above.");
        cut.FindAll(".shield-hole").Select(hole => hole.ClassList.Last()).Should().OnlyContain(name => name == "peg--blue");
    }

    [Fact]
    public void Should_StartAFreshEvilGame_When_TryAgainIsClicked()
    {
        var cut = RenderAndOpenEvilMode();
        foreach (var guess in FirstFitEvilWin)
        {
            PlayGuess(cut, guess);
        }

        cut.Find(".btn-try-again").Click();

        cut.FindAll(".board-row.played").Should().BeEmpty();
        cut.FindAll(".result-banner").Should().BeEmpty();
    }

    [Fact]
    public void Should_SaveTheFreshEvilGame_When_TryAgainIsClicked()
    {
        var cut = RenderAndOpenEvilMode();
        foreach (var guess in FirstFitEvilWin)
        {
            PlayGuess(cut, guess);
        }

        cut.Find(".btn-try-again").Click();

        var evilSaves = JSInterop.Invocations["localStorage.setItem"].Where(call => (string?)call.Arguments[0] == EvilKey).ToList();
        evilSaves[^1].Arguments[1].Should().Be("""{"guesses":[]}""");
    }

    [Fact]
    public async Task Should_KeepTheNewEvilGame_When_TheSavedOneArrivesAfterItStarted()
    {
        var pendingLoad = JSInterop.Setup<string?>("localStorage.getItem", EvilKey);
        var cut = Render<Game>();
        cut.Find(".mode-evil").Click();
        PlayGuess(cut, [CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red]);

        pendingLoad.SetResult("""{"guesses":[["Blue","Blue","Blue","Blue"],["Green","Green","Green","Green"]]}""");
        // The page resumes after the load on the renderer's dispatcher; queue behind
        // it so the assertion sees the outcome rather than racing it.
        await cut.InvokeAsync(() => { });

        cut.FindAll(".board-row.played").Should().ContainSingle();
    }

    [Fact]
    public void Should_NotOfferAnotherEvilGame_When_OneIsInProgress()
    {
        var cut = RenderAndOpenEvilMode();

        PlayGuess(cut, [CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red]);

        cut.FindAll(".btn-try-again").Should().BeEmpty();
    }

    [Fact]
    public void Should_SaveTheEvilGameUnderItsOwnKey_When_AnEvilGuessIsSubmitted()
    {
        var cut = RenderAndOpenEvilMode();

        PlayGuess(cut, [CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red]);

        var saves = JSInterop.Invocations["localStorage.setItem"];
        saves.Should().ContainSingle(call => (string?)call.Arguments[0] == EvilKey)
            .Which.Arguments[1].Should().Be("""{"guesses":[["Red","Red","Red","Red"]]}""");
        saves.Should().NotContain(call => (string?)call.Arguments[0] == SavedGameKey);
    }

    [Fact]
    public void Should_RestoreTheEvilGame_When_ThePageFirstRenders()
    {
        JSInterop.Setup<string?>("localStorage.getItem", EvilKey).SetResult("""{"guesses":[["Red","Red","Red","Red"]]}""");
        var cut = Render<Game>();
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("localStorage.getItem", calledTimes: 4));

        cut.Find(".mode-evil").Click();

        cut.FindAll(".board-row.played").Should().ContainSingle();
    }

    [Fact]
    public void Should_RestoreTodaysDailyGame_When_ThePageFirstRenders()
    {
        JSInterop.Setup<string?>("localStorage.getItem", DailyKey)
            .SetResult("""{"date":"2026-11-03","guesses":[["White","White","White","White"]]}""");
        var cut = Render<Game>();
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("localStorage.getItem", calledTimes: 4));

        cut.Find(".mode-daily").Click();

        cut.FindAll(".board-row.played").Should().ContainSingle();
    }

    // Debug arrives as a query parameter, so it is set by navigating rather than by passing
    // the parameter directly.
    private void RequestDebugMode()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("debug", true));
    }

    // A clock the tests set: the instant and the player's time zone, which together decide
    // the local date the daily puzzle follows.
    private sealed class FixedTimeProvider : TimeProvider
    {
        public required DateTimeOffset UtcNow { get; set; }

        public required TimeZoneInfo Zone { get; set; }

        public override DateTimeOffset GetUtcNow() => UtcNow;

        public override TimeZoneInfo LocalTimeZone => Zone;
    }

    private sealed class FakeHostEnvironment : IWebAssemblyHostEnvironment
    {
        public required string Environment { get; set; }

        public string BaseAddress => "http://localhost/";
    }
}
