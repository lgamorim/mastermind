using System;

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
}
