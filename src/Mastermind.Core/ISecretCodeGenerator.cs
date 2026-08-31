namespace Mastermind.Core;

/// <summary>Generates the secret code the code breaker must guess.</summary>
public interface ISecretCodeGenerator
{
    /// <summary>Generates a secret code of the requested length.</summary>
    /// <param name="size">Number of pegs in the code; must not be negative.</param>
    /// <returns>The generated code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
    CodePeg[] Generate(int size);
}
