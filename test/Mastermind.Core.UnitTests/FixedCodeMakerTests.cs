using System;
using FluentAssertions;
using Xunit;
using static Mastermind.Core.CodePeg;

namespace Mastermind.Core.UnitTests;

public class FixedCodeMakerTests
{
    private static readonly CodePeg[] Secret = [Red, Blue, Yellow, Green];

    [Fact]
    public void Should_ScoreGuessesAgainstItsSecret_When_Answering()
    {
        var codeMaker = new FixedCodeMaker(Secret);

        var response = codeMaker.Answer([Red, Red, Blue, Blue]);

        response.Should().Be(Response.Score(Secret, [Red, Red, Blue, Blue]));
    }

    [Fact]
    public void Should_RevealTheCodeItWasGiven_When_TheSecretIsRead()
    {
        var codeMaker = new FixedCodeMaker(Secret);

        codeMaker.SecretCode.Should().Equal(Secret);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_TheSecretIsNull()
    {
        // null! deliberately breaks the non-nullable contract: rejecting null is what this test checks.
        var create = new Action(() => _ = new FixedCodeMaker(null!));

        create.Should().ThrowExactly<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void Should_ThrowArgumentException_When_TheSecretHasTheWrongLength(int length)
    {
        var create = new Action(() => _ = new FixedCodeMaker(new CodePeg[length]));

        create.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Should_ThrowArgumentNullExceptionForTheGuess_When_TheGuessIsNull()
    {
        var codeMaker = new FixedCodeMaker(Secret);

        // null! deliberately breaks the non-nullable contract: rejecting null is what this test checks.
        var answer = new Action(() => codeMaker.Answer(null!));

        answer.Should().ThrowExactly<ArgumentNullException>().WithParameterName("guess");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void Should_ThrowArgumentException_When_TheGuessHasTheWrongLength(int length)
    {
        var codeMaker = new FixedCodeMaker(Secret);

        var answer = new Action(() => codeMaker.Answer(new CodePeg[length]));

        answer.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Should_KeepItsOwnCopy_When_TheCallersArrayChangesLater()
    {
        var secret = new[] { Red, Blue, Yellow, Green };
        var codeMaker = new FixedCodeMaker(secret);

        secret[0] = Black;

        codeMaker.SecretCode.Should().Equal(Red, Blue, Yellow, Green);
        codeMaker.Answer([Red, Blue, Yellow, Green]).Should().Be(new Response(4, 0));
    }

    [Fact]
    public void Should_RejectMutation_When_TheSecretIsCastBackToAnArray()
    {
        var codeMaker = new FixedCodeMaker(Secret);

        var mutate = new Action(() => ((CodePeg[])codeMaker.SecretCode)[0] = Black);

        mutate.Should().Throw<InvalidCastException>();
    }
}
