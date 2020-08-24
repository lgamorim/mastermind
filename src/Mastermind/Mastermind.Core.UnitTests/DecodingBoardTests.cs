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

        [Fact]
        public void ShouldFindAllWhiteKeyPegsWhenAllCodeColorsMatchOtherShieldPosition()
        {
            //Arrange
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();
            decodingBoard.CodeMaker(shield);
            
            //Act
            var code = new[] {CodePeg.White, CodePeg.Green, CodePeg.Blue, CodePeg.Black};
            var result = decodingBoard.CodeBreaker(code);

            //Assert
            result.Should().NotBeNull();
            result.BlackKeyPegs.Should().Be(0);
            result.WhiteKeyPegs.Should().Be(colors.Length);
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
        [InlineData(new[]{CodePeg.Red, CodePeg.Black, CodePeg.Green, CodePeg.White})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.Red, CodePeg.Yellow, CodePeg.Red})]
        [InlineData(new[]{CodePeg.Red, CodePeg.Yellow, CodePeg.Red, CodePeg.Green})]
        [InlineData(new[]{CodePeg.Black, CodePeg.Blue, CodePeg.White, CodePeg.Red})]
        public void ShouldFindOneWhiteKeyPegWhenCodeColorMatchesSingleOtherShieldPosition(CodePeg[] code)
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
            result.WhiteKeyPegs.Should().Be(1);
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

        [Theory]
        [InlineData(new[]{CodePeg.Black, CodePeg.Black, CodePeg.Blue, CodePeg.Blue})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.Green, CodePeg.White, CodePeg.Blue})]
        [InlineData(new[]{CodePeg.Green, CodePeg.Black, CodePeg.Black, CodePeg.White})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.Blue, CodePeg.Black, CodePeg.Black})]
        public void ShouldFindTwoWhiteKeyPegsWhenCodeColorMatchesTwoOtherShieldPositions(CodePeg[] code)
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
            result.WhiteKeyPegs.Should().Be(2);
        }

        [Theory]
        [InlineData(new[]{CodePeg.Black, CodePeg.Red, CodePeg.Blue, CodePeg.Blue})]
        [InlineData(new[]{CodePeg.Red, CodePeg.Blue, CodePeg.Black, CodePeg.Black})]
        public void ShouldNotFindTwoWhiteKeyPegsWhenCodeColorMatchesSingleOtherShieldPosition(CodePeg[] code)
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
            result.WhiteKeyPegs.Should().NotBe(2);
        }

        [Theory]
        [InlineData(new[]{CodePeg.Black, CodePeg.White, CodePeg.Blue, CodePeg.Green})]
        [InlineData(new[]{CodePeg.Red, CodePeg.Black, CodePeg.White, CodePeg.Green})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.Black, CodePeg.White, CodePeg.Red})]
        [InlineData(new[]{CodePeg.Green, CodePeg.Black, CodePeg.Blue, CodePeg.White})]
        public void ShouldFindThreeWhiteKeyPegsWhenCodeColorMatchesThreeOtherShieldPositions(CodePeg[] code)
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
            result.WhiteKeyPegs.Should().Be(3);
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
        public void ShouldThrowArgumentExceptionWhenCodeBreakerCodeIsGreaterThanShield()
        {
            //Arrange
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();
            decodingBoard.CodeMaker(shield);

            //Act
            void Action() => decodingBoard.CodeBreaker(new CodePeg[colors.Length + 1]);
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

        [Fact]
        public void ShouldSolveSecretCodeWhenBlackPegsEqualsShieldCount()
        {
            //Arrange
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();
            decodingBoard.CodeMaker(shield);
            
            //Act
            var response = new Response(4, 0);
            var result = decodingBoard.HasSolvedSecretCode(response);

            //Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        [InlineData(3, 0)]
        public void ShouldNotSolveSecretCodeWhenBlackPegsNotEqualsShieldCount(int blackKeyPegs, int whiteKeyPegs)
        {
            //Arrange
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();
            decodingBoard.CodeMaker(shield);
            
            //Act
            var response = new Response(blackKeyPegs, whiteKeyPegs);
            var result = decodingBoard.HasSolvedSecretCode(response);

            //Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(5, 0)]
        [InlineData(0, 5)]
        [InlineData(4, 1)]
        [InlineData(1, 4)]
        public void ShouldThrowArgumentExceptionWhenTotalKeyPegsOutsideBoundaries(int blackKeyPegs, int whiteKeyPegs)
        {
            //Arrange
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            var decodingBoard = new DecodingBoard();
            decodingBoard.CodeMaker(shield);
            
            //Act
            var response = new Response(blackKeyPegs, whiteKeyPegs);
            void Action() => decodingBoard.HasSolvedSecretCode(response);
            var exception = Record.Exception(Action);

            //Assert
            exception.Should().BeOfType<ArgumentException>();
        }
    }
}