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

        [Fact]
        public void ShouldFindAllBlackKeyPegsWhenAllCodeColorsMatchShieldPosition()
        {
            //Arrange
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();
            decodingBoard.CodeMaker(shield);
            
            //Act
            var code = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var result = decodingBoard.CodeBreaker(code);

            //Assert
            result.Should().NotBeNull();
            result.BlackKeyPegs.Should().Be(colors.Length);
            result.WhiteKeyPegs.Should().Be(0);
        }

        [Theory]
        [InlineData(new[]{CodePeg.Black, CodePeg.Black, CodePeg.Black, CodePeg.Black})]
        [InlineData(new[]{CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Red})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.White, CodePeg.Green, CodePeg.Black})]
        [InlineData(new[]{CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White})]
        public void ShouldFindOneBlackKeyPegWhenCodeColorMatchesSingleShieldPosition(CodePeg[] code)
        {
            //Arrange
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();
            decodingBoard.CodeMaker(shield);
            
            //Act
            var result = decodingBoard.CodeBreaker(code);
            
            //Assert
            result.Should().NotBeNull();
            result.BlackKeyPegs.Should().Be(1);
        }

        [Theory]
        [InlineData(new[]{CodePeg.Black, CodePeg.Black, CodePeg.Blue, CodePeg.Blue})]
        [InlineData(new[]{CodePeg.Black, CodePeg.Green, CodePeg.White, CodePeg.Black})]
        [InlineData(new[]{CodePeg.Green, CodePeg.Blue, CodePeg.Blue, CodePeg.White})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.Blue, CodePeg.Black, CodePeg.Black})]
        public void ShouldFindTwoBlackKeyPegsWhenCodeColorMatchesTwoShieldPositions(CodePeg[] code)
        {
            //Arrange
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Blue, CodePeg.Black};
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();
            decodingBoard.CodeMaker(shield);
            
            //Act
            var result = decodingBoard.CodeBreaker(code);
            
            //Assert
            result.Should().NotBeNull();
            result.BlackKeyPegs.Should().Be(2);
        }

        [Fact]
        public void ShouldThrowArgumentExceptionWhenCodeBreakerCodeIsEmpty()
        {
            //Arrange
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();
            decodingBoard.CodeMaker(shield);

            //Act
            void Action() => decodingBoard.CodeBreaker(new CodePeg[0]);
            var exception = Record.Exception(Action);

            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void ShouldThrowArgumentNullExceptionWhenCodeBreakerCodeIsNull()
        {
            //Arrange
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();
            decodingBoard.CodeMaker(shield);

            //Act
            void Action() => decodingBoard.CodeBreaker(null);
            var exception = Record.Exception(Action);

            //Assert
            exception.Should().BeOfType<ArgumentNullException>();
        }
    }
}