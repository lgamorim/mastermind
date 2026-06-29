using System;

namespace Mastermind.Core;

public class DecodingBoard
{
    public DecodingBoard()
    {
        BoardConfig = new BoardConfig(4, 10);
    }

    public DecodingBoard(BoardConfig boardConfig)
    {
        if (boardConfig.ShieldSize <= 0 || boardConfig.TotalRows <= 0)
            throw new ArgumentException("Shield size and total rows must be greater than zero.", nameof(boardConfig));

        BoardConfig = boardConfig;
    }

    public BoardConfig BoardConfig { get; }

    public Shield Shield { get; private set; }

    public void PlayCodeMaker(Shield shield)
    {
        ArgumentNullException.ThrowIfNull(shield);
        if (shield.Count != BoardConfig.ShieldSize)
            throw new ArgumentException($"Shield must have exactly {BoardConfig.ShieldSize} pegs.", nameof(shield));

        Shield = shield;
    }

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

    public bool HasCodeBreakerSolvedSecretCode(Response response)
    {
        if (Shield is null)
            throw new InvalidOperationException("Code maker must play first.");

        var totalKeyPegs = response.BlackKeyPegs + response.WhiteKeyPegs;
        if (totalKeyPegs < 0 || totalKeyPegs > Shield.Count)
            throw new ArgumentException($"The total number of key pegs must be between 0 and {Shield.Count}.", nameof(response));

        return response.BlackKeyPegs == Shield.Count;
    }
}