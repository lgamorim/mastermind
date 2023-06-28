using System;
using FluentAssertions;
using Xunit;

namespace Mastermind.Core.UnitTests;

public class ShieldTests
{
    [Fact]
    public void Should_AccessLowerIndex_When_ShieldHasColors()
    {
        const int count = 4;
        var colors = new CodePeg[count];

        var shield = new Shield(colors);

        shield[0].Should().NotBeNull();
        shield[0].Should().BeOfType<CodePeg>();
    }

    [Fact]
    public void Should_AccessUpperIndex_When_ShieldHasColors()
    {
        const int count = 4;
        var colors = new CodePeg[count];

        var shield = new Shield(colors);

        shield[count - 1].Should().NotBeNull();
        shield[count - 1].Should().BeOfType<CodePeg>();
    }

    [Fact]
    public void Should_MatchHolesCount_When_ShieldHasColors()
    {
        const int count = 4;
        var colors = new CodePeg[count];

        var shield = new Shield(colors);

        shield.Count.Should().Be(count);
    }

    [Fact]
    public void Should_MatchIndexColor_When_ShieldHasColors()
    {
        var colors = new[]
        {
            CodePeg.Black,
            CodePeg.Blue,
            CodePeg.Green,
            CodePeg.White
        };

        var shield = new Shield(colors);

        shield[0].Should().Be(CodePeg.Black);
        shield[1].Should().Be(CodePeg.Blue);
        shield[2].Should().Be(CodePeg.Green);
        shield[3].Should().Be(CodePeg.White);
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
        var action = new Action(() => new Shield(null));

        action.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0, CodePeg.Black)]
    [InlineData(1, CodePeg.Blue)]
    [InlineData(2, CodePeg.Green)]
    [InlineData(3, CodePeg.White)]
    public void Should_HaveColorAtIndex_When_CodePegMatchesShieldColor(int index, CodePeg color)
    {
        var colors = new[]
        {
            CodePeg.Black,
            CodePeg.Blue,
            CodePeg.Green,
            CodePeg.White
        };
        var shield = new Shield(colors);
            
        var result = shield.HasColorAt(index, color);

        result.Should().Be(true);
    }

    [Theory]
    [InlineData(0, CodePeg.Blue)]
    [InlineData(0, CodePeg.Green)]
    [InlineData(0, CodePeg.Red)]
    [InlineData(0, CodePeg.White)]
    [InlineData(0, CodePeg.Yellow)]
    public void Should_NotHaveColorAtIndex_When_ComparedToAllOtherCodePegs(int index, CodePeg color)
    {
        var colors = new[]
        {
            CodePeg.Black,
            CodePeg.Blue,
            CodePeg.Green,
            CodePeg.White
        };
        var shield = new Shield(colors);
            
        var result = shield.HasColorAt(index, color);

        result.Should().Be(false);
    }
}