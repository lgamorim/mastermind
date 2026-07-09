using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Mastermind.Core;
using NSubstitute;
using Xunit;

namespace Mastermind.ConsoleApp.UnitTests;

public class ConsoleAppRunnerTests
{
    private static readonly CodePeg[] SecretCode = [CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Green];

    [Fact]
    public void Should_ShowAvailableCodePegColors_When_GameStarts()
    {
        var output = new StringWriter();
        var runner = CreateRunner(input: string.Empty, output: output);

        runner.Run([]);

        output.ToString().Should().Contain("[i] There are Code Pegs with 6 different colors:");
    }

    [Fact]
    public void Should_RequestSecretFromGenerator_When_GameStarts()
    {
        var secretCodeGenerator = CreateSecretCodeGenerator(SecretCode);
        var runner = new ConsoleAppRunner(secretCodeGenerator, new StringReader(string.Empty), new StringWriter(), new StringWriter());

        runner.Run([]);

        secretCodeGenerator.Received(1).Generate(4);
    }

    [Fact]
    public void Should_HideSecretCode_When_DebugArgumentIsNotProvided()
    {
        var output = new StringWriter();
        var runner = CreateRunner(input: string.Empty, output: output);

        runner.Run([]);

        output.ToString().Should().Contain("[X] [X] [X] [X]");
    }

    [Fact]
    public void Should_RevealSecretCode_When_DebugArgumentIsProvided()
    {
        var output = new StringWriter();
        var runner = CreateRunner(input: string.Empty, output: output);

        runner.Run(["DEBUG"]);

        output.ToString().Should().Contain("[^] The Code Maker has played.");
        output.ToString().Should().NotContain("[X] [X] [X] [X]");
    }

    [Fact]
    public void Should_DeclareCodeBreakerWins_When_GuessMatchesSecretCode()
    {
        var output = new StringWriter();
        var runner = CreateRunner(input: "Red Blue Yellow Green", output: output);

        var exitCode = runner.Run([]);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("[~] Code Breaker wins!");
    }

    [Fact]
    public void Should_DeclareCodeMakerWins_When_GuessesAreExhausted()
    {
        var wrongGuesses = string.Join(Environment.NewLine, Enumerable.Repeat("Red Red Red Red", 10));
        var output = new StringWriter();
        var runner = CreateRunner(input: wrongGuesses, output: output);

        var exitCode = runner.Run([]);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("[i] The secret code was:");
        output.ToString().Should().Contain("[^] Code Maker wins!");
    }

    [Fact]
    public void Should_PromptAgain_When_GuessHasWrongNumberOfColors()
    {
        var input = string.Join(Environment.NewLine, "Red Blue", "Red Blue Yellow Green");
        var output = new StringWriter();
        var runner = CreateRunner(input: input, output: output);

        runner.Run([]);

        output.ToString().Should().Contain("[!] The Code Breaker plays by typing 4 colors separated by a blank space.");
        output.ToString().Should().Contain("[~] Code Breaker wins!");
    }

    [Fact]
    public void Should_AcceptGuess_When_ColorsAreCaseInsensitive()
    {
        var output = new StringWriter();
        var runner = CreateRunner(input: "red blue YELLOW green", output: output);

        var exitCode = runner.Run([]);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("[~] Code Breaker wins!");
    }

    [Fact]
    public void Should_PromptAgain_When_GuessHasInvalidColor()
    {
        var input = string.Join(Environment.NewLine, "Red Blue Yellow Purple", "Red Blue Yellow Green");
        var output = new StringWriter();
        var runner = CreateRunner(input: input, output: output);

        runner.Run([]);

        output.ToString().Should().Contain("[!] The color Purple played is not a valid Code Peg color.");
        output.ToString().Should().Contain("[~] Code Breaker wins!");
    }

    [Fact]
    public void Should_ReprintGuessHistoryBoard_When_PlayingFurtherGuesses()
    {
        var input = string.Join(Environment.NewLine, "Red Red Red Red", "Red Blue Yellow Green");
        var output = new StringWriter();
        var runner = CreateRunner(input: input, output: output);

        runner.Run([]);

        // Collapse runs of spaces/tabs so the assertion is independent of the
        // board's column padding (the play echo uses single spaces).
        var text = System.Text.RegularExpressions.Regex.Replace(output.ToString(), "[ \t]+", " ");
        text.Should().Contain("Guess");
        text.Should().Contain("Result");
        // The first guess row is echoed once when played, then reprinted on the
        // history board for every subsequent render -> it must appear more than once.
        CountOccurrences(text, "[Red] [Red] [Red] [Red]").Should().BeGreaterThan(1);
    }

    [Fact]
    public void Should_AcceptGuess_When_InputHasExtraWhitespace()
    {
        var output = new StringWriter();
        var runner = CreateRunner(input: "  Red   Blue  Yellow Green ", output: output);

        var exitCode = runner.Run([]);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("[~] Code Breaker wins!");
    }

