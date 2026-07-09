namespace Mastermind.Core;

public interface ISecretCodeGenerator
{
    CodePeg[] Generate(int size);
}
