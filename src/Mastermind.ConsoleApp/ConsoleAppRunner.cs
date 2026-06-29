using Mastermind.Core;

namespace Mastermind.ConsoleApp;

public sealed class ConsoleAppRunner(
    ISecretCodeGenerator secretCodeGenerator,
    TextReader input,
    TextWriter output,
    TextWriter error)
{
    private readonly DecodingBoard decodingBoard = new();

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
    private const string CodeMakerWinsMessage = "\n\n[^] Code Maker wins!";
    private const string WrongCountMessage = "[!] The Code Breaker plays by typing 4 colors separated by a blank space.";
    private const string BlackLegend = "[Black] = right color in the right position.";
    private const string WhiteLegend = "[White] = right color in the wrong position.";

    private int sessionBreakerWins;
    private int sessionMakerWins;

    private int RunGame(string[] args)
    {
        var isDebug = args.Length > 0 && args[0].ToUpperInvariant().Equals("DEBUG");

        ShowBanner();
        ShowCodePegColors();
        ShowLegend();
        output.WriteLine("\n[~] The Code Breaker plays by typing 4 colors separated by a blank space.");

        do
        {
            if (RunSingleGame(isDebug))
            {
                sessionBreakerWins++;
            }
            else
            {
                sessionMakerWins++;
            }

            output.WriteLine($"\n[i] Score so far - Code Breaker {sessionBreakerWins} : {sessionMakerWins} Code Maker.");
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

    private bool RunSingleGame(bool isDebug)
    {
        var generatedCode = PlayCodeMaker(decodingBoard.BoardConfig.ShieldSize, isDebug);
        var shield = new Shield(generatedCode);
        decodingBoard.PlayCodeMaker(shield);

        var solved = false;
        var history = new List<(CodePeg[] Guess, Response Response)>();

        for (var play = 1; play <= decodingBoard.BoardConfig.TotalRows; play++)
        {
            RenderBoard(history);

            var codePlayed = PlayCodeBreaker(play);
            if (codePlayed is null) break;

            output.Write("[~] The Code Breaker has played:\n\t");

            foreach (var color in codePlayed)
            {
                WriteColor(color.ToString());
                output.Write(' ');
            }

            var response = decodingBoard.PlayCodeBreaker(codePlayed);
            history.Add((codePlayed, response));

            output.Write("\n[^] The Code Maker has responded:\n\t");

            for (var i = 0; i < response.BlackKeyPegs; i++)
            {
                WriteColor(nameof(KeyPeg.Black));
                output.Write(' ');
            }

            for (var i = 0; i < response.WhiteKeyPegs; i++)
            {
                WriteColor(nameof(KeyPeg.White));
                output.Write(' ');
            }

            output.WriteLine();

            if (decodingBoard.HasCodeBreakerSolvedSecretCode(response))
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
            output.Write("\n[i] The secret code was:\n\t");
            foreach (var color in generatedCode)
            {
                WriteColor(color.ToString());
                output.Write(' ');
            }

            WriteLineColored(CodeMakerWinsMessage, ConsoleColor.Red);
            output.WriteLine("    Better luck next time!");
        }

        return solved;
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
        var codePegColors = GetCodePegColors();
        output.WriteLine($"[i] There are Code Pegs with {codePegColors.Length} different colors:");
        foreach (var color in codePegColors)
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
        var pattern = secretCodeGenerator.Generate(size);

        output.Write("[^] The Code Maker has played.\n\t");

        if (isDebug)
        {
            foreach (var color in pattern)
            {
                WriteColor(color.ToString());
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

    private CodePeg[]? PlayCodeBreaker(int play)
    {
        while (true)
        {
            output.Write($"\n[>] Code Breaker play {play}/{decodingBoard.BoardConfig.TotalRows}:\t");

            var line = input.ReadLine();
            if (line is null) return null;

            var trimmed = line.Trim();
            if (trimmed.StartsWith('/'))
            {
                if (TryRunCommand(trimmed, out var quit))
                {
                    if (quit) return null;
                    continue;
                }

                output.WriteLine($"[!] Unknown command '{trimmed}'. Type /help to see the available commands.");
                continue;
            }

            var colors = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (colors.Length != decodingBoard.BoardConfig.ShieldSize)
            {
                output.WriteLine(WrongCountMessage);
                output.WriteLine($"    You entered {colors.Length} color(s); {decodingBoard.BoardConfig.ShieldSize} are required.");
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

            if (isValid) return codePlayed.ToArray();
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
        var shieldSize = decodingBoard.BoardConfig.ShieldSize;
        output.WriteLine($"\n[?] Mastermind - break the secret code of {shieldSize} colors within {decodingBoard.BoardConfig.TotalRows} attempts.");
        output.WriteLine($"    How to play: type {shieldSize} color names separated by spaces (for example: Red Blue Green Yellow).");
        output.WriteLine($"    {BlackLegend}");
        output.WriteLine($"    {WhiteLegend}");
        output.WriteLine($"    Valid colors: {string.Join(", ", GetCodePegColors())}.");
        output.WriteLine("    Commands: /help, /history, /quit.");
    }

    private void ShowSessionHistory()
    {
        output.WriteLine($"\n[i] Session history - Code Breaker wins: {sessionBreakerWins}, Code Maker wins: {sessionMakerWins}.");
    }

    private const int ColorCellWidth = 9;

    private void RenderBoard(List<(CodePeg[] Guess, Response Response)> history)
    {
        if (history.Count == 0) return;

        output.WriteLine("\n[i] Board so far:");
        output.Write("\t  #  ");
        output.Write("Guess".PadRight(decodingBoard.BoardConfig.ShieldSize * ColorCellWidth));
        output.WriteLine("Result");

        for (var row = 0; row < history.Count; row++)
        {
            var (guess, response) = history[row];

            output.Write($"\t{row + 1,3}  ");

            foreach (var color in guess)
            {
                WriteColorCell(color.ToString());
            }

            for (var i = 0; i < response.BlackKeyPegs; i++)
            {
                WriteColor(nameof(KeyPeg.Black));
                output.Write(' ');
            }

            for (var i = 0; i < response.WhiteKeyPegs; i++)
            {
                WriteColor(nameof(KeyPeg.White));
                output.Write(' ');
            }

            output.WriteLine();
        }
    }

    private void WriteColorCell(string color)
    {
        WriteColor(color);
        var padding = ColorCellWidth - (color.Length + 2);
        output.Write(new string(' ', Math.Max(1, padding)));
    }

    private void WriteLineColored(string message, ConsoleColor color)
    {
        if (!Console.IsOutputRedirected)
        {
            Console.ForegroundColor = color;
        }

        output.WriteLine(message);

        if (!Console.IsOutputRedirected)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

    private void WriteColor(string color, bool newline = false)
    {
        if (!Console.IsOutputRedirected)
        {
            var foregroundColor = Enum.Parse<ConsoleColor>(color);
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = foregroundColor != ConsoleColor.Black ? ConsoleColor.Black : ConsoleColor.White;
        }

        var message = $"[{color}]";
        output.Write(message);

        if (!Console.IsOutputRedirected)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        if (newline) output.WriteLine();
    }
}