    [Fact]
    public void Should_EndGame_When_QuitCommandIsEntered()
    {
        var output = new StringWriter();
        var runner = CreateRunner(input: "/quit", output: output);

        var exitCode = runner.Run([]);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("[^] Code Maker wins!");
    }

    [Fact]
    public void Should_RejectBareQuit_When_NotPrefixedWithSlash()
    {
        var input = string.Join(Environment.NewLine, "quit", "Red Blue Yellow Green");
        var output = new StringWriter();
        var runner = CreateRunner(input: input, output: output);

        runner.Run([]);

        // A bare "quit" is a single token -> treated as a wrong-count guess, not a command.
        output.ToString().Should().Contain("[!] The Code Breaker plays by typing 4 colors separated by a blank space.");
        output.ToString().Should().Contain("[~] Code Breaker wins!");
    }

    [Fact]
    public void Should_ShowHelpWithoutConsumingAttempt_When_HelpCommandIsEntered()
    {
        var input = string.Join(Environment.NewLine, "/help", "Red Blue Yellow Green");
        var output = new StringWriter();
        var runner = CreateRunner(input: input, output: output);

        var exitCode = runner.Run([]);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("How to play");
        output.ToString().Should().Contain("[~] Code Breaker wins!");
    }

    [Fact]
    public void Should_PlayAnotherRound_When_PlayerChoosesToPlayAgain()
    {
        var input = string.Join(Environment.NewLine,
            "Red Blue Yellow Green", // win round 1
            "y",                     // play again
            "Red Blue Yellow Green", // win round 2
            "n");                    // stop
        var output = new StringWriter();
        var runner = CreateRunner(input: input, output: output);

        var exitCode = runner.Run([]);

        exitCode.Should().Be(0);
        CountOccurrences(output.ToString(), "[~] Code Breaker wins!").Should().Be(2);
    }

    [Fact]
    public void Should_ReportSessionScore_When_HistoryCommandIsEntered()
    {
        var input = string.Join(Environment.NewLine,
            "Red Blue Yellow Green", // win round 1
            "y",                     // play again
            "/history",              // ask for the session tally mid-round
            "Red Blue Yellow Green", // win round 2
            "n");
        var output = new StringWriter();
        var runner = CreateRunner(input: input, output: output);

        runner.Run([]);

        output.ToString().Should().Contain("Code Breaker wins: 1");
        output.ToString().Should().Contain("Code Maker wins: 0");
    }

    [Fact]
    public void Should_ReportZeroSessionScore_When_HistoryCommandIsEnteredBeforeAnyGameFinishes()
    {
        var input = string.Join(Environment.NewLine, "/history", "Red Blue Yellow Green", "n");
        var output = new StringWriter();
        var runner = CreateRunner(input: input, output: output);

        runner.Run([]);

        output.ToString().Should().Contain("Code Breaker wins: 0");
        output.ToString().Should().Contain("Code Maker wins: 0");
    }

    [Fact]
    public void Should_ShowBannerAndLegend_When_GameStarts()
    {
        var output = new StringWriter();
        var runner = CreateRunner(input: string.Empty, output: output);

        runner.Run([]);

        var text = output.ToString();
        text.Should().Contain("MASTERMIND");
        text.Should().Contain("right color in the right position");
    }

    [Fact]
    public void Should_EncourageRetry_When_CodeMakerWins()
    {
        var wrongGuesses = string.Join(Environment.NewLine, Enumerable.Repeat("Red Red Red Red", 10));
        var output = new StringWriter();
        var runner = CreateRunner(input: wrongGuesses, output: output);

        runner.Run([]);

        output.ToString().Should().Contain("Better luck next time");
    }

    [Fact]
    public void Should_EndGame_When_InputReachesEndOfStream()
    {
        var output = new StringWriter();
        var runner = CreateRunner(input: string.Empty, output: output);

        var exitCode = runner.Run([]);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("[^] Code Maker wins!");
    }

    [Fact]
    public void Should_WriteErrorAndReturnOne_When_SecretCodeGeneratorThrows()
    {
        var secretCodeGenerator = Substitute.For<ISecretCodeGenerator>();
        secretCodeGenerator.Generate(Arg.Any<int>()).Returns(_ => throw new InvalidOperationException("Generator failed"));

        var error = new StringWriter();
        var runner = new ConsoleAppRunner(secretCodeGenerator, new StringReader(string.Empty), new StringWriter(), error);

        var exitCode = runner.Run([]);

        exitCode.Should().Be(1);
        error.ToString().Trim().Should().Be("Generator failed");
    }

    private static ConsoleAppRunner CreateRunner(string input, TextWriter output)
    {
        return new ConsoleAppRunner(
            CreateSecretCodeGenerator(SecretCode),
            new StringReader(input),
            output,
            new StringWriter());
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static ISecretCodeGenerator CreateSecretCodeGenerator(CodePeg[] secretCode)
    {
        var secretCodeGenerator = Substitute.For<ISecretCodeGenerator>();
        secretCodeGenerator.Generate(Arg.Any<int>()).Returns(secretCode);

        return secretCodeGenerator;
    }
}
