using System;
using System.Collections.Generic;

namespace Mastermind.Core;

// Every code on the standard board, generated once: 1,296 for 6 colors in 4 holes, small enough
// to filter in full after every guess. The order is fixed, counting in base 6 with the first hole
// as the most significant digit (Red Red Red Red first), so anything that picks "the first code
// that fits" is deterministic.
internal static class CodeSpace
{
    public static IReadOnlyList<CodePeg[]> All { get; } = Enumerate();

    private static CodePeg[][] Enumerate()
    {
        var colors = Enum.GetValues<CodePeg>();
        var length = BoardConfig.Standard.ShieldSize;
        var count = (int)Math.Pow(colors.Length, length);

        var codes = new CodePeg[count][];
        for (var index = 0; index < count; index++)
        {
            // Read the index as a number in base colors.Length, one digit per hole.
            var code = new CodePeg[length];
            var rest = index;
            for (var hole = length - 1; hole >= 0; hole--)
            {
                code[hole] = colors[rest % colors.Length];
                rest /= colors.Length;
            }

            codes[index] = code;
        }

        return codes;
    }
}
