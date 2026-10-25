using System;
using FluentAssertions;
using Mastermind.WebApp.Services;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class GameStatsTests
{
    [Fact]
    public void Should_StartWithNothingRecorded_When_Empty()
    {
        var stats = GameStats.Empty;

        stats.GamesPlayed.Should().Be(0);
        stats.Wins.Should().Be(0);
        stats.CurrentStreak.Should().Be(0);
        stats.MaxStreak.Should().Be(0);
        stats.WinsByGuessCount.Should().HaveCount(10).And.OnlyContain(count => count == 0);
        stats.WinRatePercent.Should().Be(0);
    }

    [Fact]
    public void Should_CountTheWinAndExtendTheStreak_When_AWinIsRecorded()
    {
        var stats = GameStats.Empty.RecordWin(guessCount: 4);

        stats.GamesPlayed.Should().Be(1);
        stats.Wins.Should().Be(1);
        stats.CurrentStreak.Should().Be(1);
        stats.MaxStreak.Should().Be(1);
        stats.WinsByGuessCount.Should().Equal(0, 0, 0, 1, 0, 0, 0, 0, 0, 0);
    }

    [Fact]
    public void Should_ResetTheStreakButKeepTheBest_When_ALossFollowsWins()
    {
        var stats = GameStats.Empty.RecordWin(3).RecordWin(5).RecordLoss();

        stats.GamesPlayed.Should().Be(3);
        stats.Wins.Should().Be(2);
        stats.CurrentStreak.Should().Be(0);
        stats.MaxStreak.Should().Be(2);
    }

    [Fact]
    public void Should_RaiseTheBestStreak_When_TheCurrentStreakPassesIt()
    {
        var stats = GameStats.Empty.RecordWin(1).RecordLoss().RecordWin(2).RecordWin(2);

        stats.CurrentStreak.Should().Be(2);
        stats.MaxStreak.Should().Be(2);
        stats.WinsByGuessCount.Should().Equal(1, 2, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void Should_AcceptTheFirstAndLastRow_When_AWinIsRecorded(int guessCount)
    {
        var stats = GameStats.Empty.RecordWin(guessCount);

        stats.WinsByGuessCount[guessCount - 1].Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Should_ThrowArgumentOutOfRangeException_When_TheGuessCountIsOffTheBoard(int guessCount)
    {
        var record = () => GameStats.Empty.RecordWin(guessCount);

        record.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Should_LeaveTheOriginalUntouched_When_AWinIsRecorded()
    {
        var before = GameStats.Empty.RecordWin(2);

        before.RecordWin(2);

        before.Wins.Should().Be(1);
        before.WinsByGuessCount[1].Should().Be(1);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 2, 67)]
    [InlineData(3, 1, 33)]
    [InlineData(4, 4, 100)]
    [InlineData(8, 1, 13)]
    public void Should_RoundTheWinRateToAWholePercent_When_GamesWerePlayed(int played, int wins, int expected)
    {
        var stats = GameStats.Empty;
        for (var i = 0; i < wins; i++) stats = stats.RecordWin(1);
        for (var i = wins; i < played; i++) stats = stats.RecordLoss();

        stats.WinRatePercent.Should().Be(expected);
    }

    [Fact]
    public void Should_BeConsistent_When_BuiltByRecordingGames()
    {
        var stats = GameStats.Empty.RecordWin(3).RecordLoss().RecordWin(7).RecordWin(7);

        stats.IsConsistent().Should().BeTrue();
    }

    public static TheoryData<string, GameStats> InconsistentStats => new()
    {
        { "negative games", new GameStats(-1, 0, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0]) },
        { "more wins than games", new GameStats(1, 2, 0, 0, [2, 0, 0, 0, 0, 0, 0, 0, 0, 0]) },
        { "distribution too short", new GameStats(1, 1, 1, 1, [1, 0, 0]) },
        { "distribution does not add up to the wins", new GameStats(2, 2, 2, 2, [1, 0, 0, 0, 0, 0, 0, 0, 0, 0]) },
        { "negative bucket", new GameStats(2, 1, 1, 1, [2, -1, 0, 0, 0, 0, 0, 0, 0, 0]) },
        { "current streak above best", new GameStats(3, 3, 3, 2, [3, 0, 0, 0, 0, 0, 0, 0, 0, 0]) },
        { "best streak above wins", new GameStats(3, 1, 0, 2, [1, 0, 0, 0, 0, 0, 0, 0, 0, 0]) },
        { "missing distribution", new GameStats(0, 0, 0, 0, default) },
        { "negative current streak", new GameStats(1, 1, -1, 1, [1, 0, 0, 0, 0, 0, 0, 0, 0, 0]) },
        { "distribution too long", new GameStats(1, 1, 1, 1, [1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]) },
    };

    [Theory]
    [MemberData(nameof(InconsistentStats))]
    public void Should_NotBeConsistent_When_TheNumbersContradictEachOther(string scenario, GameStats stats)
    {
        stats.IsConsistent().Should().BeFalse(scenario);
    }
}
