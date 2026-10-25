using System.Collections.Immutable;
using System.Text.Json.Serialization;

using Mastermind.Core;

namespace Mastermind.WebApp.Services;

// The player's record across games, kept in the browser next to the saved
// game. Immutable all the way down (the counts are an ImmutableArray):
// recording a game returns new stats, so a value read for rendering never
// changes underneath the page, and a changed record is a new instance.
// WinsByGuessCount[i] counts the wins that took i + 1 guesses, one bucket per
// board row.
public sealed record GameStats(int GamesPlayed, int Wins, int CurrentStreak, int MaxStreak, ImmutableArray<int> WinsByGuessCount)
{
    public static GameStats Empty => new(0, 0, 0, 0, ImmutableArray.Create(new int[BoardConfig.Standard.TotalRows]));

    // Derived from the stored counts, so it is never stored itself.
    [JsonIgnore]
    public int WinRatePercent => GamesPlayed == 0 ? 0 : (int)Math.Round(100.0 * Wins / GamesPlayed, MidpointRounding.AwayFromZero);

    public GameStats RecordWin(int guessCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(guessCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(guessCount, WinsByGuessCount.Length);

        var winsByGuessCount = WinsByGuessCount.SetItem(guessCount - 1, WinsByGuessCount[guessCount - 1] + 1);
        var currentStreak = CurrentStreak + 1;

        return new GameStats(GamesPlayed + 1, Wins + 1, currentStreak, Math.Max(MaxStreak, currentStreak), winsByGuessCount);
    }

    public GameStats RecordLoss() => this with { GamesPlayed = GamesPlayed + 1, CurrentStreak = 0 };

    // Stats come back from browser storage, which the player can edit, so
    // they are checked for numbers that could not have come from playing
    // before they are shown or added to.
    public bool IsConsistent() =>
        !WinsByGuessCount.IsDefault
        && WinsByGuessCount.Length == BoardConfig.Standard.TotalRows
        && WinsByGuessCount.All(count => count >= 0)
        && WinsByGuessCount.Sum() == Wins
        && Wins >= 0
        && Wins <= GamesPlayed
        && CurrentStreak >= 0
        && CurrentStreak <= MaxStreak
        && MaxStreak <= Wins;
}
