using Mastermind.Core;

namespace Mastermind.WebApp.Services;

public sealed class GameStateService(ISecretCodeGenerator secretCodeGenerator)
{
    private Game? _game;
    private bool _revealSecretFromStart;

    public BoardConfig BoardConfig => _game?.BoardConfig ?? BoardConfig.Standard;

    public IReadOnlyList<GuessRecord> History => _game?.History ?? [];

    public bool IsGameOver => _game is { Status: not GameStatus.InProgress };

    public bool HasWon => _game is { Status: GameStatus.CodeBreakerWon };

    public bool IsDebugMode { get; set; }

    // Debug mode is read once per game: toggling it mid-game must not reveal
    // (or hide) the code of a game already being played.
    public IReadOnlyList<CodePeg>? RevealedSecretCode =>
        _game is not null && (_revealSecretFromStart || IsGameOver) ? _game.SecretCode : null;

    public IReadOnlyList<CodePeg> AvailableColors { get; } = Array.AsReadOnly(Enum.GetValues<CodePeg>());

    public void StartNewGame()
    {
        _game = new Game(secretCodeGenerator);
        _revealSecretFromStart = IsDebugMode;
    }

    public bool TrySubmitGuess(CodePeg[] guess, out Response response, out string? error)
    {
        if (_game is null)
        {
            response = default;
            error = "Start a new game to play.";
            return false;
        }

        try
        {
            response = _game.Guess(guess);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            response = default;
            error = exception.Message;
            return false;
        }

        error = null;
        return true;
    }
}
