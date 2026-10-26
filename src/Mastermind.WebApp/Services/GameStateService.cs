using Mastermind.Core;

namespace Mastermind.WebApp.Services;

// The player keeps two games: a free-play game and the day's puzzle. Mode says which one is
// on screen, and every board-facing member below reads that one, so switching never
// abandons the other. Only free-play games count toward the stats.
public sealed class GameStateService(ISecretCodeGenerator secretCodeGenerator)
{
    private Round? _freePlay;
    private Round? _daily;

    public GameMode Mode { get; private set; } = GameMode.FreePlay;

    public bool HasGame => Current is not null;

    public bool HasFreePlayGame => _freePlay is not null;

    // The date of the daily puzzle being kept, whether or not it is on screen.
    public DateOnly? DailyDate => _daily?.Date;

    public BoardConfig BoardConfig => Current?.Game.BoardConfig ?? BoardConfig.Standard;

    public IReadOnlyList<GuessRecord> History => Current?.Game.History ?? [];

    public bool IsGameOver => Current is { Game.Status: not GameStatus.InProgress };

    public bool HasWon => Current is { Game.Status: GameStatus.CodeBreakerWon };

    public bool IsDebugMode { get; set; }

    // Debug mode is read once per game: toggling it mid-game must not reveal
    // (or hide) the code of a game already being played.
    public IReadOnlyList<CodePeg>? RevealedSecretCode =>
        Current is { } round && (round.RevealSecretFromStart || IsGameOver) ? round.Game.SecretCode : null;

    public IReadOnlyList<CodePeg> AvailableColors { get; } = Array.AsReadOnly(Enum.GetValues<CodePeg>());

    // Only free-play games played without the secret showing count, and each is
    // recorded once, at the moment it ends: restoring a finished game from
    // storage records nothing, so a reload cannot count it again.
    public GameStats Stats { get; private set; } = GameStats.Empty;

    // Reviewed once per change to a game's history rather than on every render;
    // the page shows the notes only when the player has the coach switched on.
    public IReadOnlyList<CoachNote> CoachNotes => Current?.CoachNotes ?? [];

    private Round? Current => Mode is GameMode.Daily ? _daily : _freePlay;

    // Starts a new free-play game and shows it, from either mode.
    public void StartNewGame()
    {
        // Walking away from a game in progress counts as a loss, so restarting
        // cannot protect a streak. A game with no guesses yet was never played.
        if (_freePlay is { Game: { Status: GameStatus.InProgress, History.Count: > 0 }, CountsTowardStats: true })
        {
            Stats = Stats.RecordLoss();
        }

        _freePlay = new Round(new Game(secretCodeGenerator), IsDebugMode, date: null);
        Mode = GameMode.FreePlay;
    }

    // Shows the free-play game, as it was left.
    public void ShowFreePlay() => Mode = GameMode.FreePlay;

    // Shows the given day's puzzle: the one already kept if it is that day's, otherwise a
    // fresh one, since a new day replaces yesterday's puzzle.
    public void StartDaily(DateOnly today)
    {
        if (_daily?.Date != today)
        {
            _daily = new Round(new Game(DailyPuzzle.SecretCodeFor(today)), IsDebugMode, today);
        }

        Mode = GameMode.Daily;
    }

    public bool TrySubmitGuess(CodePeg[] guess, out Response response, out string? error)
    {
        if (Current is not { } round)
        {
            response = default;
            error = "Start a new game to play.";
            return false;
        }

        try
        {
            response = round.Game.Guess(guess);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            response = default;
            error = exception.Message;
            return false;
        }

        if (round.CountsTowardStats)
        {
            Stats = round.Game.Status switch
            {
                GameStatus.CodeBreakerWon => Stats.RecordWin(round.Game.History.Count),
                GameStatus.CodeMakerWon => Stats.RecordLoss(),
                _ => Stats,
            };
        }

        round.Review();
        error = null;
        return true;
    }

    public bool TryRestoreStats(GameStats stats)
    {
        if (!stats.IsConsistent()) return false;

        Stats = stats;
        return true;
    }

    public SavedGame? CreateSavedGame() =>
        _freePlay is null
            ? null
            : new SavedGame([.. _freePlay.Game.SecretCode], GuessesOf(_freePlay), _freePlay.RevealSecretFromStart);

    public SavedDailyGame? CreateSavedDailyGame() =>
        _daily is { Date: { } date } ? new SavedDailyGame(date, GuessesOf(_daily)) : null;

    // A save comes from browser storage, which the player can edit, so it is
    // validated rather than trusted: every code must be present (JSON's
    // nullable checks stop at the properties, not the array items) and every
    // peg a real color (the enum would otherwise index past the board's color
    // counts), and replaying the guesses through Game rejects wrong lengths and
    // guesses after the end. A rejected save leaves the current game untouched.
    // A restored game becomes the free-play game; the mode on screen is unchanged.
    public bool TryRestore(SavedGame savedGame)
    {
        if (!AreRealCodes(savedGame.Guesses.Prepend(savedGame.SecretCode))) return false;
        if (Replay(savedGame.SecretCode, savedGame.Guesses) is not { } game) return false;

        _freePlay = new Round(game, savedGame.RevealSecretFromStart, date: null);
        return true;
    }

    // Validated like a free-play save, and only today's puzzle is restored: a save from
    // another day belongs to a puzzle that is no longer the daily one.
    public bool TryRestoreDaily(SavedDailyGame savedDailyGame, DateOnly today)
    {
        if (savedDailyGame.Date != today) return false;
        if (!AreRealCodes(savedDailyGame.Guesses)) return false;
        if (Replay(DailyPuzzle.SecretCodeFor(today), savedDailyGame.Guesses) is not { } game) return false;

        _daily = new Round(game, IsDebugMode, today);
        return true;
    }

    private static CodePeg[][] GuessesOf(Round round) => [.. round.Game.History.Select(record => record.Guess.ToArray())];

    private static bool AreRealCodes(IEnumerable<CodePeg[]> codes) =>
        codes.All(code => code is not null && code.All(Enum.IsDefined));

    private static Game? Replay(CodePeg[] secretCode, CodePeg[][] guesses)
    {
        try
        {
            var game = new Game(secretCode);
            foreach (var guess in guesses)
            {
                game.Guess(guess);
            }

            return game;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    // One of the player's two games, with what the page needs alongside it. Date is set for
    // the daily puzzle and null for free play.
    private sealed class Round(Game game, bool revealSecretFromStart, DateOnly? date)
    {
        public Game Game { get; } = game;

        public bool RevealSecretFromStart { get; } = revealSecretFromStart;

        public DateOnly? Date { get; } = date;

        public bool CountsTowardStats => Date is null && !RevealSecretFromStart;

        public IReadOnlyList<CoachNote> CoachNotes { get; private set; } = Coach.Review(game.History);

        public void Review() => CoachNotes = Coach.Review(Game.History);
    }
}
