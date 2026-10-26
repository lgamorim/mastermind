using System;
using FluentAssertions;
using Mastermind.Core;
using Mastermind.WebApp.Services;
using NSubstitute;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class GameStateServiceTests
{
    private static readonly CodePeg[] SecretCode = [CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Green];

    [Fact]
    public void Should_RequestSecretOfShieldSize_When_StartNewGameCalled()
    {
        var secretCodeGenerator = CreateSecretCodeGenerator(SecretCode);
        var gameStateService = new GameStateService(secretCodeGenerator);

        gameStateService.StartNewGame();

        secretCodeGenerator.Received(1).Generate(gameStateService.BoardConfig.ShieldSize);
    }

    [Fact]
    public void Should_RecordGuessInHistory_When_GuessSubmitted()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();

        gameStateService.TrySubmitGuess([CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red], out var response, out _);

        gameStateService.History.Should().HaveCount(1);
        gameStateService.History[0].Attempt.Should().Be(1);
        gameStateService.History[0].Guess.Should().Equal(CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red);
        gameStateService.History[0].Response.Should().Be(response);
    }

    [Fact]
    public void Should_SetHasWonAndGameOver_When_GuessMatchesSecretCodeExactly()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();

        gameStateService.TrySubmitGuess(SecretCode, out _, out _);

        gameStateService.HasWon.Should().BeTrue();
        gameStateService.IsGameOver.Should().BeTrue();
    }

    [Fact]
    public void Should_RevealSecretCode_When_PlayerWins()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();

        gameStateService.TrySubmitGuess(SecretCode, out _, out _);

        gameStateService.RevealedSecretCode.Should().Equal(SecretCode);
    }

    [Fact]
    public void Should_SetGameOverAndRevealSecretCode_When_AllGuessesExhaustedWithoutWinning()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        var wrongGuess = new[] { CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White };

        for (var attempt = 0; attempt < gameStateService.BoardConfig.TotalRows; attempt++)
        {
            gameStateService.TrySubmitGuess(wrongGuess, out _, out _);
        }

        gameStateService.IsGameOver.Should().BeTrue();
        gameStateService.HasWon.Should().BeFalse();
        gameStateService.RevealedSecretCode.Should().Equal(SecretCode);
    }

    [Fact]
    public void Should_RevealSecretCodeImmediately_When_DebugModeEnabledBeforeNewGame()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.IsDebugMode = true;

        gameStateService.StartNewGame();

        gameStateService.RevealedSecretCode.Should().Equal(SecretCode);
    }

    [Fact]
    public void Should_NotRevealSecretCode_When_DebugModeDisabledAndGameInProgress()
    {
        var gameStateService = CreateGameStateService();

        gameStateService.StartNewGame();

        gameStateService.RevealedSecretCode.Should().BeNull();
    }

    [Fact]
    public void Should_NotRevealSecretCode_When_DebugModeEnabledAfterGameStarted()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();

        gameStateService.IsDebugMode = true;

        gameStateService.RevealedSecretCode.Should().BeNull();
    }

    [Fact]
    public void Should_ReturnFalseWithError_When_GuessSubmittedBeforeAnyGameStarted()
    {
        var gameStateService = CreateGameStateService();

        var result = gameStateService.TrySubmitGuess(SecretCode, out _, out var error);

        result.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
        gameStateService.History.Should().BeEmpty();
        gameStateService.IsGameOver.Should().BeFalse();
    }

    [Fact]
    public void Should_ReturnFalseWithError_When_GuessHasWrongLength()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();

        var result = gameStateService.TrySubmitGuess([CodePeg.Red, CodePeg.Blue], out _, out var error);

        result.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
        gameStateService.History.Should().BeEmpty();
    }

    [Fact]
    public void Should_ResetState_When_StartNewGameCalledAgainAfterGameOver()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess(SecretCode, out _, out _);

        gameStateService.StartNewGame();

        gameStateService.History.Should().BeEmpty();
        gameStateService.IsGameOver.Should().BeFalse();
        gameStateService.HasWon.Should().BeFalse();
    }

    [Fact]
    public void Should_ReturnFalseWithoutMutatingState_When_GuessSubmittedAfterGameOver()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess(SecretCode, out _, out _); // win -> game over

        var result = gameStateService.TrySubmitGuess(SecretCode, out _, out var error);

        result.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
        gameStateService.History.Should().HaveCount(1);
        gameStateService.HasWon.Should().BeTrue();
        gameStateService.IsGameOver.Should().BeTrue();
    }

    [Fact]
    public void Should_NotReflectSourceArrayMutation_When_GuessArrayModifiedAfterSubmit()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        var guess = new[] { CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red };

        gameStateService.TrySubmitGuess(guess, out _, out _);
        guess[0] = CodePeg.Blue;

        gameStateService.History[0].Guess[0].Should().Be(CodePeg.Red);
    }

    [Fact]
    public void Should_ExposeAllSixCodePegColors_As_AvailableColors()
    {
        var gameStateService = CreateGameStateService();

        gameStateService.AvailableColors.Should().BeEquivalentTo(Enum.GetValues<CodePeg>());
    }

    [Fact]
    public void Should_RejectMutation_When_AvailableColorsIsCastBackToAnArray()
    {
        var gameStateService = CreateGameStateService();

        var mutate = new Action(() => ((CodePeg[])gameStateService.AvailableColors)[0] = CodePeg.Black);

        mutate.Should().Throw<InvalidCastException>();
        gameStateService.AvailableColors[0].Should().Be(CodePeg.Red);
    }

    [Fact]
    public void Should_RevealTheOriginalSecret_When_TheGeneratorReusesItsArray()
    {
        var generated = new[] { CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Green };
        var gameStateService = new GameStateService(CreateSecretCodeGenerator(generated));
        gameStateService.StartNewGame();

        // A generator that reuses its buffer between calls must not be able to
        // change the code this game was started with.
        generated[0] = CodePeg.Black;
        gameStateService.TrySubmitGuess([CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Green], out _, out _);

        gameStateService.HasWon.Should().BeTrue();
        gameStateService.RevealedSecretCode.Should().Equal(CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Green);
    }

    [Fact]
    public void Should_HaveNothingToSave_When_NoGameHasStarted()
    {
        var gameStateService = CreateGameStateService();

        gameStateService.CreateSavedGame().Should().BeNull();
    }

    [Fact]
    public void Should_CaptureTheSecretGuessesAndDebugReveal_When_ASavedGameIsCreated()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.IsDebugMode = true;
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess([CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red], out _, out _);
        gameStateService.TrySubmitGuess([CodePeg.White, CodePeg.Blue, CodePeg.White, CodePeg.White], out _, out _);

        var savedGame = gameStateService.CreateSavedGame();

        savedGame.Should().BeEquivalentTo(new
        {
            SecretCode,
            Guesses = new[]
            {
                new[] { CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red },
                new[] { CodePeg.White, CodePeg.Blue, CodePeg.White, CodePeg.White },
            },
            RevealSecretFromStart = true,
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Should_ReplayTheGuesses_When_ASavedGameInProgressIsRestored()
    {
        var gameStateService = CreateGameStateService();
        var savedGame = new SavedGame(SecretCode, [[CodePeg.Red, CodePeg.Yellow, CodePeg.White, CodePeg.White]], RevealSecretFromStart: false);

        var restored = gameStateService.TryRestore(savedGame);

        restored.Should().BeTrue();
        gameStateService.History.Should().ContainSingle();
        gameStateService.History[0].Guess.Should().Equal(CodePeg.Red, CodePeg.Yellow, CodePeg.White, CodePeg.White);
        gameStateService.History[0].Response.Should().Be(new Response(1, 1));
        gameStateService.IsGameOver.Should().BeFalse();
        gameStateService.RevealedSecretCode.Should().BeNull();
    }

    [Fact]
    public void Should_RestoreTheOutcome_When_ASavedFinishedGameIsRestored()
    {
        var gameStateService = CreateGameStateService();
        var savedGame = new SavedGame(SecretCode, [[CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White], SecretCode], RevealSecretFromStart: false);

        gameStateService.TryRestore(savedGame);

        gameStateService.IsGameOver.Should().BeTrue();
        gameStateService.HasWon.Should().BeTrue();
        gameStateService.RevealedSecretCode.Should().Equal(SecretCode);
    }

    [Fact]
    public void Should_KeepRevealingTheSecret_When_ASavedDebugGameIsRestored()
    {
        var gameStateService = CreateGameStateService();

        gameStateService.TryRestore(new SavedGame(SecretCode, [], RevealSecretFromStart: true));

        gameStateService.RevealedSecretCode.Should().Equal(SecretCode);
    }

    [Fact]
    public void Should_NotAskForANewSecret_When_ASavedGameIsRestored()
    {
        var secretCodeGenerator = CreateSecretCodeGenerator(SecretCode);
        var gameStateService = new GameStateService(secretCodeGenerator);

        gameStateService.TryRestore(new SavedGame([CodePeg.Black, CodePeg.Black, CodePeg.Black, CodePeg.Black], [], RevealSecretFromStart: false));

        secretCodeGenerator.DidNotReceiveWithAnyArgs().Generate(default);
    }

    [Fact]
    public void Should_PlayAgainstTheSavedSecret_When_AGuessFollowsARestore()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.TryRestore(new SavedGame([CodePeg.Black, CodePeg.Black, CodePeg.Black, CodePeg.Black], [], RevealSecretFromStart: false));

        gameStateService.TrySubmitGuess([CodePeg.Black, CodePeg.Black, CodePeg.Black, CodePeg.Black], out _, out _);

        gameStateService.HasWon.Should().BeTrue();
    }

    public static TheoryData<string, SavedGame> InvalidSavedGames => new()
    {
        { "secret too short", new SavedGame([CodePeg.Red, CodePeg.Blue], [], false) },
        { "undefined secret peg", new SavedGame([CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, (CodePeg)99], [], false) },
        { "guess too long", new SavedGame(SecretCode, [[CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red]], false) },
        { "undefined guess peg", new SavedGame(SecretCode, [[CodePeg.Red, CodePeg.Red, CodePeg.Red, (CodePeg)(-1)]], false) },
        { "guess after the win", new SavedGame(SecretCode, [SecretCode, SecretCode], false) },
        // JSON's nullable checks cover properties, not array items, so "guesses":[null] gets this far.
        { "null guess", new SavedGame(SecretCode, [null!], false) },
    };

    [Theory]
    [MemberData(nameof(InvalidSavedGames))]
    public void Should_RejectTheSaveAndKeepTheCurrentGame_When_ASavedGameIsInvalid(string scenario, SavedGame savedGame)
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess([CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red], out _, out _);

        var restored = gameStateService.TryRestore(savedGame);

        restored.Should().BeFalse(scenario);
        gameStateService.History.Should().ContainSingle(scenario);
        gameStateService.IsGameOver.Should().BeFalse(scenario);
    }

    [Fact]
    public void Should_HaveEmptyStats_When_NothingHasBeenPlayed()
    {
        var gameStateService = CreateGameStateService();

        gameStateService.Stats.Should().BeEquivalentTo(GameStats.Empty);
    }

    [Fact]
    public void Should_RecordTheWinWithItsGuessCount_When_TheCodeIsCracked()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess([CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White], out _, out _);

        gameStateService.TrySubmitGuess(SecretCode, out _, out _);

        gameStateService.Stats.GamesPlayed.Should().Be(1);
        gameStateService.Stats.Wins.Should().Be(1);
        gameStateService.Stats.WinsByGuessCount[1].Should().Be(1);
    }

    [Fact]
    public void Should_RecordALoss_When_EveryRowIsUsedWithoutCrackingTheCode()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();

        for (var attempt = 0; attempt < gameStateService.BoardConfig.TotalRows; attempt++)
        {
            gameStateService.TrySubmitGuess([CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White], out _, out _);
        }

        gameStateService.Stats.GamesPlayed.Should().Be(1);
        gameStateService.Stats.Wins.Should().Be(0);
    }

    [Fact]
    public void Should_NotRecordAnything_When_AGuessLeavesTheGameInProgress()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();

        gameStateService.TrySubmitGuess([CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White], out _, out _);

        gameStateService.Stats.GamesPlayed.Should().Be(0);
    }

    [Fact]
    public void Should_RecordALoss_When_ANewGameAbandonsOneWithGuesses()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess(SecretCode, out _, out _);
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess([CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White], out _, out _);

        gameStateService.StartNewGame();

        gameStateService.Stats.GamesPlayed.Should().Be(2);
        gameStateService.Stats.Wins.Should().Be(1);
        gameStateService.Stats.CurrentStreak.Should().Be(0);
    }

    [Fact]
    public void Should_NotRecordALoss_When_ANewGameReplacesOneWithNoGuesses()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();

        gameStateService.StartNewGame();

        gameStateService.Stats.GamesPlayed.Should().Be(0);
    }

    [Fact]
    public void Should_NotRecordTheGameTwice_When_ANewGameFollowsAFinishedOne()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess(SecretCode, out _, out _);

        gameStateService.StartNewGame();

        gameStateService.Stats.GamesPlayed.Should().Be(1);
    }

    [Fact]
    public void Should_NotRecordDebugGames_When_TheyAreWonOrAbandoned()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.IsDebugMode = true;
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess(SecretCode, out _, out _);
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess([CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White], out _, out _);

        gameStateService.StartNewGame();

        gameStateService.Stats.GamesPlayed.Should().Be(0);
    }

    [Fact]
    public void Should_NotRecordAnything_When_AFinishedGameIsRestored()
    {
        var gameStateService = CreateGameStateService();

        gameStateService.TryRestore(new SavedGame(SecretCode, [SecretCode], RevealSecretFromStart: false));

        gameStateService.Stats.GamesPlayed.Should().Be(0);
    }

    [Fact]
    public void Should_RecordALoss_When_ANewGameAbandonsARestoredGameWithGuesses()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.TryRestore(new SavedGame(SecretCode, [[CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White]], RevealSecretFromStart: false));

        gameStateService.StartNewGame();

        gameStateService.Stats.GamesPlayed.Should().Be(1);
    }

    [Fact]
    public void Should_AdoptTheStoredStats_When_TheyAreConsistent()
    {
        var gameStateService = CreateGameStateService();
        var stored = GameStats.Empty.RecordWin(3).RecordLoss().RecordWin(6);

        var restored = gameStateService.TryRestoreStats(stored);

        restored.Should().BeTrue();
        gameStateService.Stats.Should().BeEquivalentTo(stored);
    }

    [Fact]
    public void Should_KeepTheCurrentStats_When_TheStoredStatsAreInconsistent()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.TryRestoreStats(GameStats.Empty.RecordWin(3));

        var restored = gameStateService.TryRestoreStats(new GameStats(1, 5, 0, 0, [5, 0, 0, 0, 0, 0, 0, 0, 0, 0]));

        restored.Should().BeFalse();
        gameStateService.Stats.Wins.Should().Be(1);
    }

    [Fact]
    public void Should_HaveNoCoachNotes_When_NoGameHasStarted()
    {
        var gameStateService = CreateGameStateService();

        var coachNotes = gameStateService.CoachNotes;

        coachNotes.Should().BeEmpty();
    }

    [Fact]
    public void Should_KeepTheCoachNotes_When_ASavedGameIsRejected()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess([CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red], out _, out _);
        var notesBefore = gameStateService.CoachNotes;

        // A guess after the winning one cannot be replayed, so the save is rejected.
        gameStateService.TryRestore(new SavedGame(SecretCode, [SecretCode, SecretCode], RevealSecretFromStart: false));

        gameStateService.CoachNotes.Should().BeSameAs(notesBefore);
    }

    [Fact]
    public void Should_ReviewEveryPlayedGuess_When_GuessesAreSubmitted()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess([CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red], out _, out _);

        gameStateService.TrySubmitGuess([CodePeg.Blue, CodePeg.Blue, CodePeg.Blue, CodePeg.Blue], out _, out _);

        gameStateService.CoachNotes.Should().Equal(Coach.Review(gameStateService.History));
        gameStateService.CoachNotes[1].ContradictedAttempt.Should().Be(1);
    }

    [Fact]
    public void Should_KeepTheCoachNotes_When_AGuessIsRejected()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess([CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red], out _, out _);

        gameStateService.TrySubmitGuess([CodePeg.Red], out _, out _);

        gameStateService.CoachNotes.Should().ContainSingle();
    }

    [Fact]
    public void Should_ClearTheCoachNotes_When_ANewGameStarts()
    {
        var gameStateService = CreateGameStateService();
        gameStateService.StartNewGame();
        gameStateService.TrySubmitGuess([CodePeg.Red, CodePeg.Red, CodePeg.Red, CodePeg.Red], out _, out _);

        gameStateService.StartNewGame();

        gameStateService.CoachNotes.Should().BeEmpty();
    }

    [Fact]
    public void Should_ReviewTheRestoredGuesses_When_ASavedGameIsRestored()
    {
        var gameStateService = CreateGameStateService();

        gameStateService.TryRestore(new SavedGame(SecretCode, [[CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White]], RevealSecretFromStart: false));

        // Four Whites scored nothing, so White is nowhere: 5 colors in each of 4 holes.
        gameStateService.CoachNotes.Should().ContainSingle().Which.CodesRemaining.Should().Be(625);
    }

    private static GameStateService CreateGameStateService()
    {
        return new GameStateService(CreateSecretCodeGenerator(SecretCode));
    }

    private static ISecretCodeGenerator CreateSecretCodeGenerator(CodePeg[] secretCode)
    {
        var secretCodeGenerator = Substitute.For<ISecretCodeGenerator>();
        secretCodeGenerator.Generate(Arg.Any<int>()).Returns(secretCode);

        return secretCodeGenerator;
    }
}
