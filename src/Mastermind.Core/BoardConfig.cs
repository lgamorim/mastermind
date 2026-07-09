namespace Mastermind.Core;

/// <summary>Immutable configuration for a decoding board.</summary>
public readonly record struct BoardConfig
{
    /// <summary>Creates a board configuration.</summary>
    /// <param name="shieldSize">Number of pegs in the secret code.</param>
    /// <param name="totalRows">Number of guesses the code breaker is allowed.</param>
    public BoardConfig(int shieldSize, int totalRows)
    {
        ShieldSize = shieldSize;
        TotalRows = totalRows;
    }

    /// <summary>Number of pegs in the secret code.</summary>
    public int ShieldSize { get; }

    /// <summary>Number of guesses the code breaker is allowed.</summary>
    public int TotalRows { get; }
}
