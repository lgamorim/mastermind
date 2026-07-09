namespace Mastermind.Core;

/// <summary>The feedback pegs the code maker uses to score a guess.</summary>
public enum KeyPeg
{
    /// <summary>A correct color in the wrong position.</summary>
    White,

    /// <summary>A correct color in the correct position.</summary>
    Black
}
