using System;
using FluentAssertions;
using Xunit;

namespace Mastermind.Core.UnitTests
{
    public class DecodingBoardTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, -1)]
        public void Should_ThrowArgumentException_When_BoardConfigIsInvalid(int shieldSize, int totalRows)
        {
            var boardConfig = new BoardConfig(shieldSize, totalRows);

            var action = new Action(() => new DecodingBoard(boardConfig));

            action.Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void Should_CreateShield_When_CodeMakerPlaysValidShield()
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new CodePeg[4];
            var shield = new Shield(colors);

            decodingBoard.PlayCodeMaker(shield);

            decodingBoard.Shield.Should().NotBeNull();
            decodingBoard.Shield.Should().Be(shield);
        }

        [Fact]
        public void Should_ThrowArgumentNullException_When_CodeMakerShieldIsNull()
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            void Action() => decodingBoard.PlayCodeMaker(null);
            var exception = Record.Exception(Action);

            exception.Should().NotBeNull();
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [InlineData(3)]
        [InlineData(5)]
        public void Should_ThrowArgumentException_When_CodeMakerShieldIsDifferentThanConfig(int shieldSize)
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new CodePeg[shieldSize];
            var shield = new Shield(colors);

            void Action() => decodingBoard.PlayCodeMaker(shield);
            var exception = Record.Exception(Action);

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Should_FindAllBlackKeyPegs_When_AllCodeColorsMatchShieldPosition()
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var code = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            
            var result = decodingBoard.PlayCodeBreaker(code);

            result.Should().NotBeNull();
            result.BlackKeyPegs.Should().Be(colors.Length);
            result.WhiteKeyPegs.Should().Be(0);
        }

        [Fact]
        public void Should_FindAllWhiteKeyPegs_When_AllCodeColorsMatchOtherShieldPosition()
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var code = new[] {CodePeg.White, CodePeg.Green, CodePeg.Blue, CodePeg.Black};
            
            var result = decodingBoard.PlayCodeBreaker(code);

            result.Should().NotBeNull();
            result.BlackKeyPegs.Should().Be(0);
            result.WhiteKeyPegs.Should().Be(colors.Length);
        }

        [Theory]
        [InlineData(new[]{CodePeg.Black, CodePeg.Black, CodePeg.Black, CodePeg.Black})]
        [InlineData(new[]{CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Red})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.White, CodePeg.Green, CodePeg.Black})]
        [InlineData(new[]{CodePeg.White, CodePeg.White, CodePeg.White, CodePeg.White})]
        public void Should_FindOneBlackKeyPeg_When_CodeColorMatchesSingleShieldPosition(CodePeg[] code)
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var result = decodingBoard.PlayCodeBreaker(code);
            
            result.Should().NotBeNull();
            result.BlackKeyPegs.Should().Be(1);
        }

        [Theory]
        [InlineData(new[]{CodePeg.Red, CodePeg.Black, CodePeg.Green, CodePeg.White})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.Red, CodePeg.Yellow, CodePeg.Red})]
        [InlineData(new[]{CodePeg.Red, CodePeg.Yellow, CodePeg.Red, CodePeg.Green})]
        [InlineData(new[]{CodePeg.Black, CodePeg.Blue, CodePeg.White, CodePeg.Red})]
        public void Should_FindOneWhiteKeyPeg_When_CodeColorMatchesSingleOtherShieldPosition(CodePeg[] code)
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var result = decodingBoard.PlayCodeBreaker(code);
            
            result.Should().NotBeNull();
            result.WhiteKeyPegs.Should().Be(1);
        }

        [Theory]
        [InlineData(new[]{CodePeg.Black, CodePeg.Black, CodePeg.Blue, CodePeg.Blue})]
        [InlineData(new[]{CodePeg.Black, CodePeg.Green, CodePeg.White, CodePeg.Black})]
        [InlineData(new[]{CodePeg.Green, CodePeg.Blue, CodePeg.Blue, CodePeg.White})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.Blue, CodePeg.Black, CodePeg.Black})]
        public void Should_FindTwoBlackKeyPegs_When_CodeColorMatchesTwoShieldPositions(CodePeg[] code)
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Blue, CodePeg.Black};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var result = decodingBoard.PlayCodeBreaker(code);
            
            result.Should().NotBeNull();
            result.BlackKeyPegs.Should().Be(2);
        }

        [Theory]
        [InlineData(new[]{CodePeg.Black, CodePeg.Black, CodePeg.Blue, CodePeg.Blue})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.Green, CodePeg.White, CodePeg.Blue})]
        [InlineData(new[]{CodePeg.Green, CodePeg.Black, CodePeg.Black, CodePeg.White})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.Blue, CodePeg.Black, CodePeg.Black})]
        public void Should_FindTwoWhiteKeyPegs_When_CodeColorMatchesTwoOtherShieldPositions(CodePeg[] code)
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Blue, CodePeg.Black};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var result = decodingBoard.PlayCodeBreaker(code);
            
            result.Should().NotBeNull();
            result.WhiteKeyPegs.Should().Be(2);
        }

        [Theory]
        [InlineData(new[]{CodePeg.Black, CodePeg.Red, CodePeg.Blue, CodePeg.Blue})]
        [InlineData(new[]{CodePeg.Red, CodePeg.Blue, CodePeg.Black, CodePeg.Black})]
        public void Should_NotFindTwoWhiteKeyPegs_When_CodeColorMatchesSingleOtherShieldPosition(CodePeg[] code)
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var result = decodingBoard.PlayCodeBreaker(code);
            
            result.Should().NotBeNull();
            result.WhiteKeyPegs.Should().NotBe(2);
        }

        [Theory]
        [InlineData(new[]{CodePeg.Black, CodePeg.White, CodePeg.Blue, CodePeg.Green})]
        [InlineData(new[]{CodePeg.Red, CodePeg.Black, CodePeg.White, CodePeg.Green})]
        [InlineData(new[]{CodePeg.Blue, CodePeg.Black, CodePeg.White, CodePeg.Red})]
        [InlineData(new[]{CodePeg.Green, CodePeg.Black, CodePeg.Blue, CodePeg.White})]
        public void Should_FindThreeWhiteKeyPegs_When_CodeColorMatchesThreeOtherShieldPositions(CodePeg[] code)
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var result = decodingBoard.PlayCodeBreaker(code);
            
            result.Should().NotBeNull();
            result.WhiteKeyPegs.Should().Be(3);
        }

        [Fact]
        public void Should_ThrowArgumentException_When_CodeBreakerCodeIsEmpty()
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);

            void Action() => decodingBoard.PlayCodeBreaker(new CodePeg[0]);
            var exception = Record.Exception(Action);

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Should_ThrowArgumentException_When_CodeBreakerCodeIsGreaterThanShield()
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);

            void Action() => decodingBoard.PlayCodeBreaker(new CodePeg[colors.Length + 1]);
            var exception = Record.Exception(Action);

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Should_ThrowArgumentNullException_When_CodeBreakerCodeIsNull()
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);

            void Action() => decodingBoard.PlayCodeBreaker(null);
            var exception = Record.Exception(Action);

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Should_SolveSecretCode_When_ResponseBlackPegsEqualsShieldCount()
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var response = new Response(4, 0);
            
            var result = decodingBoard.HasCodeBreakerSolvedSecretCode(response);

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        [InlineData(3, 0)]
        public void Should_NotSolveSecretCode_When_ResponseBlackPegsNotEqualsShieldCount(int blackKeyPegs, int whiteKeyPegs)
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var response = new Response(blackKeyPegs, whiteKeyPegs);
            
            var result = decodingBoard.HasCodeBreakerSolvedSecretCode(response);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(5, 0)]
        [InlineData(0, 5)]
        [InlineData(4, 1)]
        [InlineData(1, 4)]
        public void Should_ThrowArgumentException_When_ResponseTotalKeyPegsOutsideBoundaries(int blackKeyPegs, int whiteKeyPegs)
        {
            var boardConfig = new BoardConfig(4, 10);
            var decodingBoard = new DecodingBoard(boardConfig);
            
            var colors = new[] {CodePeg.Black, CodePeg.Blue, CodePeg.Green, CodePeg.White};
            var shield = new Shield(colors);
            decodingBoard.PlayCodeMaker(shield);
            
            var response = new Response(blackKeyPegs, whiteKeyPegs);
            
            void Action() => decodingBoard.HasCodeBreakerSolvedSecretCode(response);
            var exception = Record.Exception(Action);

            exception.Should().BeOfType<ArgumentException>();
        }
    }
}