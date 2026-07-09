using Mastermind.Core;

namespace Mastermind.WebApp.Services;

public sealed class GameStateService(ISecretCodeGenerator secretCodeGenerator)
{
    private readonly List<GuessRecord> history = [];

    private DecodingBoard decodingBoard = new();
    private CodePeg[]? secretCode;

    public BoardConfig BoardConfig => decodingBoard.BoardConfig;

    public IReadOnlyList<GuessRecord> History => history;

    public bool IsGameOver { get; private set; }

    public bool HasWon { get; private set; }

    public bool IsDebugMode { get; set; }

    public IReadOnlyList<CodePeg>? RevealedSecretCode { get; private set; }

    public IReadOnlyList<CodePeg> AvailableColors { get; } = Enum.GetValues<CodePeg>();

    public void StartNewGame()
    {
        decodingBoard = new DecodingBoard();
        history.Clear();
        IsGameOver = false;
        HasWon = false;
        RevealedSecretCode = null;

        secretCode = secretCodeGenerator.Generate(decodingBoard.BoardConfig.ShieldSize);
        decodingBoard.PlayCodeMaker(new Shield(secretCode));

        if (IsDebugMode)
        {
            RevealedSecretCode = ReadOnlySnapshot(secretCode);
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
            response = decodingBoard.PlayCodeBreaker(guess);
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
        var secret = secretCode!;
        history.Add(new GuessRecord(history.Count + 1, ReadOnlySnapshot(guess), response));

        if (decodingBoard.HasCodeBreakerSolvedSecretCode(response))
        {
            HasWon = true;
            IsGameOver = true;
            RevealedSecretCode = ReadOnlySnapshot(secret);
        }
        else if (history.Count == decodingBoard.BoardConfig.TotalRows)
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
