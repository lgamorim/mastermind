using Mastermind.Core;

namespace Mastermind.ConsoleApp;

public sealed class ConsoleAppRunner(
    ISecretCodeGenerator secretCodeGenerator,
    TextReader input,
    TextWriter output,
    TextWriter error)
{
    private readonly DecodingBoard _decodingBoard = new();

    // Console colors are process-global state this class does not own, so only
    // touch them when our own writer is the one painting the terminal. A runner
    // constructed with a StringWriter (tests, or any embedding host) writes its
    // text elsewhere and must leave the terminal alone.
    private readonly bool _useColor = ReferenceEquals(output, Console.Out) && !Console.IsOutputRedirected;

    public int Run(string[] args)
    {
        try
        {
            return RunGame(args);
        }
        catch (Exception exception)
        {
            error.WriteLine(exception.Message);
            return 1;
        }
    }

    private const string CodeBreakerWinsMessage = "\n[~] Code Breaker wins!";
    private const string CodeMakerWinsMessage = "\n[^] Code Maker wins!";
    private const string QuitMessage = "\n[~] Thanks for playing!";
    private const string BlackLegend = "[Black] = right color in the right position.";
    private const string WhiteLegend = "[White] = right color in the wrong position.";

    private int _sessionBreakerWins;
    private int _sessionMakerWins;

    private int RunGame(string[] args)
    {
        var isDebug = args.Length > 0 && args[0].ToUpperInvariant().Equals("DEBUG");

        ShowBanner();
        ShowCodePegColors();
        ShowLegend();
        output.WriteLine($"\n[~] The Code Breaker plays by typing {_decodingBoard.BoardConfig.ShieldSize} colors separated by a blank space.");

        do
        {
            var outcome = RunSingleGame(isDebug);

            // An abandoned round is neither a win nor a loss, and the player has
            // already said they are done -- leave the tally alone and do not ask
            // them to confirm a second time.
            if (outcome is GameOutcome.Abandoned) break;

            if (outcome is GameOutcome.CodeBreakerWon)
            {
                _sessionBreakerWins++;
            }
            else
            {
                _sessionMakerWins++;
            }

            output.WriteLine($"\n[i] Score so far - Code Breaker {_sessionBreakerWins} : {_sessionMakerWins} Code Maker.");
        }
        while (PromptPlayAgain());

        return 0;
    }

    private bool PromptPlayAgain()
    {
        output.Write("\n[>] Play again? (y/n):\t");

        var line = input.ReadLine();
        if (line is null) return false;

        var answer = line.Trim().ToLowerInvariant();
        return answer is "y" or "yes";
    }

    private GameOutcome RunSingleGame(bool isDebug)
    {
        var generatedCode = PlayCodeMaker(_decodingBoard.BoardConfig.ShieldSize, isDebug);
        var shield = new Shield(generatedCode);
        _decodingBoard.PlayCodeMaker(shield);

        var solved = false;
        var history = new List<(CodePeg[] Guess, Response Response)>();

        for (var play = 1; play <= _decodingBoard.BoardConfig.TotalRows; play++)
        {
            RenderBoard(history);

            var turn = PlayCodeBreaker(play);
            if (turn.Code is not { } codePlayed)
            {
                if (turn.Action is BreakerAction.Quit)
                {
                    RenderBoard(history);
                    RevealSecretCode(generatedCode);
                    WriteLineColored(QuitMessage, ConsoleColor.Cyan);
                    return GameOutcome.Abandoned;
                }

                // End of input: the code breaker is out of the game without
                // having cracked the code, which scores as a Code Maker win.
                break;
            }

            output.Write("[~] The Code Breaker has played:\n\t");

            foreach (var color in codePlayed)
            {
                WriteColor(color);
                output.Write(' ');
            }

            var response = _decodingBoard.PlayCodeBreaker(codePlayed);
            history.Add((codePlayed, response));

            output.Write("\n[^] The Code Maker has responded:\n\t");

            for (var i = 0; i < response.BlackKeyPegs; i++)
            {
                WriteColor(KeyPeg.Black);
                output.Write(' ');
            }

            for (var i = 0; i < response.WhiteKeyPegs; i++)
            {
                WriteColor(KeyPeg.White);
                output.Write(' ');
            }

            output.WriteLine();

            if (_decodingBoard.HasCodeBreakerSolvedSecretCode(response))
            {
                RenderBoard(history);
                WriteLineColored(CodeBreakerWinsMessage, ConsoleColor.Green);
                solved = true;
                break;
            }
        }

        if (!solved)
        {
            RenderBoard(history);
            RevealSecretCode(generatedCode);
            WriteLineColored(CodeMakerWinsMessage, ConsoleColor.Red);
            output.WriteLine("    Better luck next time!");
        }

        return solved ? GameOutcome.CodeBreakerWon : GameOutcome.CodeMakerWon;
    }

    private void RevealSecretCode(CodePeg[] generatedCode)
    {
        output.Write("\n[i] The secret code was:\n\t");
        foreach (var color in generatedCode)
        {
            WriteColor(color);
            output.Write(' ');
        }

        output.WriteLine();
    }

    private void ShowBanner()
    {
        output.WriteLine("==============================");
        output.WriteLine("    Welcome to MASTERMIND!");
        output.WriteLine("==============================");
    }

    private void ShowLegend()
    {
        output.WriteLine("\n[i] Feedback legend:");
        output.WriteLine($"    {BlackLegend}");
        output.WriteLine($"    {WhiteLegend}");
    }

    private void ShowCodePegColors()
    {
        var codePegs = Enum.GetValues<CodePeg>();
        output.WriteLine($"[i] There are Code Pegs with {codePegs.Length} different colors:");
        foreach (var color in codePegs)
        {
            output.Write("\t");
            WriteColor(color, true);
        }
    }

    private static string[] GetCodePegColors()
    {
        return Enum.GetNames<CodePeg>();
    }

    private CodePeg[] PlayCodeMaker(int size, bool isDebug)
    {
        // Copy what the generator hands back, as GameStateService does: this
        // array is held until the end-of-round reveal, and a generator that
        // reuses its buffer must not be able to change it in the meantime.
        var pattern = (CodePeg[])secretCodeGenerator.Generate(size).Clone();

        output.Write("[^] The Code Maker has played.\n\t");

        if (isDebug)
        {
            foreach (var color in pattern)
            {
                WriteColor(color);
                output.Write(' ');
            }

            output.WriteLine();
        }
        else
        {
            output.WriteLine("[X] [X] [X] [X]");
        }

        return pattern;
    }

    private BreakerTurn PlayCodeBreaker(int play)
    {
        while (true)
        {
            output.Write($"\n[>] Code Breaker play {play}/{_decodingBoard.BoardConfig.TotalRows}:\t");

            var line = input.ReadLine();
            if (line is null) return new BreakerTurn(BreakerAction.EndOfInput, null);

            var trimmed = line.Trim();
            if (trimmed.StartsWith('/'))
            {
                if (TryRunCommand(trimmed, out var quit))
                {
                    if (quit) return new BreakerTurn(BreakerAction.Quit, null);
                    continue;
                }

                output.WriteLine($"[!] Unknown command '{trimmed}'. Type /help to see the available commands.");
                continue;
            }

            var colors = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (colors.Length != _decodingBoard.BoardConfig.ShieldSize)
            {
                output.WriteLine($"[!] The Code Breaker plays by typing {_decodingBoard.BoardConfig.ShieldSize} colors separated by a blank space.");
                output.WriteLine($"    You entered {colors.Length} color(s); {_decodingBoard.BoardConfig.ShieldSize} are required.");
                continue;
            }

            var codePlayed = new List<CodePeg>(colors.Length);
            var isValid = true;
            foreach (var color in colors)
            {
                if (!Enum.TryParse<CodePeg>(color, ignoreCase: true, out var peg))
                {
                    output.WriteLine($"[!] The color {color} played is not a valid Code Peg color.");
                    output.WriteLine($"    Valid colors: {string.Join(", ", GetCodePegColors())}.");
                    isValid = false;
                    break;
                }

                codePlayed.Add(peg);
            }

            if (isValid) return new BreakerTurn(BreakerAction.Played, codePlayed.ToArray());
        }
    }

    private bool TryRunCommand(string command, out bool quit)
    {
        quit = false;
        switch (command.ToLowerInvariant())
        {
            case "/quit":
            case "/exit":
            case "/q":
                quit = true;
                return true;
            case "/help":
                ShowHelp();
                return true;
            case "/history":
                ShowSessionHistory();
                return true;
            default:
                return false;
        }
    }

    private void ShowHelp()
    {
        var shieldSize = _decodingBoard.BoardConfig.ShieldSize;
        output.WriteLine($"\n[?] Mastermind - break the secret code of {shieldSize} colors within {_decodingBoard.BoardConfig.TotalRows} attempts.");
        output.WriteLine($"    How to play: type {shieldSize} color names separated by spaces (for example: Red Blue Green Yellow).");
        output.WriteLine($"    {BlackLegend}");
        output.WriteLine($"    {WhiteLegend}");
        output.WriteLine($"    Valid colors: {string.Join(", ", GetCodePegColors())}.");
        output.WriteLine("    Commands: /help, /history, /quit.");
    }

    private void ShowSessionHistory()
    {
        output.WriteLine($"\n[i] Session history - Code Breaker wins: {_sessionBreakerWins}, Code Maker wins: {_sessionMakerWins}.");
    }

    private const int ColorCellWidth = 9;

    private void RenderBoard(List<(CodePeg[] Guess, Response Response)> history)
    {
        if (history.Count == 0) return;

        output.WriteLine("\n[i] Board so far:");
        output.Write("\t  #  ");
        output.Write("Guess".PadRight(_decodingBoard.BoardConfig.ShieldSize * ColorCellWidth));
        output.WriteLine("Result");

        for (var row = 0; row < history.Count; row++)
        {
            var (guess, response) = history[row];

            output.Write($"\t{row + 1,3}  ");

            foreach (var color in guess)
            {
                WriteColorCell(color);
            }

            for (var i = 0; i < response.BlackKeyPegs; i++)
            {
                WriteColor(KeyPeg.Black);
                output.Write(' ');
            }

            for (var i = 0; i < response.WhiteKeyPegs; i++)
            {
                WriteColor(KeyPeg.White);
                output.Write(' ');
            }

            output.WriteLine();
        }
    }

    private void WriteColorCell(CodePeg color)
    {
        WriteColor(color);
        var padding = ColorCellWidth - (color.ToString().Length + 2);
        output.Write(new string(' ', Math.Max(1, padding)));
    }

    private void WriteLineColored(string message, ConsoleColor color)
    {
        if (_useColor)
        {
            Console.ForegroundColor = color;
        }

        output.WriteLine(message);

        if (_useColor)
        {
            Console.ResetColor();
        }
    }

    private void WriteColor(CodePeg color, bool newline = false) =>
        WriteColorLabel(color.ToString(), ToConsoleColor(color), newline);

    private void WriteColor(KeyPeg keyPeg) =>
        WriteColorLabel(keyPeg.ToString(), ToConsoleColor(keyPeg), newline: false);

    private void WriteColorLabel(string label, ConsoleColor color, bool newline)
    {
        if (_useColor)
        {
            Console.ForegroundColor = color;
            Console.BackgroundColor = color != ConsoleColor.Black ? ConsoleColor.Black : ConsoleColor.White;
        }

        output.Write($"[{label}]");

        // Reset to the terminal's own colors rather than a hardcoded
        // gray-on-black, which used to repaint light-themed terminals for the
        // rest of the session and leave them that way on exit.
        if (_useColor)
        {
            Console.ResetColor();
        }

        if (newline) output.WriteLine();
    }

    // Explicit peg-to-console-color maps. These switch expressions have no
    // discard arm, so adding a CodePeg/KeyPeg member without a mapping fails
    // the build (CS8509) instead of throwing at runtime like the old
    // name-based Enum.Parse<ConsoleColor> did.
    private static ConsoleColor ToConsoleColor(CodePeg color) => color switch
    {
        CodePeg.Red => ConsoleColor.Red,
        CodePeg.Blue => ConsoleColor.Blue,
        CodePeg.Yellow => ConsoleColor.Yellow,
        CodePeg.Green => ConsoleColor.Green,
        CodePeg.White => ConsoleColor.White,
        CodePeg.Black => ConsoleColor.Black,
    };

    private static ConsoleColor ToConsoleColor(KeyPeg keyPeg) => keyPeg switch
    {
        KeyPeg.Black => ConsoleColor.Black,
        KeyPeg.White => ConsoleColor.White,
    };

    // How a round ended. Abandoned is deliberately distinct from CodeMakerWon:
    // quitting must not be scored as a loss.
    private enum GameOutcome
    {
        CodeBreakerWon,
        CodeMakerWon,
        Abandoned
    }

    // What the code breaker did when prompted. Quit and EndOfInput both mean
    // "no guess", but they end the session very differently.
    private enum BreakerAction
    {
        Played,
        Quit,
        EndOfInput
    }

    // Code is non-null exactly when Action is Played, which is what lets
    // RunSingleGame pattern-match the guess out without a null suppression.
    private readonly record struct BreakerTurn(BreakerAction Action, CodePeg[]? Code);
}
