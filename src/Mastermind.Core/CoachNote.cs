namespace Mastermind.Core;

/// <summary>What the <see cref="Coach"/> has to say about one played guess.</summary>
/// <param name="Attempt">One-based number of the guess within its game.</param>
/// <param name="CodesRemaining">
/// How many codes still fit every response up to and including this guess's. Never zero in
/// a real game, since the secret code always fits.
/// </param>
/// <param name="ContradictedAttempt">
/// The earliest earlier guess whose response rules this guess out as the secret code, or
/// <see langword="null"/> when the guess could still have been the code.
/// </param>
public sealed record CoachNote(int Attempt, int CodesRemaining, int? ContradictedAttempt);
