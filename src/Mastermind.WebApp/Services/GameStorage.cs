using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Microsoft.JSInterop;

namespace Mastermind.WebApp.Services;

// Keeps the current game and the player's stats in the browser's
// localStorage so a reload does not lose them. Storage is a convenience,
// never a requirement: if it is blocked (private browsing, quota) or an entry
// is damaged, the player simply starts fresh instead of the app failing. Call
// only from event handlers or OnAfterRenderAsync, where JS interop is
// available.
public sealed class GameStorage(IJSRuntime jsRuntime)
{
    // Versioned so a future change to a stored shape can ignore old entries
    // rather than misread them.
    private const string GameKey = "mastermind.savedGame.v1";
    private const string StatsKey = "mastermind.stats.v1";
    private const string DailyKey = "mastermind.daily.v1";
    private const string EvilKey = "mastermind.evil.v1";

    public Task<SavedGame?> LoadGameAsync() => LoadAsync(GameKey, GameStorageJsonContext.Default.SavedGame);

    public Task SaveGameAsync(SavedGame savedGame) => SaveAsync(GameKey, savedGame, GameStorageJsonContext.Default.SavedGame);

    public Task<GameStats?> LoadStatsAsync() => LoadAsync(StatsKey, GameStorageJsonContext.Default.GameStats);

    public Task SaveStatsAsync(GameStats stats) => SaveAsync(StatsKey, stats, GameStorageJsonContext.Default.GameStats);

    public Task<SavedDailyGame?> LoadDailyGameAsync() => LoadAsync(DailyKey, GameStorageJsonContext.Default.SavedDailyGame);

    public Task SaveDailyGameAsync(SavedDailyGame savedDailyGame) =>
        SaveAsync(DailyKey, savedDailyGame, GameStorageJsonContext.Default.SavedDailyGame);

    public Task<SavedEvilGame?> LoadEvilGameAsync() => LoadAsync(EvilKey, GameStorageJsonContext.Default.SavedEvilGame);

    public Task SaveEvilGameAsync(SavedEvilGame savedEvilGame) =>
        SaveAsync(EvilKey, savedEvilGame, GameStorageJsonContext.Default.SavedEvilGame);

    private async Task<T?> LoadAsync<T>(string key, JsonTypeInfo<T> typeInfo)
        where T : class
    {
        try
        {
            var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            return json is null ? null : JsonSerializer.Deserialize(json, typeInfo);
        }
        catch (Exception exception) when (exception is JSException or JsonException)
        {
            return null;
        }
    }

    private async Task SaveAsync<T>(string key, T value, JsonTypeInfo<T> typeInfo)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value, typeInfo));
        }
        catch (JSException)
        {
            // Storage refused the write; the game carries on, just unsaved.
        }
    }
}

// Source-generated so serialization survives the WebAssembly publish's
// trimming. Colors are stored by name, and missing or null fields fail
// deserialization instead of yielding a half-built entry.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(SavedGame))]
[JsonSerializable(typeof(GameStats))]
[JsonSerializable(typeof(SavedDailyGame))]
[JsonSerializable(typeof(SavedEvilGame))]
internal sealed partial class GameStorageJsonContext : JsonSerializerContext;
