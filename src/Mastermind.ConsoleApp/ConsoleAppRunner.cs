using Mastermind.Core;

using Spectre.Console;

namespace Mastermind.ConsoleApp;

// Output goes through Spectre.Console, which decides whether color is
// possible (a redirected stream, NO_COLOR, or a console with no color support
// gets plain text), so the runner never touches the process-global
// Console colors itself.
public sealed class ConsoleAppRunner(
    ISecretCodeGenerator secretCodeGenerator,
    TextReader input,
    IAnsiConsole console,
    TextWriter error)
{
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

    private static readonly string BlackLegend = $"{ToSymbol(KeyPeg.Black)} = right color in the right position.";
    private static readonly string WhiteLegend = $"{ToSymbol(KeyPeg.White)} = right color in the wrong position.";

    // Pads color names in the board so the pegs line up from row to row.
    private static readonly int LongestColorName = Enum.GetNames<CodePeg>().Max(name => name.Length);

    private int _sessionBreakerWins;
    private int _sessionMakerWins;

    private int RunGame(string[] args)
    {
        var isDebug = args.Length > 0 && args[0].ToUpperInvariant().Equals("DEBUG");

        ShowBanner();
        ShowCodePegColors();
        ShowLegend();
        WriteLine($"\n[~] The Code Breaker plays by typing {BoardConfig.Standard.ShieldSize} colors separated by a blank space.");

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

            WriteLine($"\n[i] Score so far - Code Breaker {_sessionBreakerWins} : {_sessionMakerWins} Code Maker.");
        }
        while (PromptPlayAgain());

        return 0;
    }

    private bool PromptPlayAgain()
    {
        Write("\n[>] Play again? (y/n):\t");

        var line = input.ReadLine();
        if (line is null) return false;

        var answer = line.Trim().ToLowerInvariant();
        return answer is "y" or "yes";
    }

    private GameOutcome RunSingleGame(bool isDebug)
    {
        var game = new Game(secretCodeGenerator);
        ShowCodeMakerPlayed(game.SecretCode, isDebug);

        while (game.Status is GameStatus.InProgress)
        {
            RenderBoard(game.History);

            var turn = PlayCodeBreaker(game.History.Count + 1);
            if (turn.Code is not { } codePlayed)
            {
                if (turn.Action is BreakerAction.Quit)
                {
                    RenderBoard(game.History);
                    RevealSecretCode(game.SecretCode);
                    WriteLineColored(QuitMessage, Color.Aqua);
                    return GameOutcome.Abandoned;
                }

                // End of input: the code breaker is out of the game without
                // having cracked the code, which scores as a Code Maker win.
                break;
            }

            WriteLine("[~] The Code Breaker has played:");
            WritePegs(codePlayed);

            var response = game.Guess(codePlayed);

            WriteLine("[^] The Code Maker has responded:");
            WriteLine($"\t{KeyPegsText(response)}");
        }

        RenderBoard(game.History);

        if (game.Status is GameStatus.CodeBreakerWon)
        {
            WriteLineColored(CodeBreakerWinsMessage, Color.Green);
            return GameOutcome.CodeBreakerWon;
        }

        RevealSecretCode(game.SecretCode);
        WriteLineColored(CodeMakerWinsMessage, Color.Red);
        WriteLine("    Better luck next time!");
        return GameOutcome.CodeMakerWon;
    }

    private void RevealSecretCode(IReadOnlyList<CodePeg> secretCode)
    {
        WriteLine("\n[i] The secret code was:");
        WritePegs(secretCode);
    }

    private void ShowBanner()
    {
        WriteLine("==============================");
        WriteLine("    Welcome to MASTERMIND!");
        WriteLine("==============================");
    }

    private void ShowLegend()
    {
        WriteLine("\n[i] Feedback legend:");
        WriteLine($"    {BlackLegend}");
        WriteLine($"    {WhiteLegend}");
    }

    private void ShowCodePegColors()
    {
        var codePegs = Enum.GetValues<CodePeg>();
        WriteLine($"[i] There are Code Pegs with {codePegs.Length} different colors:");
        foreach (var color in codePegs)
        {
            console.MarkupLine($"\t{PegMarkup(color)}");
        }
    }

    private static string[] GetCodePegColors()
    {
        return Enum.GetNames<CodePeg>();
    }

    private void ShowCodeMakerPlayed(IReadOnlyList<CodePeg> secretCode, bool isDebug)
    {
        WriteLine("[^] The Code Maker has played.");

        if (isDebug)
        {
            WritePegs(secretCode);
        }
        else
        {
            WriteLine("\t[X] [X] [X] [X]");
        }
    }

    private BreakerTurn PlayCodeBreaker(int play)
    {
        while (true)
        {
            Write($"\n[>] Code Breaker play {play}/{BoardConfig.Standard.TotalRows}:\t");

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

                WriteLine($"[!] Unknown command '{trimmed}'. Type /help to see the available commands.");
                continue;
            }

            var colors = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (colors.Length != BoardConfig.Standard.ShieldSize)
            {
                WriteLine($"[!] The Code Breaker plays by typing {BoardConfig.Standard.ShieldSize} colors separated by a blank space.");
                WriteLine($"    You entered {colors.Length} color(s); {BoardConfig.Standard.ShieldSize} are required.");
                continue;
            }

            var codePlayed = new List<CodePeg>(colors.Length);
            var isValid = true;
            foreach (var color in colors)
            {
                if (!Enum.TryParse<CodePeg>(color, ignoreCase: true, out var peg))
                {
                    WriteLine($"[!] The color {color} played is not a valid Code Peg color.");
                    WriteLine($"    Valid colors: {string.Join(", ", GetCodePegColors())}.");
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
        var shieldSize = BoardConfig.Standard.ShieldSize;
        WriteLine($"\n[?] Mastermind - break the secret code of {shieldSize} colors within {BoardConfig.Standard.TotalRows} attempts.");
        WriteLine($"    How to play: type {shieldSize} color names separated by spaces (for example: Red Blue Green Yellow).");
        WriteLine($"    {BlackLegend}");
        WriteLine($"    {WhiteLegend}");
        WriteLine($"    Valid colors: {string.Join(", ", GetCodePegColors())}.");
        WriteLine("    Commands: /help, /history, /quit.");
    }

    private void ShowSessionHistory()
    {
        WriteLine($"\n[i] Session history - Code Breaker wins: {_sessionBreakerWins}, Code Maker wins: {_sessionMakerWins}.");
    }

    private void RenderBoard(IReadOnlyList<GuessRecord> history)
    {
        if (history.Count == 0) return;

        var board = new Table()
            .Border(TableBorder.Simple)
            .AddColumn(new TableColumn("#").RightAligned())
            .AddColumn("Guess")
            .AddColumn("Feedback");

        foreach (var (attempt, guess, response) in history)
        {
            var pegs = string.Join("  ", guess.Select(color => PegMarkup(color, LongestColorName)));
            board.AddRow(new Text(attempt.ToString()), new Markup(pegs), new Text(KeyPegsText(response)));
        }

        WriteLine("\n[i] Board so far:");
        console.Write(board);
    }

    // Plain messages go straight to the console's writer instead of through
    // Spectre's renderer, which would hard-wrap them at the console width (80
    // columns when output is redirected), inserting line breaks the game never
    // wrote. Only pegs, colored lines and the board are rendered by Spectre.
    private void Write(string text) => console.Profile.Out.Writer.Write(text);

    private void WriteLine(string text) => console.Profile.Out.Writer.WriteLine(text);

    private void WritePegs(IEnumerable<CodePeg> pegs) =>
        console.MarkupLine($"\t{string.Join("  ", pegs.Select(color => PegMarkup(color)))}");

    private void WriteLineColored(string message, Color color) =>
        console.MarkupLine($"[{color.ToMarkup()}]{Markup.Escape(message)}[/]");

    // A code peg as a colored dot followed by its name, so it still reads
    // without color (redirected output, NO_COLOR, color-blind players).
    private static string PegMarkup(CodePeg color, int nameWidth = 0) =>
        $"[{ToSpectreColor(color).ToMarkup()}]●[/] {color.ToString().PadRight(nameWidth)}";

    // Key pegs need no color: filled and hollow dots tell them apart, and black
    // text would vanish on a dark terminal.
    private static string KeyPegsText(Response response)
    {
        var pegs = Enumerable.Repeat(ToSymbol(KeyPeg.Black), response.BlackKeyPegs)
            .Concat(Enumerable.Repeat(ToSymbol(KeyPeg.White), response.WhiteKeyPegs))
            .ToList();
        return pegs.Count == 0 ? "none" : string.Join(" ", pegs);
    }

    // Explicit peg maps. These switch expressions have no discard arm, so adding
    // a CodePeg/KeyPeg member without a mapping fails the build (CS8509)
    // instead of throwing at runtime.
    private static Color ToSpectreColor(CodePeg color) => color switch
    {
        CodePeg.Red => Color.Red,
        CodePeg.Blue => Color.Blue,
        CodePeg.Yellow => Color.Yellow,
        CodePeg.Green => Color.Green,
        CodePeg.White => Color.White,
        // Grey rather than black, which would be invisible on a dark terminal.
        CodePeg.Black => Color.Grey,
    };

    private static string ToSymbol(KeyPeg keyPeg) => keyPeg switch
    {
        KeyPeg.Black => "●",
        KeyPeg.White => "○",
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
