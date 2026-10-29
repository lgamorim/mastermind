using System.Collections.Generic;

namespace Mastermind.Core;

/// <summary>The side of the board that answers the code breaker's guesses.</summary>
public interface ICodeMaker
{
    /// <summary>
    /// A code that fits every answer given so far. For a code maker that chose its code at
    /// the start, it is that code; one that decides as it goes may change it after each answer.
    /// </summary>
    IReadOnlyList<CodePeg> SecretCode { get; }

    /// <summary>Answers a guess with key pegs.</summary>
    /// <param name="guess">The guess, one peg per shield hole; <see cref="Game"/> checks its length before asking.</param>
    /// <returns>The black and white key pegs the guess earns.</returns>
    Response Answer(IReadOnlyList<CodePeg> guess);
}
