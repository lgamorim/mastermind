using System;
using FluentAssertions;
using Xunit;

namespace Mastermind.Core.UnitTests
{
    public class ShieldTests
    {
        [Fact]
        public void ShouldAccessLowerIndexWhenShieldHasColors()
        {
            //Arrange
            const int count = 4;
            var colors = new CodePeg[count];

            //Act
            var shield = new Shield(colors);

            //Assert
            shield[0].Should().NotBeNull();
            shield[0].Should().BeOfType<CodePeg>();
        }

        [Fact]
        public void ShouldAccessUpperIndexWhenShieldHasColors()
        {
            //Arrange
            const int count = 4;
            var colors = new CodePeg[count];

            //Act
            var shield = new Shield(colors);

            //Assert
            shield[count - 1].Should().NotBeNull();
            shield[count - 1].Should().BeOfType<CodePeg>();
        }

        [Fact]
        public void ShouldMatchHolesCountWhenShieldHasColors()
        {
            //Arrange
            const int count = 4;
            var colors = new CodePeg[count];

            //Act
            var shield = new Shield(colors);

            //Assert
            shield.Count.Should().Be(count);
        }

        [Fact]
        public void ShouldMatchIndexColorWhenShieldHasColors()
        {
            //Arrange
            var colors = new[]
            {
                CodePeg.Black,
                CodePeg.Blue,
                CodePeg.Green,
                CodePeg.White
            };

            //Act
            var shield = new Shield(colors);

            //Assert
            shield[0].Should().Be(CodePeg.Black);
            shield[1].Should().Be(CodePeg.Blue);
            shield[2].Should().Be(CodePeg.Green);
            shield[3].Should().Be(CodePeg.White);
        }

        [Fact]
        public void ShouldThrowArgumentExceptionWhenColorsIsEmpty()
        {
            //Arrange
            var colors = new CodePeg[0];

            //Act
            var action = new Action(() => new Shield(colors));

            //Assert
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ShouldThrowArgumentNullExceptionWhenColorsIsNull()
        {
            //Arrange

            //Act
            var action = new Action(() => new Shield(null));

            //Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(0, CodePeg.Black)]
        [InlineData(1, CodePeg.Blue)]
        [InlineData(2, CodePeg.Green)]
        [InlineData(3, CodePeg.White)]
        public void ShouldHaveColorAtIndexWhenCodePegMatchesShieldColor(int index, CodePeg color)
        {
            //Arrange
            var colors = new[]
            {
                CodePeg.Black,
                CodePeg.Blue,
                CodePeg.Green,
                CodePeg.White
            };
            var shield = new Shield(colors);
            
            //Act
            var result = shield.HasColorAt(index, color);

            //Assert
            result.Should().Be(true);
        }

        [Theory]
        [InlineData(0, CodePeg.Blue)]
        [InlineData(0, CodePeg.Green)]
        [InlineData(0, CodePeg.Red)]
        [InlineData(0, CodePeg.White)]
        [InlineData(0, CodePeg.Yellow)]
        public void ShouldNotHaveColorAtIndexWhenComparedToAllOtherCodePegs(int index, CodePeg color)
        {
            //Arrange
            var colors = new[]
            {
                CodePeg.Black,
                CodePeg.Blue,
                CodePeg.Green,
                CodePeg.White
            };
            var shield = new Shield(colors);
            
            //Act
            var result = shield.HasColorAt(index, color);

            //Assert
            result.Should().Be(false);
        }
    }
}