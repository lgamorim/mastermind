using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Mastermind.Core;

/// <summary>
/// The daily puzzle: one secret code per calendar date, the same for every player, numbered
/// from the day the series started.
/// </summary>
public static class DailyPuzzle
{
    /// <summary>The date of puzzle number 1.</summary>
    public static DateOnly FirstDate { get; } = new(2026, 10, 26);

    /// <summary>Numbers a date's puzzle, counting <see cref="FirstDate"/> as 1.</summary>
    /// <param name="date">The puzzle's date; not before <see cref="FirstDate"/>.</param>
    /// <returns>The puzzle number.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="date"/> is before <see cref="FirstDate"/>.</exception>
    public static int NumberFor(DateOnly date)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(date, FirstDate);

        return date.DayNumber - FirstDate.DayNumber + 1;
    }

    /// <summary>Derives a date's secret code.</summary>
    /// <param name="date">The puzzle's date.</param>
    /// <returns>A new array holding the code, one peg per hole of the standard board.</returns>
    /// <remarks>
    /// The code comes from a SHA-256 hash of the date rather than from <see cref="Random"/>,
    /// whose sequence for a seed is not guaranteed across .NET versions: every player, on any
    /// runtime, must get the same code for the same date. Anyone reading this source can
    /// work out a day's code; that is accepted for a game like this one.
    /// </remarks>
    public static CodePeg[] SecretCodeFor(DateOnly date)
    {
        var seed = $"mastermind-daily:{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var colors = Enum.GetValues<CodePeg>();
        var length = BoardConfig.Standard.ShieldSize;

        // The hash's first 8 bytes pick one of the board's codes (1,296 for 6 colors in 4
        // holes), which is then spelled out as one base-6 digit per hole. Reducing a 64-bit
        // value this way favors some codes by about 1 in 10^16, far too little to matter.
        var codeCount = (ulong)Math.Pow(colors.Length, length);
        var index = BinaryPrimitives.ReadUInt64BigEndian(hash) % codeCount;

        var code = new CodePeg[length];
        for (var hole = length - 1; hole >= 0; hole--)
        {
            code[hole] = colors[(int)(index % (ulong)colors.Length)];
            index /= (ulong)colors.Length;
        }

        return code;
    }
}
