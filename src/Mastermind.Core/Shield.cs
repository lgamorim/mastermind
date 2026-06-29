using System;

namespace Mastermind.Core;

public class Shield
{
    private readonly CodePeg[] colors;

    public Shield(CodePeg[] colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Length == 0)
            throw new ArgumentException("Shield must contain at least one peg.", nameof(colors));
        this.colors = (CodePeg[])colors.Clone();
    }

    public CodePeg this[int index] => colors[index];

    public int Count => colors.Length;

    public bool HasColorAt(int index, CodePeg color)
    {
        return this[index] == color;
    }
}