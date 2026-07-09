using System;

namespace Mastermind.Core;

/// <summary>
/// The Mastermind decoding board: the code maker places a secret code, the code
/// breaker guesses, and each guess is scored in black and white key pegs.
/// </summary>
public class DecodingBoard
{
    /// <summary>Creates a board with the standard 4-peg, 10-row configuration.</summary>
    public DecodingBoard()
    {
        BoardConfig = new BoardConfig(4, 10);
    }

    /// <summary>Creates a board with the given configuration.</summary>
    /// <param name="boardConfig">Shield size and row count; both must be greater than zero.</param>
    /// <exception cref="ArgumentException">A dimension in <paramref name="boardConfig"/> is not positive.</exception>
    public DecodingBoard(BoardConfig boardConfig)
    {
        if (boardConfig.ShieldSize <= 0 || boardConfig.TotalRows <= 0)
            throw new ArgumentException("Shield size and total rows must be greater than zero.", nameof(boardConfig));

        BoardConfig = boardConfig;
    }

    /// <summary>The board configuration.</summary>
    public BoardConfig BoardConfig { get; }

    /// <summary>The secret code once the code maker has played; otherwise <see langword="null"/>.</summary>
    public Shield? Shield { get; private set; }

    /// <summary>Places (or replaces) the secret code.</summary>
    /// <param name="shield">The secret code; its length must equal <see cref="BoardConfig"/>.ShieldSize.</param>
    /// <exception cref="ArgumentNullException"><paramref name="shield"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="shield"/> has the wrong number of pegs.</exception>
    public void PlayCodeMaker(Shield shield)
    {
        ArgumentNullException.ThrowIfNull(shield);
        if (shield.Count != BoardConfig.ShieldSize)
            throw new ArgumentException($"Shield must have exactly {BoardConfig.ShieldSize} pegs.", nameof(shield));

        Shield = shield;
    }

    /// <summary>Scores a guess against the secret code.</summary>
    /// <param name="code">The guess; its length must equal the shield's peg count.</param>
    /// <returns>The black and white key pegs earned by the guess.</returns>
    /// <exception cref="InvalidOperationException">The code maker has not played yet.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> has the wrong number of pegs.</exception>
    public Response PlayCodeBreaker(CodePeg[] code)
    {
        if (Shield is null)
            throw new InvalidOperationException("Code maker must play first.");
        ArgumentNullException.ThrowIfNull(code);
        if (code.Length != Shield.Count)
            throw new ArgumentException($"Code must have exactly {Shield.Count} pegs.", nameof(code));

        var colorCount = Enum.GetValues<CodePeg>().Length;
        var secretCounts = new int[colorCount];
        var codeCounts = new int[colorCount];

        var blackKeyPegs = 0;
        for (var i = 0; i < code.Length; i++)
        {
            if (Shield.HasColorAt(i, code[i]))
            {
                blackKeyPegs++;
            }
            else
            {
                secretCounts[(int)Shield[i]]++;
                codeCounts[(int)code[i]]++;
            }
        }

        var whiteKeyPegs = 0;
        for (var i = 0; i < colorCount; i++)
        {
            whiteKeyPegs += Math.Min(secretCounts[i], codeCounts[i]);
        }

        return new Response(blackKeyPegs, whiteKeyPegs);
    }

    /// <summary>Determines whether a response represents a fully solved code (all black key pegs).</summary>
    /// <param name="response">A response to evaluate.</param>
    /// <returns><see langword="true"/> when every peg is a black key peg; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The code maker has not played yet.</exception>
    public bool HasCodeBreakerSolvedSecretCode(Response response)
    {
        if (Shield is null)
            throw new InvalidOperationException("Code maker must play first.");

        return response.BlackKeyPegs == Shield.Count;
    }
}
