using System;

namespace Mastermind.Core;

/// <summary>The secret code placed by the code maker, hidden behind the board's shield.</summary>
public class Shield
{
    private readonly CodePeg[] colors;

    /// <summary>Creates a shield from a copy of the supplied colors.</summary>
    /// <param name="colors">The secret code; must contain at least one peg.</param>
    /// <exception cref="ArgumentNullException"><paramref name="colors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="colors"/> is empty.</exception>
    public Shield(CodePeg[] colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Length == 0)
            throw new ArgumentException("Shield must contain at least one peg.", nameof(colors));
        this.colors = (CodePeg[])colors.Clone();
    }

    /// <summary>Gets the color at the given position.</summary>
    /// <param name="index">Zero-based peg position.</param>
    public CodePeg this[int index] => colors[index];

    /// <summary>Gets the number of pegs in the shield.</summary>
    public int Count => colors.Length;

    /// <summary>Determines whether the peg at <paramref name="index"/> is the given color.</summary>
    /// <param name="index">Zero-based peg position.</param>
    /// <param name="color">Color to compare against.</param>
    /// <returns><see langword="true"/> when the colors match; otherwise <see langword="false"/>.</returns>
    public bool HasColorAt(int index, CodePeg color)
    {
        return this[index] == color;
    }
}
