using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Mastermind.Core;
using Mastermind.WebApp.Services;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class DailyShareTextTests
{
    private static readonly CodePeg[] AnyGuess = [CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Green];

    private static IReadOnlyList<GuessRecord> History(params Response[] responses) =>
        responses.Select((response, index) => new GuessRecord(index + 1, AnyGuess, response)).ToList();

    [Fact]
    public void Should_LayOutTheHeaderRowsAndLegend_When_ADailyGameIsWon()
    {
        var text = DailyShareText.Create(12, History(new Response(1, 1), new Response(2, 1), new Response(4, 0)), won: true);

        text.Should().Be(string.Join("\n",
            "Mastermind Daily #12 3/10",
            "",
            "🟩🟨⬛⬛",
            "🟩🟩🟨⬛",
            "🟩🟩🟩🟩",
            "",
            "🟩 right color, right place",
            "🟨 right color, wrong place"));
    }

    [Fact]
    public void Should_ScoreItAsX_When_ADailyGameIsLost()
    {
        var history = History(Enumerable.Repeat(new Response(0, 2), 10).ToArray());

        var text = DailyShareText.Create(3, history, won: false);

        text.Split('\n')[0].Should().Be("Mastermind Daily #3 X/10");
        text.Split('\n').Count(line => line == "🟨🟨⬛⬛").Should().Be(10);
    }

    [Fact]
    public void Should_FillTheRowWithEmptySquares_When_AGuessScoresNothing()
    {
        var text = DailyShareText.Create(1, History(new Response(0, 0), new Response(4, 0)), won: true);

        text.Split('\n')[2].Should().Be("⬛⬛⬛⬛");
    }

    [Fact]
    public void Should_NeverRevealTheGuessedColors_When_Created()
    {
        var text = DailyShareText.Create(1, History(new Response(1, 2), new Response(4, 0)), won: true);

        foreach (var color in Enum.GetNames<CodePeg>())
        {
            text.Should().NotContain(color);
        }
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_TheHistoryIsNull()
    {
        // null! deliberately breaks the non-nullable contract: rejecting null is what this test checks.
        var create = new Action(() => DailyShareText.Create(1, null!, won: false));

        create.Should().ThrowExactly<ArgumentNullException>();
    }
}
