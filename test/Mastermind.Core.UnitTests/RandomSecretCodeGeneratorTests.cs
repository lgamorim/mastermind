using System;
using FluentAssertions;
using Xunit;

namespace Mastermind.Core.UnitTests;

public class RandomSecretCodeGeneratorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(6)]
    public void Should_GenerateCodeOfRequestedSize_When_SizeIsPositive(int size)
    {
        var generator = new RandomSecretCodeGenerator();

        var code = generator.Generate(size);

        code.Should().HaveCount(size);
    }

    [Fact]
    public void Should_GenerateOnlyValidCodePegColors_When_CodeIsGenerated()
    {
        var generator = new RandomSecretCodeGenerator();

        var code = generator.Generate(100);

        code.Should().OnlyContain(peg => Enum.IsDefined(peg));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Should_ThrowArgumentOutOfRangeException_When_SizeIsNegative(int size)
    {
        var generator = new RandomSecretCodeGenerator();

        var action = new Action(() => generator.Generate(size));

        action.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Should_ReturnEmptyCode_When_SizeIsZero()
    {
        var generator = new RandomSecretCodeGenerator();

        var code = generator.Generate(0);

        code.Should().BeEmpty();
    }
}
