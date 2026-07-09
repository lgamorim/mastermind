namespace Mastermind.Core;

/// <summary>Generates the secret code the code breaker must guess.</summary>
public interface ISecretCodeGenerator
{
    /// <summary>Generates a secret code of the requested length.</summary>
    /// <param name="size">Number of pegs in the code.</param>
    /// <returns>The generated code.</returns>
    CodePeg[] Generate(int size);
}
