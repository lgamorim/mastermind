using System;
using FluentAssertions;
using Xunit;
using static Mastermind.Core.CodePeg;

namespace Mastermind.Core.UnitTests;

public class DecodingBoardTests
{
    private static readonly CodePeg[] DefaultSecret = [Black, Blue, Green, White];

    private static DecodingBoard CreateBoard(int shieldSize = 4, int totalRows = 10)
        => new(new BoardConfig(shieldSize, totalRows));

    private static DecodingBoard CreatePlayedBoard() => CreatePlayedBoard(DefaultSecret);

    private static DecodingBoard CreatePlayedBoard(CodePeg[] secret)
    {
        var decodingBoard = CreateBoard(secret.Length);
        decodingBoard.PlayCodeMaker(new Shield(secret));
        return decodingBoard;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, -1)]
    [InlineData(-1, 10)]
    public void Should_ThrowArgumentException_When_BoardConfigIsInvalid(int shieldSize, int totalRows)
    {
        var action = new Action(() => new DecodingBoard(new BoardConfig(shieldSize, totalRows)));

        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Should_CreateShield_When_CodeMakerPlaysValidShield()
    {
        var decodingBoard = CreateBoard();
        var shield = new Shield(new CodePeg[4]);

        decodingBoard.PlayCodeMaker(shield);

        decodingBoard.Shield.Should().Be(shield);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_CodeMakerShieldIsNull()
    {
        var decodingBoard = CreateBoard();

        var action = new Action(() => decodingBoard.PlayCodeMaker(null));

        action.Should().ThrowExactly<ArgumentNullException>();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void Should_ThrowArgumentException_When_CodeMakerShieldIsDifferentThanConfig(int shieldSize)
    {
        var decodingBoard = CreateBoard();
        var shield = new Shield(new CodePeg[shieldSize]);

        var action = new Action(() => decodingBoard.PlayCodeMaker(shield));

        action.Should().ThrowExactly<ArgumentException>();
    }

    // Each row carries its own secret, guess and the full expected feedback
    // (both black and white key pegs), so every scoring scenario is asserted on
    // both dimensions rather than just one. Consolidates the former per-count
    // theories and the all-black / all-white / mixed / duplicate scoring tests.
    public static TheoryData<CodePeg[], CodePeg[], int, int> ScoringCases() => new()
    {
        // All exact matches / all correct colors in the wrong positions.
        { [Black, Blue, Green, White], [Black, Blue, Green, White], 4, 0 },
        { [Black, Blue, Green, White], [White, Green, Blue, Black], 0, 4 },

        // Exactly one black key peg, against secret [Black, Blue, Green, White].
        { [Black, Blue, Green, White], [Black, Black, Black, Black], 1, 0 },
        { [Black, Blue, Green, White], [Red, Blue, Yellow, Red], 1, 0 },
        { [Black, Blue, Green, White], [Blue, White, Green, Black], 1, 3 },
        { [Black, Blue, Green, White], [White, White, White, White], 1, 0 },

        // Correct colors in wrong positions, against secret [Black, Blue, Green, White].
        { [Black, Blue, Green, White], [Red, Black, Green, White], 2, 1 },
        { [Black, Blue, Green, White], [Blue, Red, Yellow, Red], 0, 1 },
        { [Black, Blue, Green, White], [Red, Yellow, Red, Green], 0, 1 },
        { [Black, Blue, Green, White], [Black, Blue, White, Red], 2, 1 },

        // Two black key pegs, against secret [Black, Blue, Blue, Black].
        { [Black, Blue, Blue, Black], [Black, Black, Blue, Blue], 2, 2 },
        { [Black, Blue, Blue, Black], [Black, Green, White, Black], 2, 0 },
        { [Black, Blue, Blue, Black], [Green, Blue, Blue, White], 2, 0 },
        { [Black, Blue, Blue, Black], [Blue, Blue, Black, Black], 2, 2 },

        // Wrong-position colors with duplicates, against secret [Black, Blue, Blue, Black].
        { [Black, Blue, Blue, Black], [Blue, Green, White, Blue], 0, 2 },
        { [Black, Blue, Blue, Black], [Green, Black, Black, White], 0, 2 },

        // Duplicate cap: a color present once in the secret yields at most one white peg.
        { [Black, Blue, Green, White], [Black, Red, Blue, Blue], 1, 1 },
        { [Black, Blue, Green, White], [Red, Blue, Black, Black], 1, 1 },

        // Three correct colors in the wrong positions, against secret [Black, Blue, Green, White].
        { [Black, Blue, Green, White], [Black, White, Blue, Green], 1, 3 },
        { [Black, Blue, Green, White], [Red, Black, White, Green], 0, 3 },
        { [Black, Blue, Green, White], [Blue, Black, White, Red], 0, 3 },
        { [Black, Blue, Green, White], [Green, Black, Blue, White], 1, 3 },

        // Mixed black and white scoring.
        { [Red, Red, Blue, Green], [Red, Blue, Green, Yellow], 1, 2 },
        { [Blue, Blue, Green, Red], [Blue, Green, Red, Red], 2, 1 },
        { [Yellow, Yellow, Green, Blue], [Blue, Red, Green, White], 1, 1 },
        { [Black, Black, Black, Black], [Red, Red, Red, Red], 0, 0 },
        { [Black, Blue, Green, White], [Black, Green, Blue, Yellow], 1, 2 },

        // Duplicate pegs in both secret and guess.
        { [Red, Blue, Red, Blue], [Blue, Red, Green, Green], 0, 2 },
        { [Red, Red, Blue, Blue], [Blue, Blue, Red, Red], 0, 4 },
    };

    [Theory]
    [MemberData(nameof(ScoringCases))]
    public void Should_ScoreKeyPegs_AccordingToSecretAndGuess(
        CodePeg[] secret, CodePeg[] guess, int expectedBlack, int expectedWhite)
    {
        var decodingBoard = CreatePlayedBoard(secret);

        var result = decodingBoard.PlayCodeBreaker(guess);

        result.Should().NotBeNull();
        result.BlackKeyPegs.Should().Be(expectedBlack);
        result.WhiteKeyPegs.Should().Be(expectedWhite);
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_CodeBreakerPlaysBeforeCodeMaker()
    {
        var decodingBoard = CreateBoard();

        var action = new Action(() => decodingBoard.PlayCodeBreaker(DefaultSecret));

        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("Code maker must play first.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void Should_ThrowArgumentException_When_CodeBreakerCodeLengthDiffersFromShield(int codeLength)
    {
        var decodingBoard = CreatePlayedBoard();

        var action = new Action(() => decodingBoard.PlayCodeBreaker(new CodePeg[codeLength]));

        action.Should().ThrowExactly<ArgumentException>()
            .WithMessage("*Code must have exactly 4 pegs.*");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_CodeBreakerCodeIsNull()
    {
        var decodingBoard = CreatePlayedBoard();

        var action = new Action(() => decodingBoard.PlayCodeBreaker(null));

        action.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void Should_SolveSecretCode_When_ResponseBlackPegsEqualsShieldCount()
    {
        var decodingBoard = CreatePlayedBoard();

        var result = decodingBoard.HasCodeBreakerSolvedSecretCode(new Response(4, 0));

        result.Should().BeTrue();
    }

    [Theory]
    // Total below the shield count.
    [InlineData(0, 0)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    // Total equal to the shield count but not all black.
    [InlineData(0, 4)]
    [InlineData(3, 1)]
    // Negative black pegs with an in-range total: returns false rather than throwing.
    [InlineData(-1, 5)]
    public void Should_NotSolveSecretCode_When_BlackPegsDoNotEqualShieldCount(int blackKeyPegs, int whiteKeyPegs)
    {
        var decodingBoard = CreatePlayedBoard();

        var result = decodingBoard.HasCodeBreakerSolvedSecretCode(new Response(blackKeyPegs, whiteKeyPegs));

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(5, 0)]
    [InlineData(0, 5)]
    [InlineData(4, 1)]
    [InlineData(1, 4)]
    public void Should_ThrowArgumentException_When_ResponseTotalKeyPegsOutsideBoundaries(int blackKeyPegs, int whiteKeyPegs)
    {
        var decodingBoard = CreatePlayedBoard();

        var action = new Action(() => decodingBoard.HasCodeBreakerSolvedSecretCode(new Response(blackKeyPegs, whiteKeyPegs)));

        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Should_UseDefaultBoardConfig_When_DefaultConstructorIsUsed()
    {
        var decodingBoard = new DecodingBoard();

        decodingBoard.BoardConfig.ShieldSize.Should().Be(4);
        decodingBoard.BoardConfig.TotalRows.Should().Be(10);
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_SolveCheckBeforeCodeMaker()
    {
        var decodingBoard = new DecodingBoard();

        var action = new Action(() => decodingBoard.HasCodeBreakerSolvedSecretCode(new Response(4, 0)));

        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("Code maker must play first.");
    }

    [Fact]
    public void Should_ScoreFivePegShield_When_BoardConfigUsesNonDefaultShieldSize()
    {
        var decodingBoard = CreatePlayedBoard([Red, Blue, Yellow, Green, White]);

        var result = decodingBoard.PlayCodeBreaker([Blue, Yellow, Green, White, Red]);

        result.BlackKeyPegs.Should().Be(0);
        result.WhiteKeyPegs.Should().Be(5);
    }

    [Fact]
    public void Should_SolveFivePegSecretCode_When_ResponseBlackPegsEqualsShieldCount()
    {
        var decodingBoard = CreatePlayedBoard([Red, Blue, Yellow, Green, White]);

        var result = decodingBoard.HasCodeBreakerSolvedSecretCode(new Response(5, 0));

        result.Should().BeTrue();
    }

    [Fact]
    public void Should_OverwriteShield_When_CodeMakerPlaysTwice()
    {
        var decodingBoard = CreateBoard();

        var firstShield = new Shield([Red, Red, Red, Red]);
        var secondShield = new Shield([Blue, Blue, Blue, Blue]);

        decodingBoard.PlayCodeMaker(firstShield);
        decodingBoard.PlayCodeMaker(secondShield);

        decodingBoard.Shield.Should().Be(secondShield);

        var result = decodingBoard.PlayCodeBreaker([Blue, Blue, Blue, Blue]);

        result.BlackKeyPegs.Should().Be(4);
        result.WhiteKeyPegs.Should().Be(0);
    }
}
