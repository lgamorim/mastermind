using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using static Mastermind.Core.CodePeg;

namespace Mastermind.Core.UnitTests;

public class DailyPuzzleTests
{
    [Fact]
    public void Should_StartTheSeriesOnLaunchDay_When_TheFirstDateIsRead()
    {
        DailyPuzzle.FirstDate.Should().Be(new DateOnly(2026, 10, 26));
    }

    [Theory]
    [InlineData(2026, 10, 26, 1)]
    [InlineData(2026, 10, 27, 2)]
    [InlineData(2027, 10, 26, 366)]
    public void Should_NumberEachDayFromLaunch_When_ADateIsGiven(int year, int month, int day, int expected)
    {
        DailyPuzzle.NumberFor(new DateOnly(year, month, day)).Should().Be(expected);
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_TheDateIsBeforeLaunch()
    {
        var number = new Action(() => DailyPuzzle.NumberFor(new DateOnly(2026, 10, 25)));

        number.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    // Pinned values, computed independently (Python's hashlib over the same seed text): every
    // player's puzzle depends on this mapping, so it must never change by accident.
    public static TheoryData<DateOnly, CodePeg[]> KnownCodes => new()
    {
        { new DateOnly(2026, 10, 26), [Red, Green, Red, Yellow] },
        { new DateOnly(2026, 10, 27), [Red, Black, Black, Green] },
        { new DateOnly(2027, 1, 1), [White, Green, Yellow, Yellow] },
        { new DateOnly(2030, 6, 15), [Yellow, Blue, Blue, Red] },
    };

    [Theory]
    [MemberData(nameof(KnownCodes))]
    public void Should_DeriveTheKnownCode_When_ADateIsGiven(DateOnly date, CodePeg[] expected)
    {
        DailyPuzzle.SecretCodeFor(date).Should().Equal(expected);
    }

    [Fact]
    public void Should_GiveTheSameCode_When_TheSameDateIsAskedAgain()
    {
        var date = new DateOnly(2028, 2, 29);

        DailyPuzzle.SecretCodeFor(date).Should().Equal(DailyPuzzle.SecretCodeFor(date));
    }

    [Fact]
    public void Should_GiveFullCodesThatVaryFromDayToDay_When_AMonthOfDatesIsDerived()
    {
        var codes = Enumerable.Range(0, 30)
            .Select(offset => DailyPuzzle.SecretCodeFor(DailyPuzzle.FirstDate.AddDays(offset)))
            .ToList();

        codes.Should().OnlyContain(code => code.Length == BoardConfig.Standard.ShieldSize && code.All(Enum.IsDefined));
        codes.Select(code => string.Join(",", code)).Distinct().Should().HaveCountGreaterThan(25);
    }

    [Fact]
    public void Should_HandTheCallerItsOwnCopy_When_ACodeIsDerived()
    {
        var date = new DateOnly(2026, 10, 26);
        var first = DailyPuzzle.SecretCodeFor(date);

        first[0] = Black;

        DailyPuzzle.SecretCodeFor(date)[0].Should().Be(Red);
    }
}
