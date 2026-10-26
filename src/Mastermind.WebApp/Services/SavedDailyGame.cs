using Mastermind.Core;

namespace Mastermind.WebApp.Services;

// The day's puzzle as stored in the browser. The secret is left out: it follows from the
// date, so storing it would only give an edited save another way to be wrong.
public sealed record SavedDailyGame(DateOnly Date, CodePeg[][] Guesses);
