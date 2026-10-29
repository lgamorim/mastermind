using System;
using System.Collections.Generic;
using System.Linq;

namespace Mastermind.Core;

/// <summary>
/// Reviews the code breaker's guesses on the <see cref="BoardConfig.Standard"/> board: how many
/// codes still fit the feedback after each guess, and whether a guess contradicted earlier
/// feedback. It never reveals which codes remain, only how many.
/// </summary>
public static class Coach
{
    /// <summary>How many codes the code maker can choose from.</summary>
    public static int TotalCodes => CodeSpace.All.Count;

    /// <summary>Reviews a game's guesses in the order they were played.</summary>
    /// <param name="history">The played guesses with the responses they earned, oldest first.</param>
    /// <returns>One note per guess, in the same order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="history"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A guess in <paramref name="history"/> does not have one peg per shield hole.</exception>
    public static IReadOnlyList<CoachNote> Review(IReadOnlyList<GuessRecord> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        var candidates = CodeSpace.All.AsEnumerable();
        var notes = new List<CoachNote>(history.Count);

        for (var played = 0; played < history.Count; played++)
        {
            var record = history[played];

            // The guess could have been the code only if, taking it as the secret, every earlier
            // guess would have earned exactly the response it did.
            var contradicted = history
                .Take(played)
                .FirstOrDefault(earlier => Response.Score(record.Guess, earlier.Guess) != earlier.Response);

            candidates = candidates.Where(code => Response.Score(code, record.Guess) == record.Response).ToList();
            notes.Add(new CoachNote(record.Attempt, candidates.Count(), contradicted?.Attempt));
        }

        return notes.AsReadOnly();
    }
}
