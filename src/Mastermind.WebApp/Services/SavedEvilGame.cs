using Mastermind.Core;

namespace Mastermind.WebApp.Services;

// An Evil Mastermind game as stored in the browser: just the guesses. The evil code maker
// answers the same guesses the same way every time, so replaying them rebuilds the game.
public sealed record SavedEvilGame(CodePeg[][] Guesses);
