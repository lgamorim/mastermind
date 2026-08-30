using Mastermind.Core;
using Mastermind.WebApp.Services;

using Microsoft.AspNetCore.Components;

namespace Mastermind.WebApp.Pages;

public partial class Game
{
    private CodePeg?[] _guessSlots = [];
    private string? _submitError;
    private CodePeg? _draggedColor;
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
        _dragOverSlot = null;
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

    private void OnSlotDrop(int slotIndex)
    {
        if (_draggedColor.HasValue)
        {
            _guessSlots[slotIndex] = _draggedColor;
        }

        _dragOverSlot = null;
    }

    private void OnSlotClick(int slotIndex)
    {
        _guessSlots[slotIndex] = null;
    }

    private void SubmitGuess()
    {
        var guess = _guessSlots.Select(slot => slot!.Value).ToArray();

        if (GameState.TrySubmitGuess(guess, out _, out var error))
        {
            Array.Clear(_guessSlots);
            _submitError = null;
            _draggedColor = null;
            _dragOverSlot = null;
        }
        else
        {
            _submitError = error;
        }
    }
}
