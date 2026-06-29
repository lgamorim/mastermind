using Mastermind.Core;

namespace Mastermind.ConsoleApp;

public sealed class RandomSecretCodeGenerator : ISecretCodeGenerator
{
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
