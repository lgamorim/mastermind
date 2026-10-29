using System;
using System.Collections.Generic;
using System.Linq;

namespace Mastermind.Core;

/// <summary>
/// The code maker of Evil Mastermind: it never commits to a code. It keeps every code that fits
/// its answers so far and answers each guess in whatever way keeps the most of them, so the code
/// breaker wins only by narrowing the field to one code and then guessing it. Every answer fits
/// at least one remaining code, so the feedback never contradicts itself.
/// </summary>
public sealed class EvilCodeMaker : ICodeMaker
{
    private IReadOnlyList<CodePeg[]> _remaining = CodeSpace.All;

    /// <summary>
    /// The first remaining code, which fits every answer so far. It can change after each
    /// answer; once the game is over, it is the code the code maker settled on.
    /// </summary>
    public IReadOnlyList<CodePeg> SecretCode => Array.AsReadOnly((CodePeg[])_remaining[0].Clone());

    /// <summary>
    /// Groups the remaining codes by the answer each would give, keeps the largest group, and
    /// answers for it. Groups of equal size are broken by giving the fewest black pegs, then the
    /// fewest white, so the outcome is fixed for a given sequence of guesses.
    /// </summary>
    /// <param name="guess">The guess, one peg per shield hole.</param>
    /// <returns>The key pegs for the group of codes that was kept.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guess"/> is <see langword="null"/>.</exception>
    public Response Answer(IReadOnlyList<CodePeg> guess)
    {
        ArgumentNullException.ThrowIfNull(guess);

        var kept = _remaining
            .GroupBy(code => Response.Score(code, guess))
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.BlackKeyPegs)
            .ThenBy(group => group.Key.WhiteKeyPegs)
            .First();

        // A group keeps the codes in their original order, so the first remaining code (and with
        // it SecretCode) stays deterministic too.
        _remaining = [.. kept];
        return kept.Key;
    }
}
