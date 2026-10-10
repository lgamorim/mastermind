using FluentAssertions;
using Xunit;

namespace Mastermind.Core.UnitTests;

public class BoardConfigTests
{
    [Fact]
    public void Should_DescribeTheClassicBoard_When_StandardIsRead()
    {
        var boardConfig = BoardConfig.Standard;

        boardConfig.ShieldSize.Should().Be(4);
        boardConfig.TotalRows.Should().Be(10);
    }

    [Fact]
    public void Should_UseTheStandardBoard_When_ADecodingBoardIsCreatedWithoutAConfig()
    {
        var decodingBoard = new DecodingBoard();

        decodingBoard.BoardConfig.Should().Be(BoardConfig.Standard);
    }
}
