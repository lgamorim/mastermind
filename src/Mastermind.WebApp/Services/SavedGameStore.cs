using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.JSInterop;

namespace Mastermind.WebApp.Services;

// Keeps the current game in the browser's localStorage so a reload does not
// lose it. Storage is a convenience, never a requirement: if it is blocked
// (private browsing, quota) or the save is damaged, the player simply starts
// fresh instead of the app failing. Call only from event handlers or
// OnAfterRenderAsync, where JS interop is available.
public sealed class SavedGameStore(IJSRuntime jsRuntime)
{
    // Versioned so a future change to the saved shape can ignore old saves
    // rather than misread them.
    private const string StorageKey = "mastermind.savedGame.v1";

    public async Task<SavedGame?> LoadAsync()
    {
        try
        {
            var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            return json is null ? null : JsonSerializer.Deserialize(json, SavedGameJsonContext.Default.SavedGame);
        }
        catch (Exception exception) when (exception is JSException or JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(SavedGame savedGame)
    {
        try
        {
            var json = JsonSerializer.Serialize(savedGame, SavedGameJsonContext.Default.SavedGame);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch (JSException)
        {
            // Storage refused the write; the game carries on, just unsaved.
        }
    }
}

// Source-generated so serialization survives the WebAssembly publish's
// trimming. Colors are stored by name, and missing or null fields fail
// deserialization instead of yielding a half-built save.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(SavedGame))]
internal sealed partial class SavedGameJsonContext : JsonSerializerContext;
