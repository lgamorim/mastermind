using System.Collections.Generic;

namespace Mastermind.Core;

/// <summary>One played row of a <see cref="Game"/>: the guess and how the code maker scored it.</summary>
/// <param name="Attempt">One-based number of the guess within its game.</param>
/// <param name="Guess">The code pegs the code breaker played.</param>
/// <param name="Response">The key pegs the guess earned.</param>
public sealed record GuessRecord(int Attempt, IReadOnlyList<CodePeg> Guess, Response Response);
