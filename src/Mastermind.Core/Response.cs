using System;
using System.Collections.Generic;

namespace Mastermind.Core;

/// <summary>The scoring of a single guess, expressed as black and white key pegs.</summary>
public readonly record struct Response
{
    /// <summary>Creates a response.</summary>
    /// <param name="blackKeyPegs">Count of correct colors in the correct positions; must not be negative.</param>
    /// <param name="whiteKeyPegs">Count of correct colors in the wrong positions; must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">A peg count is negative.</exception>
    public Response(int blackKeyPegs, int whiteKeyPegs)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(blackKeyPegs);
        ArgumentOutOfRangeException.ThrowIfNegative(whiteKeyPegs);

        BlackKeyPegs = blackKeyPegs;
        WhiteKeyPegs = whiteKeyPegs;
    }

    /// <summary>Count of correct colors in the correct positions.</summary>
    public int BlackKeyPegs { get; }

    /// <summary>Count of correct colors in the wrong positions.</summary>
    public int WhiteKeyPegs { get; }

    /// <summary>Scores a guess against a secret code, the way the code maker answers it.</summary>
    /// <param name="secretCode">The code being guessed.</param>
    /// <param name="guess">The guess; it must have as many pegs as <paramref name="secretCode"/>.</param>
    /// <returns>
    /// One black key peg per peg of the right color in the right position, and one white key
    /// peg per remaining right color in the wrong position; each secret peg earns at most one.
    /// </returns>
    /// <exception cref="ArgumentNullException">Either code is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The codes differ in length.</exception>
    public static Response Score(IReadOnlyList<CodePeg> secretCode, IReadOnlyList<CodePeg> guess)
    {
        ArgumentNullException.ThrowIfNull(secretCode);
        ArgumentNullException.ThrowIfNull(guess);
        if (guess.Count != secretCode.Count)
            throw new ArgumentException($"Code must have exactly {secretCode.Count} pegs.", nameof(guess));

        var colorCount = Enum.GetValues<CodePeg>().Length;
        var secretCounts = new int[colorCount];
        var guessCounts = new int[colorCount];

        var blackKeyPegs = 0;
        for (var i = 0; i < guess.Count; i++)
        {
            if (secretCode[i] == guess[i])
            {
                blackKeyPegs++;
            }
            else
            {
                secretCounts[(int)secretCode[i]]++;
                guessCounts[(int)guess[i]]++;
            }
        }

        var whiteKeyPegs = 0;
        for (var i = 0; i < colorCount; i++)
        {
            whiteKeyPegs += Math.Min(secretCounts[i], guessCounts[i]);
        }

        return new Response(blackKeyPegs, whiteKeyPegs);
    }
}
