using Mastermind.Core;

namespace Mastermind.WebApp.Services;

// The spoiler-free result a player copies after a daily game: one row of squares per guess,
// built from the feedback alone, so it shows how the game went without revealing the code.
public static class DailyShareText
{
    private const string RightPlace = "🟩";
    private const string WrongPlace = "🟨";
    private const string Miss = "⬛";

    public static string Create(int puzzleNumber, IReadOnlyList<GuessRecord> history, bool won)
    {
        ArgumentNullException.ThrowIfNull(history);

        var score = won ? history.Count.ToString() : "X";
        var rows = history.Select(record => Row(record.Response));

        return string.Join("\n",
        [
            $"Mastermind Daily #{puzzleNumber} {score}/{BoardConfig.Standard.TotalRows}",
            "",
            .. rows,
            "",
            $"{RightPlace} right color, right place",
            $"{WrongPlace} right color, wrong place",
        ]);
    }

    private static string Row(Response response)
    {
        var misses = BoardConfig.Standard.ShieldSize - response.BlackKeyPegs - response.WhiteKeyPegs;
        return string.Concat(
            string.Concat(Enumerable.Repeat(RightPlace, response.BlackKeyPegs)),
            string.Concat(Enumerable.Repeat(WrongPlace, response.WhiteKeyPegs)),
            string.Concat(Enumerable.Repeat(Miss, misses)));
    }
}
