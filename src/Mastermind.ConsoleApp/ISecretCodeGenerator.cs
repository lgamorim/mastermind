using Mastermind.Core;

namespace Mastermind.ConsoleApp;

public interface ISecretCodeGenerator
{
    CodePeg[] Generate(int size);
}
