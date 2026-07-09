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
