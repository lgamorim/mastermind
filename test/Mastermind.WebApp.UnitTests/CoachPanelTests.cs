using Bunit;
using FluentAssertions;
using Mastermind.Core;
using Mastermind.WebApp.Components;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class CoachPanelTests : BunitContext
{
    private IRenderedComponent<CoachPanel> RenderWith(params CoachNote[] notes) =>
        Render<CoachPanel>(parameters => parameters.Add(panel => panel.Notes, notes));

    [Fact]
    public void Should_SayHowManyCodesArePossible_When_NoGuessHasBeenPlayed()
    {
        var cut = RenderWith();

        cut.Find(".coach-count").TextContent.Should().Be("1,296 codes are possible.");
    }

    [Fact]
    public void Should_SayHowManyCodesStillFit_When_GuessesHaveBeenPlayed()
    {
        var cut = RenderWith(new CoachNote(1, 256, null), new CoachNote(2, 44, null));

        cut.Find(".coach-count").TextContent.Should().Be("44 codes still fit the feedback.");
    }

    [Fact]
    public void Should_UseTheSingular_When_OneCodeFits()
    {
        var cut = RenderWith(new CoachNote(1, 1, null));

        cut.Find(".coach-count").TextContent.Should().Be("1 code still fits the feedback.");
    }

    [Fact]
    public void Should_ExplainTheContradiction_When_TheLatestGuessCouldNotHaveBeenTheCode()
    {
        var cut = RenderWith(new CoachNote(1, 500, null), new CoachNote(2, 192, 1));

        cut.Find(".coach-warning").TextContent.Should()
            .Be("Guess 2 couldn't have been the code: it contradicts the feedback on guess 1.");
    }

    [Fact]
    public void Should_OnlyWarnAboutTheLatestGuess_When_AnEarlierOneWasTheContradiction()
    {
        var cut = RenderWith(new CoachNote(1, 500, null), new CoachNote(2, 192, 1), new CoachNote(3, 12, null));

        cut.FindAll(".coach-warning").Should().BeEmpty();
    }

    [Fact]
    public void Should_AnnounceUpdatesPolitely_When_Rendered()
    {
        var cut = RenderWith(new CoachNote(1, 256, null));

        cut.Find(".coach").GetAttribute("role").Should().Be("status");
    }
}
