using System;

namespace Mastermind.Core;

public class Shield
{
    private readonly CodePeg[] colors;

    public Shield(CodePeg[] colors)
    {
        if (colors is null) throw new ArgumentNullException(nameof(colors));
        if (colors.Length == 0) throw new ArgumentException(nameof(colors));
        this.colors = colors;
    }

    public CodePeg this[int index] => colors[index];

    public int Count => colors.Length;

    public bool HasColorAt(int index, CodePeg color)
    {
        return this[index] == color;
    }
}