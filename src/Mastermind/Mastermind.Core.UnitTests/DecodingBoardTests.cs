using System;
using FluentAssertions;
using Xunit;

namespace Mastermind.Core.UnitTests
{
    public class DecodingBoardTests
    {
        [Fact]
        public void ShouldCreateShieldWhenCodeMakerPlaysValidShield()
        {
            //Arrange
            const int count = 4;
            var colors = new CodePeg[count];
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();

            //Act
            decodingBoard.CodeMaker(shield);

            //Assert
            decodingBoard.Shield.Should().NotBeNull();
            decodingBoard.Shield.Should().Be(shield);
        }

        [Fact]
        public void ShouldThrowArgumentNullExceptionWhenCodeMakerShieldIsNull()
        {
            //Arrange
            var decodingBoard = new DecodingBoard();
            
            //Act
            void Action() => decodingBoard.CodeMaker(null);
            var exception = Record.Exception(Action);

            //Assert
            exception.Should().NotBeNull();
            exception.Should().BeOfType<ArgumentNullException>();
        }
    }
}