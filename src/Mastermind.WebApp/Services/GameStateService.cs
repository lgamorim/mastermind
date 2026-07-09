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

    public CodePeg[]? RevealedSecretCode { get; private set; }

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
            RevealedSecretCode = secretCode;
        }
    }

    public bool TrySubmitGuess(CodePeg[] guess, out Response response, out string? error)
    {
        try
        {
            response = decodingBoard.PlayCodeBreaker(guess);
        }
        catch (Exception exception) when (exception is ArgumentNullException or ArgumentException or InvalidOperationException)
        {
            response = default;
            error = exception.Message;
            return false;
        }

        error = null;
        history.Add(new GuessRecord(history.Count + 1, guess, response));

        if (decodingBoard.HasCodeBreakerSolvedSecretCode(response))
        {
            HasWon = true;
            IsGameOver = true;
        }
        else if (history.Count == decodingBoard.BoardConfig.TotalRows)
        {
            IsGameOver = true;
            RevealedSecretCode = secretCode;
        }

        return true;
    }
}

public sealed record GuessRecord(int Attempt, CodePeg[] Guess, Response Response);
