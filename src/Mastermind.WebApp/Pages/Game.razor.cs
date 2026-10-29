using Mastermind.Core;
using Mastermind.WebApp.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

namespace Mastermind.WebApp.Pages;

public partial class Game
{
    private CodePeg?[] _guessSlots = new CodePeg?[BoardConfig.Standard.ShieldSize];
    private string? _submitError;
    private CodePeg? _draggedColor;
    private CodePeg? _selectedColor;
    private int? _dragOverSlot;

    // Off by default so the classic game stays unaided; the player opts in to
    // the coach's counts and contradiction warnings.
    private bool _coachOn;

    // The outcome of the last Share click; the fallback text is shown when the
    // browser would not let the page write to the clipboard.
    private string? _shareStatus;
    private string? _shareFallbackText;

    [SupplyParameterFromQuery(Name = "debug")]
    [Parameter]
    public bool Debug { get; set; }

    // default!: the framework assigns every [Inject] property before any member of the component
    // runs, so this is never observed null.
    [Inject]
    private GameStateService GameState { get; set; } = default!;

    // default!: assigned by the framework before use, as GameState above.
    [Inject]
    private IWebAssemblyHostEnvironment HostEnvironment { get; set; } = default!;

    // default!: assigned by the framework before use, as GameState above.
    [Inject]
    private GameStorage Storage { get; set; } = default!;

    // default!: assigned by the framework before use, as GameState above.
    [Inject]
    private TimeProvider Clock { get; set; } = default!;

    // default!: assigned by the framework before use, as GameState above.
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private bool GameStarted => GameState.HasGame;

    private bool AllSlotsFilled => _guessSlots.All(slot => slot is not null);

    // The daily puzzle follows the player's own calendar, changing at their local midnight.
    private DateOnly Today => DateOnly.FromDateTime(Clock.GetLocalNow().DateTime);

    // Hidden if the clock reads earlier than the first puzzle, which has no number.
    private bool DailyAvailable => Today >= DailyPuzzle.FirstDate;

    private bool IsFreePlay => GameState.Mode is GameMode.FreePlay;

    private bool IsDaily => GameState.Mode is GameMode.Daily;

    private bool IsEvil => GameState.Mode is GameMode.Evil;

    // A page left open past local midnight can still be showing yesterday's puzzle. The
    // player may finish it, but the Daily button then stands for the new day's puzzle, and
    // yesterday's game is no longer saved: its save would be dropped on the next load anyway.
    private bool IsShowingTodaysDaily => IsDaily && GameState.DailyDate == Today;

    private bool IsShowingAnEarlierDaily => IsDaily && GameState.DailyDate != Today;

