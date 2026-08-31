using Mastermind.Core;
using Mastermind.WebApp.Services;

using Microsoft.AspNetCore.Components;

namespace Mastermind.WebApp.Pages;

public partial class Game
{
    private CodePeg?[] _guessSlots = [];
    private string? _submitError;
    private CodePeg? _draggedColor;
    private CodePeg? _selectedColor;
    private int? _dragOverSlot;

    [SupplyParameterFromQuery(Name = "debug")]
    [Parameter]
    public bool Debug { get; set; }

    // default!: the framework assigns every [Inject] property before any member of the component
    // runs, so this is never observed null.
    [Inject]
    private GameStateService GameState { get; set; } = default!;

    private bool GameStarted => _guessSlots.Length > 0;

    private bool AllSlotsFilled => _guessSlots.Length > 0 && _guessSlots.All(slot => slot is not null);

    protected override void OnParametersSet()
    {
        GameState.IsDebugMode = Debug;
    }

    private static string ToPegClass(CodePeg color) => $"peg--{color.ToString().ToLowerInvariant()}";

    private static string ToSlotLabel(int slotIndex, CodePeg? color) =>
        $"Hole {slotIndex + 1}, {(color is { } peg ? peg.ToString() : "empty")}";

    private static IReadOnlyList<string> GetKeyPegDots(Response response, int pegCount)
    {
        var dots = new string[pegCount];
        var i = 0;
        for (; i < response.BlackKeyPegs; i++) dots[i] = "key-black";
        for (; i < response.BlackKeyPegs + response.WhiteKeyPegs; i++) dots[i] = "key-white";
        for (; i < pegCount; i++) dots[i] = "key-empty";
        return dots;
    }

    private void StartNewGame()
    {
        GameState.StartNewGame();
        _guessSlots = new CodePeg?[GameState.BoardConfig.ShieldSize];
        _submitError = null;
        _draggedColor = null;
        _selectedColor = null;
        _dragOverSlot = null;
    }

    // Tapping is the only way in on a touch device, where HTML5 drag events are
    // never emitted: pick a color, then tap holes to fill them. Tapping the
    // selected color again puts tapping back into clear-the-hole mode.
    private void OnPaletteClick(CodePeg color)
    {
        _selectedColor = _selectedColor == color ? null : color;
    }

    private void OnPaletteDragStart(CodePeg color)
    {
        _draggedColor = color;
    }

    private void OnPaletteDragEnd()
    {
        _draggedColor = null;
        _dragOverSlot = null;
    }

    // dragenter on the slot being entered fires before dragleave on the one
    // being left, so only the slot that still owns the highlight may clear it --
    // otherwise the ring vanishes for the whole sweep across a row.
    private void OnSlotDragLeave(int slotIndex)
    {
        if (_dragOverSlot == slotIndex)
        {
            _dragOverSlot = null;
        }
    }

    private void OnSlotDrop(int slotIndex)
    {
        if (_draggedColor.HasValue)
        {
            _guessSlots[slotIndex] = _draggedColor;
        }

        _dragOverSlot = null;
        ClearSubmitError();
    }

    // With a color selected the tap places it (and keeps the selection, so the
    // rest of the row can be filled with one tap each); with none selected it
    // clears the hole, which is what a click did before tapping existed.
    private void OnSlotClick(int slotIndex)
    {
        _guessSlots[slotIndex] = _selectedColor;
        ClearSubmitError();
    }

    // The message describes the board as it was at the last submit; the moment
    // the player edits a slot it is stale, so it must not linger next to a
    // Submit button that may already be enabled again.
    private void ClearSubmitError()
    {
        _submitError = null;
    }

    private void SubmitGuess()
    {
        // The disabled Submit button is UX only -- it is the caller's courtesy,
        // not a guarantee. Without this guard an empty slot throws out of
        // Nullable<T>.Value, and on WebAssembly there is no circuit to recover:
        // the whole app drops into the unhandled-error banner.
        if (!AllSlotsFilled)
        {
            _submitError = "Fill every hole before submitting.";
            return;
        }

        // Every slot is non-null here, guarded immediately above.
        var guess = _guessSlots.Select(slot => slot!.Value).ToArray();

        if (GameState.TrySubmitGuess(guess, out _, out var error))
        {
            Array.Clear(_guessSlots);
            _submitError = null;
            _draggedColor = null;
            _selectedColor = null;
            _dragOverSlot = null;
        }
        else
        {
            _submitError = error;
        }
    }
}
