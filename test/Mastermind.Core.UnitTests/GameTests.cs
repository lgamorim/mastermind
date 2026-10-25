using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using static Mastermind.Core.CodePeg;

namespace Mastermind.Core.UnitTests;

public class GameTests
{
    private static readonly CodePeg[] SecretCode = [Red, Blue, Yellow, Green];
    private static readonly CodePeg[] WrongGuess = [White, White, White, White];

    private static Game CreateGame() => new(new FixedSecretCodeGenerator(SecretCode));

    [Fact]
    public void Should_RequestSecretOfStandardShieldSize_When_Created()
    {
        var generator = new FixedSecretCodeGenerator(SecretCode);

        var game = new Game(generator);

        generator.RequestedSizes.Should().Equal(BoardConfig.Standard.ShieldSize);
        game.BoardConfig.Should().Be(BoardConfig.Standard);
    }

    [Fact]
    public void Should_StartInProgressWithEmptyHistory_When_Created()
    {
        var game = CreateGame();

        game.Status.Should().Be(GameStatus.InProgress);
        game.History.Should().BeEmpty();
    }

    [Fact]
    public void Should_ExposeTheGeneratedSecretCode_When_Created()
    {
        var game = CreateGame();

        game.SecretCode.Should().Equal(SecretCode);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_GeneratorIsNull()
    {
        // null! deliberately breaks the non-nullable contract: rejecting null is what this test checks.
        var action = new Action(() => new Game((ISecretCodeGenerator)null!));

        action.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_GeneratorReturnsNull()
    {
        // null! stands in for a misbehaving generator that breaks its non-nullable contract.
        var action = new Action(() => new Game(new FixedSecretCodeGenerator(null!)));

        action.Should().ThrowExactly<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void Should_ThrowArgumentException_When_GeneratorReturnsWrongLength(int length)
    {
        var action = new Action(() => new Game(new FixedSecretCodeGenerator(new CodePeg[length])));

        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Should_StartInProgressWithTheGivenSecret_When_CreatedFromASecretCode()
    {
        var game = new Game([Black, Black, Green, White]);

        game.SecretCode.Should().Equal(Black, Black, Green, White);
        game.BoardConfig.Should().Be(BoardConfig.Standard);
        game.Status.Should().Be(GameStatus.InProgress);
        game.History.Should().BeEmpty();
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_TheSecretCodeIsNull()
    {
        // null! deliberately breaks the non-nullable contract: rejecting null is what this test checks.
        var action = new Action(() => new Game((CodePeg[])null!));

        action.Should().ThrowExactly<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void Should_ThrowArgumentException_When_TheSecretCodeHasTheWrongLength(int length)
    {
        var action = new Action(() => new Game(new CodePeg[length]));

        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Should_KeepTheOriginalSecret_When_TheSecretCodeArrayIsModifiedAfterCreation()
    {
        var secretCode = new[] { Red, Blue, Yellow, Green };
        var game = new Game(secretCode);

        secretCode[0] = Black;
        game.Guess([Red, Blue, Yellow, Green]);

        game.Status.Should().Be(GameStatus.CodeBreakerWon);
        game.SecretCode.Should().Equal(Red, Blue, Yellow, Green);
    }

    [Fact]
    public void Should_KeepTheOriginalSecret_When_TheGeneratorReusesItsArray()
    {
        var generated = new[] { Red, Blue, Yellow, Green };
        var game = new Game(new FixedSecretCodeGenerator(generated));

        generated[0] = Black;
        game.Guess([Red, Blue, Yellow, Green]);

        game.Status.Should().Be(GameStatus.CodeBreakerWon);
        game.SecretCode.Should().Equal(Red, Blue, Yellow, Green);
    }

    [Fact]
    public void Should_ScoreAndRecordTheGuess_When_AGuessIsPlayed()
    {
        var game = CreateGame();

        var response = game.Guess([Red, Yellow, White, White]);

        response.Should().Be(new Response(1, 1));
        game.History.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Attempt = 1, Guess = new[] { Red, Yellow, White, White }, Response = response });
        game.Status.Should().Be(GameStatus.InProgress);
    }

    [Fact]
    public void Should_NumberAttemptsSequentially_When_SeveralGuessesArePlayed()
    {
        var game = CreateGame();

        game.Guess(WrongGuess);
        game.Guess(WrongGuess);
        game.Guess(WrongGuess);

        game.History.Should().HaveCount(3);
        game.History[0].Attempt.Should().Be(1);
        game.History[1].Attempt.Should().Be(2);
        game.History[2].Attempt.Should().Be(3);
    }

    [Fact]
    public void Should_EndWithCodeBreakerWon_When_TheGuessMatchesTheSecretCode()
    {
        var game = CreateGame();

        game.Guess(SecretCode);

        game.Status.Should().Be(GameStatus.CodeBreakerWon);
    }

    [Fact]
    public void Should_StayInProgress_When_OneRowIsStillLeft()
    {
        var game = CreateGame();

        PlayWrongGuesses(game, BoardConfig.Standard.TotalRows - 1);

        game.Status.Should().Be(GameStatus.InProgress);
    }

    [Fact]
    public void Should_EndWithCodeMakerWon_When_EveryRowIsUsedWithoutSolving()
    {
        var game = CreateGame();

        PlayWrongGuesses(game, BoardConfig.Standard.TotalRows);

        game.Status.Should().Be(GameStatus.CodeMakerWon);
    }

    [Fact]
    public void Should_EndWithCodeBreakerWon_When_TheLastRowSolvesTheCode()
    {
        var game = CreateGame();
        PlayWrongGuesses(game, BoardConfig.Standard.TotalRows - 1);

        game.Guess(SecretCode);

        game.Status.Should().Be(GameStatus.CodeBreakerWon);
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_AGuessIsPlayedAfterTheCodeBreakerWon()
    {
        var game = CreateGame();
        game.Guess(SecretCode);

        var action = new Action(() => game.Guess(SecretCode));

        action.Should().ThrowExactly<InvalidOperationException>();
        game.History.Should().HaveCount(1);
        game.Status.Should().Be(GameStatus.CodeBreakerWon);
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_AGuessIsPlayedAfterTheCodeMakerWon()
    {
        var game = CreateGame();
        PlayWrongGuesses(game, BoardConfig.Standard.TotalRows);

        var action = new Action(() => game.Guess(SecretCode));

        action.Should().ThrowExactly<InvalidOperationException>();
        game.History.Should().HaveCount(BoardConfig.Standard.TotalRows);
        game.Status.Should().Be(GameStatus.CodeMakerWon);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void Should_ThrowArgumentExceptionWithoutRecording_When_TheGuessHasTheWrongLength(int length)
    {
        var game = CreateGame();

        var action = new Action(() => game.Guess(new CodePeg[length]));

        action.Should().ThrowExactly<ArgumentException>();
        game.History.Should().BeEmpty();
        game.Status.Should().Be(GameStatus.InProgress);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_TheGuessIsNull()
    {
        var game = CreateGame();

        // null! deliberately breaks the non-nullable contract: rejecting null is what this test checks.
        var action = new Action(() => game.Guess(null!));

        action.Should().ThrowExactly<ArgumentNullException>();
        game.History.Should().BeEmpty();
    }

    [Fact]
    public void Should_NotReflectSourceArrayMutation_When_TheGuessArrayIsModifiedAfterPlaying()
    {
        var game = CreateGame();
        var guess = new[] { Red, Red, Red, Red };

        game.Guess(guess);
        guess[0] = Blue;

        game.History[0].Guess[0].Should().Be(Red);
    }

    [Fact]
    public void Should_RejectMutation_When_TheHistoryIsCastBackToAList()
    {
        var game = CreateGame();
        game.Guess(WrongGuess);

        var mutate = new Action(() => ((List<GuessRecord>)game.History).Clear());

        mutate.Should().Throw<InvalidCastException>();
        game.History.Should().HaveCount(1);
    }

    [Fact]
    public void Should_RejectMutation_When_TheSecretCodeIsCastBackToAnArray()
    {
        var game = CreateGame();

        var mutate = new Action(() => ((CodePeg[])game.SecretCode)[0] = Black);

        mutate.Should().Throw<InvalidCastException>();
        game.SecretCode[0].Should().Be(Red);
    }

    [Fact]
    public void Should_RejectMutation_When_ARecordedGuessIsCastBackToAnArray()
    {
        var game = CreateGame();
        game.Guess(WrongGuess);

        var mutate = new Action(() => ((CodePeg[])game.History[0].Guess)[0] = Black);

        mutate.Should().Throw<InvalidCastException>();
        game.History[0].Guess[0].Should().Be(White);
    }

    private static void PlayWrongGuesses(Game game, int count)
    {
        for (var i = 0; i < count; i++)
        {
            game.Guess(WrongGuess);
        }
    }

    private sealed class FixedSecretCodeGenerator(CodePeg[] secretCode) : ISecretCodeGenerator
    {
        public List<int> RequestedSizes { get; } = [];

        public CodePeg[] Generate(int size)
        {
            RequestedSizes.Add(size);
            return secretCode;
        }
    }
}