    // ?debug=true shows the secret code, so only a local development run honors
    // it; the published site runs as Production and ignores it. This hides the
    // code from casual players, not from anyone reading the app's memory: the
    // code lives in the browser either way.
    protected override void OnParametersSet()
    {
        GameState.IsDebugMode = Debug && HostEnvironment.IsDevelopment();
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

    // localStorage is reached through JS interop, which is only available once
    // the page has rendered, so the stats and saved game are picked up here
    // rather than in OnInitialized. Each is applied only if the player has not
    // moved on while it was loading: a game they already started, or a result
    // they already recorded, wins over the stored one.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var statsBeforeLoad = GameState.Stats;
        var storedStats = await Storage.LoadStatsAsync();
        var statsRestored = storedStats is not null
            && ReferenceEquals(GameState.Stats, statsBeforeLoad)
            && GameState.TryRestoreStats(storedStats);

        var savedGame = await Storage.LoadGameAsync();
        var gameRestored = savedGame is not null && !GameState.HasFreePlayGame && GameState.TryRestore(savedGame);
        if (gameRestored)
        {
            ResetBoardInput();
        }

        var savedDaily = await Storage.LoadDailyGameAsync();
        var dailyRestored = savedDaily is not null && GameState.DailyDate is null && GameState.TryRestoreDaily(savedDaily, Today);

        var savedEvil = await Storage.LoadEvilGameAsync();
        var evilRestored = savedEvil is not null && !GameState.HasEvilGame && GameState.TryRestoreEvil(savedEvil);

        // Restored outside any event handler, so Blazor does not re-render on its own.
        if (statsRestored || gameRestored || dailyRestored || evilRestored)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task StartNewGame()
    {
        var statsBefore = GameState.Stats;
        GameState.StartNewGame();
        ResetBoardInput();
        await SaveProgressAsync(statsBefore);
    }

    private void OpenFreePlay()
    {
        GameState.ShowFreePlay();
        ResetBoardInput();
    }

    private void OpenEvil()
    {
        GameState.StartEvil();
        ResetBoardInput();
    }

    private async Task StartNewEvilGame()
    {
        var statsBefore = GameState.Stats;
        GameState.StartNewEvilGame();
        ResetBoardInput();
        await SaveProgressAsync(statsBefore);
    }

    private void OpenDaily()
    {
        GameState.StartDaily(Today);
        ResetBoardInput();
    }

    private void ResetBoardInput()
    {
        _guessSlots = new CodePeg?[GameState.BoardConfig.ShieldSize];
        _submitError = null;
        _draggedColor = null;
        _selectedColor = null;
        _dragOverSlot = null;
        _shareStatus = null;
        _shareFallbackText = null;
    }

    private string ResultMessage()
    {
        var guesses = GameState.History.Count;
        var plural = guesses == 1 ? "" : "es";

        return (GameState.Mode, GameState.DailyDate, GameState.HasWon) switch
        {
            (GameMode.Daily, { } date, true) => $"You cracked Daily #{DailyPuzzle.NumberFor(date)} in {guesses} guess{plural}!",
            (GameMode.Daily, { } date, false) => $"Daily #{DailyPuzzle.NumberFor(date)} kept its secret. A new puzzle starts at midnight.",
            (GameMode.Evil, _, true) => $"You beat the Evil Code Maker in {guesses} guess{plural}!",
            (GameMode.Evil, _, false) => "The Evil Code Maker got away. It settled on the code shown above.",
            (_, _, true) => $"You cracked the code in {guesses} guess{plural}!",
            _ => "The Code Maker wins — better luck next time!",
        };
    }

    // Copies the spoiler-free result. The clipboard can refuse (permissions, an
    // insecure page), so on failure the text is shown for the player to copy by hand.
    private async Task ShareResultAsync()
    {
        if (GameState.DailyDate is not { } date) return;

        var text = DailyShareText.Create(DailyPuzzle.NumberFor(date), GameState.History, GameState.HasWon);
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
            _shareStatus = "Copied to your clipboard.";
            _shareFallbackText = null;
        }
        catch (JSException)
        {
            _shareStatus = "Your browser didn't allow copying, so copy it from the box below.";
            _shareFallbackText = text;
        }
    }

    // Saves the game on screen after every move, and the stats only when the
    // move changed them (a game ended or was abandoned). GameStats is immutable,
    // so a changed record is a different instance.
    private async Task SaveProgressAsync(GameStats statsBefore)
    {
        if (IsDaily)
        {
            if (IsShowingTodaysDaily && GameState.CreateSavedDailyGame() is { } savedDaily)
            {
                await Storage.SaveDailyGameAsync(savedDaily);
            }
        }
        else if (IsEvil)
        {
            if (GameState.CreateSavedEvilGame() is { } savedEvil)
            {
                await Storage.SaveEvilGameAsync(savedEvil);
            }
        }
        else if (GameState.CreateSavedGame() is { } savedGame)
        {
            await Storage.SaveGameAsync(savedGame);
        }

        if (!ReferenceEquals(GameState.Stats, statsBefore))
        {
            await Storage.SaveStatsAsync(GameState.Stats);
        }
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

    private async Task SubmitGuess()
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
        var statsBefore = GameState.Stats;

        if (GameState.TrySubmitGuess(guess, out _, out var error))
        {
            Array.Clear(_guessSlots);
            _submitError = null;
            _draggedColor = null;
            _selectedColor = null;
            _dragOverSlot = null;
            await SaveProgressAsync(statsBefore);
        }
        else
        {
            _submitError = error;
        }
    }
}
