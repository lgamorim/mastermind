using System;
using FluentAssertions;
using Xunit;
using static Mastermind.Core.CodePeg;

namespace Mastermind.Core.UnitTests;

public class ShieldTests
{
    private static readonly CodePeg[] Colors = [Black, Blue, Green, White];

    [Theory]
    [InlineData(0, Black)]
    [InlineData(1, Blue)]
    [InlineData(2, Green)]
    [InlineData(3, White)]
    public void Should_ReturnColorAtIndex_When_ShieldHasColors(int index, CodePeg expected)
    {
        var shield = new Shield(Colors);

        shield[index].Should().Be(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Should_Throw_When_IndexIsOutOfRange(int index)
    {
        var shield = new Shield(Colors);

        var action = new Action(() => _ = shield[index]);

        action.Should().Throw<IndexOutOfRangeException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(6)]
    public void Should_MatchHolesCount_When_ShieldHasColors(int count)
    {
        var colors = new CodePeg[count];

        var shield = new Shield(colors);

        shield.Count.Should().Be(count);
    }

    [Fact]
    public void Should_ThrowArgumentException_When_ColorsIsEmpty()
    {
        var colors = new CodePeg[0];

        var action = new Action(() => new Shield(colors));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_ColorsIsNull()
    {
        var action = new Action(() => new Shield(null!));

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_NotReflectSourceArrayMutation_When_ColorsArrayIsModifiedAfterConstruction()
    {
        var colors = new[] { Black, Blue, Green, White };
        var shield = new Shield(colors);

        colors[0] = Red;

        shield[0].Should().Be(Black);
    }

    [Theory]
    [InlineData(0, Black)]
    [InlineData(1, Blue)]
    [InlineData(2, Green)]
    [InlineData(3, White)]
    public void Should_HaveColorAtIndex_When_CodePegMatchesShieldColor(int index, CodePeg color)
    {
        var shield = new Shield(Colors);

        shield.HasColorAt(index, color).Should().BeTrue();
    }

    [Theory]
    [InlineData(0, Blue)]
    [InlineData(0, Red)]
    [InlineData(1, Black)]
    [InlineData(2, White)]
    [InlineData(3, Green)]
    [InlineData(3, Yellow)]
    public void Should_NotHaveColorAtIndex_When_CodePegDoesNotMatchShieldColor(int index, CodePeg color)
    {
        var shield = new Shield(Colors);

        shield.HasColorAt(index, color).Should().BeFalse();
    }
}
