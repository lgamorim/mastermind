using Mastermind.Core;

namespace Mastermind.WebApp.Services;

public sealed class GameStateService(ISecretCodeGenerator secretCodeGenerator)
{
    private readonly List<GuessRecord> _history = [];

    private DecodingBoard _decodingBoard = new();
    private CodePeg[]? _secretCode;

    public BoardConfig BoardConfig => _decodingBoard.BoardConfig;

    public IReadOnlyList<GuessRecord> History => _history;

    public bool IsGameOver { get; private set; }

    public bool HasWon { get; private set; }

    public bool IsDebugMode { get; set; }

    public IReadOnlyList<CodePeg>? RevealedSecretCode { get; private set; }

    public IReadOnlyList<CodePeg> AvailableColors { get; } = ReadOnlySnapshot(Enum.GetValues<CodePeg>());

    public void StartNewGame()
    {
        _decodingBoard = new DecodingBoard();
        _history.Clear();
        IsGameOver = false;
        HasWon = false;
        RevealedSecretCode = null;

        // Copy what the generator hands back: a generator that reuses its
        // buffer must not be able to change this game's code afterwards.
        _secretCode = (CodePeg[])secretCodeGenerator.Generate(_decodingBoard.BoardConfig.ShieldSize).Clone();
        _decodingBoard.PlayCodeMaker(new Shield(_secretCode));

        if (IsDebugMode)
        {
            RevealedSecretCode = ReadOnlySnapshot(_secretCode);
        }
    }

    public bool TrySubmitGuess(CodePeg[] guess, out Response response, out string? error)
    {
        if (IsGameOver)
        {
            response = default;
            error = "The game is over. Start a new game to play again.";
            return false;
        }

        try
        {
            response = _decodingBoard.PlayCodeBreaker(guess);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            response = default;
            error = exception.Message;
            return false;
        }

        error = null;
        // The code maker has played (PlayCodeBreaker above would have thrown
        // otherwise), so the secret code is guaranteed to be set here.
        var secret = _secretCode!;
        _history.Add(new GuessRecord(_history.Count + 1, ReadOnlySnapshot(guess), response));

        if (_decodingBoard.HasCodeBreakerSolvedSecretCode(response))
        {
            HasWon = true;
            IsGameOver = true;
            RevealedSecretCode = ReadOnlySnapshot(secret);
        }
        else if (_history.Count == _decodingBoard.BoardConfig.TotalRows)
        {
            IsGameOver = true;
            RevealedSecretCode = ReadOnlySnapshot(secret);
        }

        return true;
    }

    // Defensive copy exposed as a read-only view, so callers can neither mutate
    // the array in place nor reach the service's internal state through it.
    private static IReadOnlyList<CodePeg> ReadOnlySnapshot(CodePeg[] code) =>
        Array.AsReadOnly((CodePeg[])code.Clone());
}

public sealed record GuessRecord(int Attempt, IReadOnlyList<CodePeg> Guess, Response Response);
