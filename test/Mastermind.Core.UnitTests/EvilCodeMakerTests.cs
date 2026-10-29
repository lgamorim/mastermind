using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using static Mastermind.Core.CodePeg;

namespace Mastermind.Core.UnitTests;

// Expected values come from an independent Python implementation of the same rule (group
// every remaining code by the answer it would give, keep the largest group, break ties by
// fewest black then fewest white pegs), not from this code.
public class EvilCodeMakerTests
{
    [Fact]
    public void Should_FitTheFirstCode_When_NothingHasBeenAskedYet()
    {
        var codeMaker = new EvilCodeMaker();

        codeMaker.SecretCode.Should().Equal(Red, Red, Red, Red);
    }

    [Fact]
    public void Should_GiveTheStingiestOfTheLargestAnswers_When_TheyTie()
    {
        // Red Red Blue Blue splits the 1,296 codes into groups led by three of 256 codes each:
        // nothing right, one white, and one black. The stingiest of those is nothing right.
        var codeMaker = new EvilCodeMaker();

        var response = codeMaker.Answer([Red, Red, Blue, Blue]);

        response.Should().Be(new Response(0, 0));
        codeMaker.SecretCode.Should().Equal(Yellow, Yellow, Yellow, Yellow);
    }

    // Always guessing the first code that still fits: each guess and the answer the evil code
    // maker gives. It keeps 625, 256, 108, 24, 4, 2, 1 and 1 codes after them.
    private static readonly (CodePeg[] Guess, Response Answer)[] FirstFitGame =
    [
        ([Red, Red, Red, Red], new Response(0, 0)),
        ([Blue, Blue, Blue, Blue], new Response(0, 0)),
        ([Yellow, Yellow, Yellow, Yellow], new Response(1, 0)),
        ([Yellow, Green, Green, Green], new Response(0, 1)),
        ([White, Yellow, White, White], new Response(1, 1)),
        ([White, Black, Yellow, Black], new Response(2, 2)),
        ([White, Black, Black, Yellow], new Response(1, 3)),
        ([Black, Black, Yellow, White], new Response(4, 0)),
    ];

    [Fact]
    public void Should_HoldOutForEightGuesses_When_TheFirstFittingCodeIsAlwaysGuessed()
    {
        var codeMaker = new EvilCodeMaker();

        var answers = FirstFitGame.Select(turn => codeMaker.Answer(turn.Guess)).ToList();

        answers.Should().Equal(FirstFitGame.Select(turn => turn.Answer));
        codeMaker.SecretCode.Should().Equal(Black, Black, Yellow, White);
    }

    [Fact]
    public void Should_RefuseTheWin_When_TwoCodesRemainAndTheGuessIsOneOfThem()
    {
        // After the sixth guess only White Black Black Yellow and Black Black Yellow White fit.
        // Guessing the first gives a one-code group either way, and the stingier answer wins.
        var codeMaker = PlayFirstFitGameUpTo(attempt: 6);

        var response = codeMaker.Answer([White, Black, Black, Yellow]);

        response.Should().Be(new Response(1, 3));
    }

    [Fact]
    public void Should_FitEveryAnswerItGave_When_TheGameIsOver()
    {
        var codeMaker = new EvilCodeMaker();
        var turns = new List<(CodePeg[] Guess, Response Answer)>();
        foreach (var guess in new CodePeg[][] { [Red, Green, Blue, Red], [Yellow, White, Black, Green], [Blue, Blue, White, Yellow] })
        {
            turns.Add((guess, codeMaker.Answer(guess)));
        }

        var secret = codeMaker.SecretCode;

        turns.Should().OnlyContain(turn => Response.Score(secret, turn.Guess) == turn.Answer);
    }

    [Fact]
    public void Should_AnswerTheSameWay_When_TheSameGuessesArePlayedAgain()
    {
        CodePeg[][] guesses = [[Red, Green, Blue, Red], [Yellow, White, Black, Green], [Blue, Blue, White, Yellow]];

        var first = guesses.Select(new EvilCodeMaker().Answer).ToList();
        var second = guesses.Select(new EvilCodeMaker().Answer).ToList();

        second.Should().Equal(first);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_TheGuessIsNull()
    {
        // null! deliberately breaks the non-nullable contract: rejecting null is what this test checks.
        var answer = new Action(() => new EvilCodeMaker().Answer(null!));

        answer.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void Should_LetTheCodeBreakerWin_When_PlayedThroughAGame()
    {
        var game = new Game(new EvilCodeMaker());

        foreach (var turn in FirstFitGame)
        {
            game.Guess(turn.Guess);
        }

        game.Status.Should().Be(GameStatus.CodeBreakerWon);
        game.History.Should().HaveCount(8);
    }

    private static EvilCodeMaker PlayFirstFitGameUpTo(int attempt)
    {
        var codeMaker = new EvilCodeMaker();
        foreach (var turn in FirstFitGame.Take(attempt))
        {
            codeMaker.Answer(turn.Guess);
        }

        return codeMaker;
    }
}
