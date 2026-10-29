using System;
using System.Collections.Generic;

namespace Mastermind.Core;

/// <summary>The classic code maker: picks a secret code at the start and scores every guess against it.</summary>
public sealed class FixedCodeMaker : ICodeMaker
{
    private readonly DecodingBoard _decodingBoard = new();

    /// <summary>Creates a code maker with the given secret code.</summary>
    /// <param name="secretCode">The secret code; it must have one peg per shield hole.</param>
    /// <exception cref="ArgumentNullException"><paramref name="secretCode"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="secretCode"/> does not have one peg per shield hole.</exception>
    public FixedCodeMaker(CodePeg[] secretCode)
    {
        ArgumentNullException.ThrowIfNull(secretCode);

        // Copy what the caller hands in: a caller (or generator) that reuses its
        // buffer must not be able to change the code afterwards.
        _decodingBoard.PlayCodeMaker(new Shield(secretCode));
        SecretCode = Array.AsReadOnly((CodePeg[])secretCode.Clone());
    }

    /// <inheritdoc />
    public IReadOnlyList<CodePeg> SecretCode { get; }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="guess"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="guess"/> does not have one peg per shield hole.</exception>
    public Response Answer(IReadOnlyList<CodePeg> guess)
    {
        ArgumentNullException.ThrowIfNull(guess);

        return _decodingBoard.PlayCodeBreaker([.. guess]);
    }
}
