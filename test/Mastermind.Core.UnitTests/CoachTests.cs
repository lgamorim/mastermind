using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using static Mastermind.Core.CodePeg;

namespace Mastermind.Core.UnitTests;

public class CoachTests
{
    private static IReadOnlyList<GuessRecord> Play(CodePeg[] secret, params CodePeg[][] guesses)
    {
        var game = new Game(secret);
        foreach (var guess in guesses)
        {
            game.Guess(guess);
        }

        return game.History;
    }

    [Fact]
    public void Should_CountEveryCodeOnTheBoard_When_NothingIsKnownYet()
    {
        // 6 colors in each of 4 positions, repeats allowed.
        Coach.TotalCodes.Should().Be(1296);
    }

    [Fact]
    public void Should_HaveNothingToSay_When_NoGuessHasBeenPlayed()
    {
        Coach.Review([]).Should().BeEmpty();
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_TheHistoryIsNull()
    {
        // null! deliberately breaks the non-nullable contract: rejecting null is what this test checks.
        var review = new Action(() => Coach.Review(null!));

        review.Should().ThrowExactly<ArgumentNullException>();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void Should_ThrowArgumentException_When_AGuessInTheHistoryHasTheWrongLength(int length)
    {
        // Not something Game can produce, but Review accepts any history.
        IReadOnlyList<GuessRecord> history = [new GuessRecord(1, Enumerable.Repeat(Red, length).ToArray(), new Response(0, 0))];

        var review = new Action(() => Coach.Review(history));

        review.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Should_LeaveOnlyCodesWithoutThoseColors_When_AGuessScoresNothing()
    {
        // Neither Red nor Blue is anywhere: 4 colors left in each of 4 positions.
        var history = Play([Yellow, Green, White, Black], [Red, Red, Blue, Blue]);

        Coach.Review(history).Single().CodesRemaining.Should().Be(256);
    }

    [Fact]
    public void Should_LeaveOnlyCodesWithExactlyOneRed_When_FourRedsScoreOneBlack()
    {
        // One position (4 ways) holds the Red, the other three any of the 5 other colors.
        var history = Play([Red, Yellow, Green, White], [Red, Red, Red, Red]);

        Coach.Review(history).Single().CodesRemaining.Should().Be(4 * 5 * 5 * 5);
    }

    [Fact]
    public void Should_NarrowTheCountGuessByGuess_When_SeveralGuessesArePlayed()
    {
        // After four Reds score one black: exactly one Red (500 codes). After four Blues also
        // score one black: one Red (4 places), one Blue (3 places left), 4 colors for the rest.
        var history = Play([Red, Blue, Green, White], [Red, Red, Red, Red], [Blue, Blue, Blue, Blue]);

        Coach.Review(history).Select(note => note.CodesRemaining).Should().Equal(500, 4 * 3 * 4 * 4);
    }

    [Fact]
    public void Should_LeaveOneCode_When_TheCodeIsCracked()
    {
        var history = Play([Red, Red, Blue, Blue], [Red, Red, Blue, Blue]);

        Coach.Review(history).Single().CodesRemaining.Should().Be(1);
    }

    [Fact]
    public void Should_NeverRuleOutTheSecret_When_AGameIsPlayedOut()
    {
        var history = Play(
            [Green, Black, Green, White],
            [Red, Red, Blue, Blue],
            [Yellow, Green, Yellow, Green],
            [Green, White, Black, Green],
            [Green, Black, Green, White]);

        var counts = Coach.Review(history).Select(note => note.CodesRemaining).ToList();

        counts.Should().OnlyContain(count => count >= 1);
        counts.Should().BeInDescendingOrder();
        counts[^1].Should().Be(1);
    }

    [Fact]
    public void Should_NumberEachNoteByItsAttempt_When_Reviewed()
    {
        var history = Play([Red, Blue, Yellow, Green], [White, White, White, White], [Black, Black, Black, Black]);

        Coach.Review(history).Select(note => note.Attempt).Should().Equal(1, 2);
    }

    [Fact]
    public void Should_NotCallTheFirstGuessAContradiction_When_Reviewed()
    {
        var history = Play([Red, Blue, Yellow, Green], [White, White, White, White]);

        Coach.Review(history).Single().ContradictedAttempt.Should().BeNull();
    }

    [Fact]
    public void Should_NameTheContradictedGuess_When_AGuessCouldNotHaveBeenTheCode()
    {
        // Four Reds scored one black, so the code holds a Red: four Blues cannot be it.
        var history = Play([Red, Blue, Yellow, Green], [Red, Red, Red, Red], [Blue, Blue, Blue, Blue]);

        Coach.Review(history)[1].ContradictedAttempt.Should().Be(1);
    }

    [Fact]
    public void Should_FindNoContradiction_When_AGuessFitsAllEarlierFeedback()
    {
        // Red, Blue, Blue, Blue holds exactly one Red, which is all four Reds scoring one black says.
        var history = Play([Red, Blue, Yellow, Green], [Red, Red, Red, Red], [Red, Blue, Blue, Blue]);

        Coach.Review(history)[1].ContradictedAttempt.Should().BeNull();
    }

    [Fact]
    public void Should_NameTheEarliestContradictedGuess_When_SeveralAreContradicted()
    {
        // Four Whites hold neither the Red nor the Yellow the first two guesses proved are there.
        var history = Play(
            [Red, Blue, Yellow, Green],
            [Red, Red, Red, Red],
            [Yellow, Yellow, Yellow, Yellow],
            [White, White, White, White]);

        Coach.Review(history)[2].ContradictedAttempt.Should().Be(1);
    }

    [Fact]
    public void Should_FindNoContradiction_When_TheWinningGuessIsPlayed()
    {
        var history = Play(
            [Red, Blue, Yellow, Green],
            [Red, Red, Red, Red],
            [Yellow, Yellow, Yellow, Yellow],
            [Red, Blue, Yellow, Green]);

        Coach.Review(history)[2].ContradictedAttempt.Should().BeNull();
    }
}
