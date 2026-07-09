using System;
using FluentAssertions;
using Xunit;

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
}
