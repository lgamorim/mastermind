using System.Collections.Generic;
using System.Linq;
using Bunit;
using FluentAssertions;
using Mastermind.WebApp.Components;
using Mastermind.WebApp.Services;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class StatsPanelTests : BunitContext
{
    // 5 games: wins in 2, 4, 4 and 4 guesses, then a loss.
    private static readonly GameStats SampleStats =
        GameStats.Empty.RecordWin(2).RecordWin(4).RecordWin(4).RecordWin(4).RecordLoss();

    [Fact]
    public void Should_ShowTheHeadlineNumbers_When_Rendered()
    {
        var cut = Render<StatsPanel>(parameters => parameters.Add(panel => panel.Stats, SampleStats));

        var tiles = cut.FindAll(".stat").ToDictionary(
            tile => tile.QuerySelector("dt")?.TextContent.Trim() ?? "",
            tile => tile.QuerySelector("dd")?.TextContent.Trim() ?? "");
        tiles.Should().Equal(new Dictionary<string, string>
        {
            ["Played"] = "5",
            ["Win rate"] = "80%",
            ["Current streak"] = "0",
            ["Best streak"] = "4",
        });
    }

    [Fact]
    public void Should_ListEveryGuessCountWithItsWins_When_GamesHaveBeenWon()
    {
        var cut = Render<StatsPanel>(parameters => parameters.Add(panel => panel.Stats, SampleStats));

        var rows = cut.FindAll(".distribution-row");
        rows.Should().HaveCount(10);
        rows.Select(row => row.QuerySelector(".distribution-count")?.FirstChild?.TextContent.Trim())
            .Should().Equal("0", "1", "0", "3", "0", "0", "0", "0", "0", "0");
        rows[3].TextContent.Should().Contain("4 guesses").And.Contain("3 wins");
        rows[1].TextContent.Should().Contain("2 guesses").And.Contain("1 win");
    }

    [Fact]
    public void Should_ScaleEachBarToTheMostCommonGuessCount_When_Rendered()
    {
        var cut = Render<StatsPanel>(parameters => parameters.Add(panel => panel.Stats, SampleStats));

        var widths = cut.FindAll(".distribution-bar").Select(bar => bar.GetAttribute("style")).ToList();
        widths[3].Should().Be("width: 100%");
        widths[1].Should().Be("width: 33%");
        widths[0].Should().Be("width: 0%");
    }

    [Fact]
    public void Should_RoundAHalfPercentBarUp_When_ItFallsExactlyBetween()
    {
        // 8 wins in one guess and 1 in two: the second bar is 12.5% of the first.
        var stats = GameStats.Empty;
        for (var i = 0; i < 8; i++) stats = stats.RecordWin(1);
        stats = stats.RecordWin(2);

        var cut = Render<StatsPanel>(parameters => parameters.Add(panel => panel.Stats, stats));

        cut.FindAll(".distribution-bar")[1].GetAttribute("style").Should().Be("width: 13%");
    }

    [Fact]
    public void Should_HighlightTheGuessCountOfTheGameJustWon_When_OneIsGiven()
    {
        var cut = Render<StatsPanel>(parameters => parameters
            .Add(panel => panel.Stats, SampleStats)
            .Add(panel => panel.HighlightedGuessCount, 2));

        var highlighted = cut.FindAll(".distribution-row.highlighted");
        highlighted.Should().ContainSingle();
        highlighted[0].GetAttribute("aria-current").Should().Be("true");
        highlighted[0].TextContent.Should().Contain("2 guesses");
    }

    [Fact]
    public void Should_HighlightNothing_When_NoGuessCountIsGiven()
    {
        var cut = Render<StatsPanel>(parameters => parameters.Add(panel => panel.Stats, SampleStats));

        cut.FindAll(".distribution-row.highlighted").Should().BeEmpty();
    }

    [Fact]
    public void Should_SayThereAreNoWinsYet_When_NoGameHasBeenWon()
    {
        var cut = Render<StatsPanel>(parameters => parameters.Add(panel => panel.Stats, GameStats.Empty.RecordLoss()));

        cut.FindAll(".distribution-row").Should().BeEmpty();
        cut.Find(".stats-empty").TextContent.Should().Contain("No wins yet");
    }

    [Fact]
    public void Should_ShowThePanelWithAHideToggle_When_FirstRendered()
    {
        var cut = Render<StatsPanel>(parameters => parameters.Add(panel => panel.Stats, SampleStats));

        var toggle = cut.Find(".stats-toggle");
        toggle.TextContent.Should().Contain("Hide statistics");
        toggle.GetAttribute("aria-expanded").Should().Be("true");
        toggle.GetAttribute("aria-controls").Should().Be(cut.Find(".stats").Id);
        cut.Find(".stats").HasAttribute("hidden").Should().BeFalse();
    }

    [Fact]
    public void Should_HideThePanel_When_TheToggleIsClicked()
    {
        var cut = Render<StatsPanel>(parameters => parameters.Add(panel => panel.Stats, SampleStats));

        cut.Find(".stats-toggle").Click();

        var toggle = cut.Find(".stats-toggle");
        toggle.TextContent.Should().Contain("Show statistics");
        toggle.GetAttribute("aria-expanded").Should().Be("false");
        cut.Find(".stats").HasAttribute("hidden").Should().BeTrue();
    }

    [Fact]
    public void Should_ShowThePanelAgain_When_TheToggleIsClickedTwice()
    {
        var cut = Render<StatsPanel>(parameters => parameters.Add(panel => panel.Stats, SampleStats));

        cut.Find(".stats-toggle").Click();
        cut.Find(".stats-toggle").Click();

        cut.Find(".stats-toggle").GetAttribute("aria-expanded").Should().Be("true");
        cut.Find(".stats").HasAttribute("hidden").Should().BeFalse();
    }

    [Fact]
    public void Should_KeepShowingTheLatestStats_When_ThePanelIsReopened()
    {
        var cut = Render<StatsPanel>(parameters => parameters.Add(panel => panel.Stats, SampleStats));
        cut.Find(".stats-toggle").Click();

        cut.Render(parameters => parameters.Add(panel => panel.Stats, SampleStats.RecordWin(1)));
        cut.Find(".stats-toggle").Click();

        cut.FindAll(".stat dd")[0].TextContent.Should().Be("6");
    }
}
