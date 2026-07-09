using System;

namespace Mastermind.Core;

/// <summary>Generates a secret code by picking each peg color uniformly at random.</summary>
public sealed class RandomSecretCodeGenerator : ISecretCodeGenerator
{
    /// <inheritdoc />
    public CodePeg[] Generate(int size)
    {
        var colors = Enum.GetValues<CodePeg>();
        var pattern = new CodePeg[size];

        for (var i = 0; i < size; i++)
        {
            pattern[i] = colors[Random.Shared.Next(0, colors.Length)];
        }

        return pattern;
    }
}
