using System.Text;

using Mastermind.ConsoleApp;
using Mastermind.Core;

using Spectre.Console;

// The pegs and board are drawn with ● ○ and box-drawing characters, which a
// legacy Windows console code page would print as '?'. Set before
// AnsiConsole.Console is first touched, since Spectre reads the encoding then.
Console.OutputEncoding = Encoding.UTF8;

var runner = new ConsoleAppRunner(
    new RandomSecretCodeGenerator(),
    Console.In,
    AnsiConsole.Console,
    Console.Error);

return runner.Run(args);
