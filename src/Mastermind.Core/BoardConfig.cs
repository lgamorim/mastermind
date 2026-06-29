namespace Mastermind.Core;

public readonly struct BoardConfig
{
    public BoardConfig(int shieldSize, int totalRows)
    {
        ShieldSize = shieldSize;
        TotalRows = totalRows;
    }

    public int ShieldSize { get; }

    public int TotalRows { get; }
}