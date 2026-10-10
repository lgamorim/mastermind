using System;
using System.Collections.Generic;

namespace Mastermind.Core;

/// <summary>
/// A single round of Mastermind on the <see cref="BoardConfig.Standard"/> board:
/// the code maker's secret code, the code breaker's guesses so far, and whether
/// either side has won.
/// </summary>
public sealed class Game
{
    private readonly DecodingBoard _decodingBoard = new();
    private readonly List<GuessRecord> _history = [];

    /// <summary>Starts a game whose secret code comes from <paramref name="secretCodeGenerator"/>.</summary>
    /// <param name="secretCodeGenerator">Supplies the secret code; it is asked for exactly one code.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="secretCodeGenerator"/> is <see langword="null"/>, or it returned <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">The generated code does not have one peg per shield hole.</exception>
    public Game(ISecretCodeGenerator secretCodeGenerator)
    {
        ArgumentNullException.ThrowIfNull(secretCodeGenerator);

        // Copy what the generator hands back: a generator that reuses its buffer
        // must not be able to change this game's code afterwards.
        var secretCode = secretCodeGenerator.Generate(BoardConfig.ShieldSize);
        ArgumentNullException.ThrowIfNull(secretCode, nameof(secretCodeGenerator));
        _decodingBoard.PlayCodeMaker(new Shield(secretCode));

        SecretCode = ReadOnlySnapshot(secretCode);
        History = _history.AsReadOnly();
    }

    /// <summary>The board's shield size and number of rows.</summary>
    public BoardConfig BoardConfig => _decodingBoard.BoardConfig;

    /// <summary>
    /// The code maker's secret code. It is always available; when to show it to
    /// the code breaker (on game over, or up front in a debug mode) is the
    /// front-end's decision.
    /// </summary>
    public IReadOnlyList<CodePeg> SecretCode { get; }

    /// <summary>Every guess played so far, oldest first.</summary>
    public IReadOnlyList<GuessRecord> History { get; }

    /// <summary>Whether the game is still running and, if not, who won.</summary>
    public GameStatus Status { get; private set; } = GameStatus.InProgress;

    /// <summary>Plays the code breaker's next guess and scores it.</summary>
    /// <param name="code">The guess; it must have one peg per shield hole.</param>
    /// <returns>The black and white key pegs the guess earned.</returns>
    /// <exception cref="InvalidOperationException">The game is already over.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> has the wrong number of pegs.</exception>
    public Response Guess(CodePeg[] code)
    {
        if (Status is not GameStatus.InProgress)
            throw new InvalidOperationException("The game is over. Start a new game to play again.");

        var response = _decodingBoard.PlayCodeBreaker(code);
        _history.Add(new GuessRecord(_history.Count + 1, ReadOnlySnapshot(code), response));

        if (_decodingBoard.HasCodeBreakerSolvedSecretCode(response))
        {
            Status = GameStatus.CodeBreakerWon;
        }
        else if (_history.Count == BoardConfig.TotalRows)
        {
            Status = GameStatus.CodeMakerWon;
        }

        return response;
    }

    // Defensive copy exposed as a read-only view, so callers can neither mutate
    // the array in place nor reach the game's internal state through it.
    private static IReadOnlyList<CodePeg> ReadOnlySnapshot(CodePeg[] code) =>
        Array.AsReadOnly((CodePeg[])code.Clone());
}
