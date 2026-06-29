using Mastermind.ConsoleApp;

var runner = new ConsoleAppRunner(
    new RandomSecretCodeGenerator(),
    Console.In,
    Console.Out,
    Console.Error);

return runner.Run(args);
