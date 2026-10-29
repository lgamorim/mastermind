using System;
using System.Collections.Generic;

namespace Mastermind.Core;

/// <summary>
/// A single round of Mastermind on the <see cref="BoardConfig.Standard"/> board:
/// the code breaker's guesses so far, the code maker's answers to them, and whether
/// either side has won.
/// </summary>
public sealed class Game
{
    private readonly ICodeMaker _codeMaker;
    private readonly List<GuessRecord> _history = [];

    /// <summary>Starts a game whose secret code comes from <paramref name="secretCodeGenerator"/>.</summary>
    /// <param name="secretCodeGenerator">Supplies the secret code; it is asked for exactly one code.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="secretCodeGenerator"/> is <see langword="null"/>, or it returned <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">The generated code does not have one peg per shield hole.</exception>
    public Game(ISecretCodeGenerator secretCodeGenerator)
        : this(GenerateSecretCode(secretCodeGenerator))
    {
    }

    /// <summary>Starts a game with a known secret code, such as one being restored.</summary>
    /// <param name="secretCode">The secret code; it must have one peg per shield hole.</param>
    /// <exception cref="ArgumentNullException"><paramref name="secretCode"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="secretCode"/> does not have one peg per shield hole.</exception>
    public Game(CodePeg[] secretCode)
        : this(new FixedCodeMaker(secretCode))
    {
    }

    /// <summary>Starts a game against the given code maker.</summary>
    /// <param name="codeMaker">Answers the code breaker's guesses.</param>
    /// <exception cref="ArgumentNullException"><paramref name="codeMaker"/> is <see langword="null"/>.</exception>
    public Game(ICodeMaker codeMaker)
    {
        ArgumentNullException.ThrowIfNull(codeMaker);

        _codeMaker = codeMaker;
        History = _history.AsReadOnly();
    }

    /// <summary>The board's shield size and number of rows.</summary>
    public BoardConfig BoardConfig => BoardConfig.Standard;

    /// <summary>
    /// The code maker's secret code: a code that fits every answer so far. It is always
    /// available; when to show it to the code breaker (on game over, or up front in a
    /// debug mode) is the front-end's decision.
    /// </summary>
    public IReadOnlyList<CodePeg> SecretCode => _codeMaker.SecretCode;

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
        ArgumentNullException.ThrowIfNull(code);
        if (code.Length != BoardConfig.ShieldSize)
            throw new ArgumentException($"Code must have exactly {BoardConfig.ShieldSize} pegs.", nameof(code));

        // The code maker sees a copy, so neither it nor the caller can change the
        // recorded guess afterwards.
        var guess = ReadOnlySnapshot(code);
        var response = _codeMaker.Answer(guess);
        _history.Add(new GuessRecord(_history.Count + 1, guess, response));

        if (response.BlackKeyPegs == BoardConfig.ShieldSize)
        {
            Status = GameStatus.CodeBreakerWon;
        }
        else if (_history.Count == BoardConfig.TotalRows)
        {
            Status = GameStatus.CodeMakerWon;
        }

        return response;
    }

    private static CodePeg[] GenerateSecretCode(ISecretCodeGenerator secretCodeGenerator)
    {
        ArgumentNullException.ThrowIfNull(secretCodeGenerator);

        var secretCode = secretCodeGenerator.Generate(BoardConfig.Standard.ShieldSize);
        ArgumentNullException.ThrowIfNull(secretCode, nameof(secretCodeGenerator));
        return secretCode;
    }

    // Defensive copy exposed as a read-only view, so callers can neither mutate
    // the array in place nor reach the game's internal state through it.
    private static IReadOnlyList<CodePeg> ReadOnlySnapshot(CodePeg[] code) =>
        Array.AsReadOnly((CodePeg[])code.Clone());
}
