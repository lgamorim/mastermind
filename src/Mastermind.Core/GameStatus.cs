namespace Mastermind.Core;

/// <summary>Where a <see cref="Game"/> stands.</summary>
public enum GameStatus
{
    /// <summary>The code breaker still has guesses left and has not cracked the code.</summary>
    InProgress,

    /// <summary>The code breaker cracked the secret code.</summary>
    CodeBreakerWon,

    /// <summary>The code breaker used every row without cracking the secret code.</summary>
    CodeMakerWon
}
