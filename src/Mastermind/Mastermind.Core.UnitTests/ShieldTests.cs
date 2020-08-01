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
    }
}