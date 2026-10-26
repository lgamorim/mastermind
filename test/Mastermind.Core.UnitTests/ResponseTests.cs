using System;
using FluentAssertions;
using Xunit;
using static Mastermind.Core.CodePeg;

namespace Mastermind.Core.UnitTests;

public class ResponseTests
{
    [Fact]
    public void Should_ExposeKeyPegCounts_When_Constructed()
    {
        var response = new Response(2, 1);

        response.BlackKeyPegs.Should().Be(2);
        response.WhiteKeyPegs.Should().Be(1);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(-1, -1)]
    public void Should_ThrowArgumentOutOfRangeException_When_KeyPegCountIsNegative(int blackKeyPegs, int whiteKeyPegs)
    {
        var action = new Action(() => _ = new Response(blackKeyPegs, whiteKeyPegs));

        action.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    public static TheoryData<CodePeg[], CodePeg[], int, int> ScoredGuesses => new()
    {
        // secret, guess, black, white
        { [Red, Blue, Yellow, Green], [Red, Blue, Yellow, Green], 4, 0 },
        { [Red, Blue, Yellow, Green], [Green, Yellow, Blue, Red], 0, 4 },
        { [Red, Blue, Yellow, Green], [White, White, Black, Black], 0, 0 },
        { [Red, Blue, Yellow, Green], [Red, Red, Blue, Blue], 1, 1 },
        // Each secret peg earns at most one key peg: four Reds against one Red.
        { [Red, Blue, Yellow, Green], [Red, Red, Red, Red], 1, 0 },
        { [Red, Red, Blue, Blue], [Blue, Blue, Red, Red], 0, 4 },
        { [Red, Red, Blue, Blue], [Red, Blue, White, White], 1, 1 },
    };

    [Theory]
    [MemberData(nameof(ScoredGuesses))]
    public void Should_ScoreTheGuessAgainstTheSecret_When_Scored(CodePeg[] secret, CodePeg[] guess, int black, int white)
    {
        var response = Response.Score(secret, guess);

        response.Should().Be(new Response(black, white));
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_ScoringANullCode()
    {
        // null! deliberately breaks the non-nullable contract: rejecting null is what this test checks.
        var nullSecret = new Action(() => Response.Score(null!, [Red, Red, Red, Red]));
        var nullGuess = new Action(() => Response.Score([Red, Red, Red, Red], null!));

        nullSecret.Should().ThrowExactly<ArgumentNullException>();
        nullGuess.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void Should_ThrowArgumentException_When_TheCodesDifferInLength()
    {
        var action = new Action(() => Response.Score([Red, Red, Red, Red], [Red, Red, Red]));

        action.Should().ThrowExactly<ArgumentException>();
    }
}
