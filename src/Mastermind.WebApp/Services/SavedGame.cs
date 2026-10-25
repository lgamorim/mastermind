using Mastermind.Core;

namespace Mastermind.WebApp.Services;

// What a game needs to survive a page reload. The responses are left out on
// purpose: restoring replays the guesses, so the board is always re-scored
// rather than trusted from storage.
public sealed record SavedGame(CodePeg[] SecretCode, CodePeg[][] Guesses, bool RevealSecretFromStart);
