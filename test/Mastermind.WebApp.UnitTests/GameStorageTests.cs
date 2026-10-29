using System;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Mastermind.Core;
using Mastermind.WebApp.Services;
using Microsoft.JSInterop;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class GameStorageTests : BunitContext
{
    private const string GameKey = "mastermind.savedGame.v1";
    private const string StatsKey = "mastermind.stats.v1";
    private const string DailyKey = "mastermind.daily.v1";
    private const string EvilKey = "mastermind.evil.v1";

    private static readonly SavedGame SampleGame = new(
        [CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Green],
        [[CodePeg.White, CodePeg.White, CodePeg.Black, CodePeg.Black]],
        RevealSecretFromStart: true);

    private GameStorage CreateStorage() => new(JSInterop.JSRuntime);

    [Fact]
    public async Task Should_ReturnNull_When_NothingIsSaved()
    {
        JSInterop.Setup<string?>("localStorage.getItem", GameKey).SetResult(null);

        var savedGame = await CreateStorage().LoadGameAsync();

        savedGame.Should().BeNull();
    }

    [Fact]
    public async Task Should_WriteColorsByNameUnderAVersionedKey_When_AGameIsSaved()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();

        await CreateStorage().SaveGameAsync(SampleGame);

        var invocation = JSInterop.VerifyInvoke("localStorage.setItem");
        invocation.Arguments[0].Should().Be(GameKey);
        invocation.Arguments[1].Should().BeOfType<string>().Which.Should().Contain("\"Red\"").And.Contain("\"White\"");
    }

    [Fact]
    public async Task Should_LoadWhatWasSaved_When_ASaveIsReadBack()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();
        var storage = CreateStorage();
        await storage.SaveGameAsync(SampleGame);
        var json = (string?)JSInterop.VerifyInvoke("localStorage.setItem").Arguments[1];
        JSInterop.Setup<string?>("localStorage.getItem", GameKey).SetResult(json);

        var savedGame = await storage.LoadGameAsync();

        savedGame.Should().BeEquivalentTo(SampleGame);
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("""{"secretCode":["Red","Blue","Yellow","Purple"],"guesses":[],"revealSecretFromStart":false}""")]
    [InlineData("""{"secretCode":["Red","Blue","Yellow","Green"],"revealSecretFromStart":false}""")]
    [InlineData("""{"secretCode":null,"guesses":[],"revealSecretFromStart":false}""")]
    public async Task Should_ReturnNull_When_TheSaveIsDamaged(string json)
    {
        JSInterop.Setup<string?>("localStorage.getItem", GameKey).SetResult(json);

        var savedGame = await CreateStorage().LoadGameAsync();

        savedGame.Should().BeNull();
    }

    [Fact]
    public async Task Should_ReturnNull_When_StorageIsUnavailable()
    {
        JSInterop.Setup<string?>("localStorage.getItem", GameKey).SetException(new JSException("Storage is blocked."));

        var savedGame = await CreateStorage().LoadGameAsync();

        savedGame.Should().BeNull();
    }

    [Fact]
    public async Task Should_KeepPlayingUnsaved_When_StorageRejectsTheSave()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetException(new JSException("Quota exceeded."));

        var save = () => CreateStorage().SaveGameAsync(SampleGame);

        await save.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_ReturnNull_When_NoStatsAreSaved()
    {
        JSInterop.Setup<string?>("localStorage.getItem", StatsKey).SetResult(null);

        var stats = await CreateStorage().LoadStatsAsync();

        stats.Should().BeNull();
    }

    [Fact]
    public async Task Should_LoadTheStatsThatWereSaved_When_TheyAreReadBack()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();
        var storage = CreateStorage();
        var stats = GameStats.Empty.RecordWin(4).RecordLoss().RecordWin(9);
        await storage.SaveStatsAsync(stats);
        var save = JSInterop.VerifyInvoke("localStorage.setItem");
        JSInterop.Setup<string?>("localStorage.getItem", StatsKey).SetResult((string?)save.Arguments[1]);

        var loaded = await storage.LoadStatsAsync();

        save.Arguments[0].Should().Be(StatsKey);
        loaded.Should().BeEquivalentTo(stats);
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("""{"gamesPlayed":1,"wins":1,"currentStreak":1,"maxStreak":1}""")]
    [InlineData("""{"gamesPlayed":1,"wins":1,"currentStreak":1,"maxStreak":1,"winsByGuessCount":null}""")]
    public async Task Should_ReturnNull_When_TheSavedStatsAreDamaged(string json)
    {
        JSInterop.Setup<string?>("localStorage.getItem", StatsKey).SetResult(json);

        var stats = await CreateStorage().LoadStatsAsync();

        stats.Should().BeNull();
    }

    [Fact]
    public async Task Should_ReturnNull_When_StorageIsUnavailableForStats()
    {
        JSInterop.Setup<string?>("localStorage.getItem", StatsKey).SetException(new JSException("Storage is blocked."));

        var stats = await CreateStorage().LoadStatsAsync();

        stats.Should().BeNull();
    }

    [Fact]
    public async Task Should_KeepPlaying_When_StorageRejectsTheStats()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetException(new JSException("Quota exceeded."));

        var save = () => CreateStorage().SaveStatsAsync(GameStats.Empty.RecordLoss());

        await save.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_WriteTheDateAndGuessesUnderTheDailyKey_When_TheDailyGameIsSaved()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();

        await CreateStorage().SaveDailyGameAsync(new SavedDailyGame(new DateOnly(2026, 11, 3), [[CodePeg.Red, CodePeg.Red, CodePeg.Blue, CodePeg.Blue]]));

        var save = JSInterop.VerifyInvoke("localStorage.setItem");
        save.Arguments[0].Should().Be(DailyKey);
        save.Arguments[1].Should().Be("""{"date":"2026-11-03","guesses":[["Red","Red","Blue","Blue"]]}""");
    }

    [Fact]
    public async Task Should_LoadTheDailyGameThatWasSaved_When_ItIsReadBack()
    {
        JSInterop.Setup<string?>("localStorage.getItem", DailyKey)
            .SetResult("""{"date":"2026-11-03","guesses":[["Red","Red","Blue","Blue"]]}""");

        var saved = await CreateStorage().LoadDailyGameAsync();

        saved.Should().BeEquivalentTo(new { Date = new DateOnly(2026, 11, 3), Guesses = new[] { new[] { CodePeg.Red, CodePeg.Red, CodePeg.Blue, CodePeg.Blue } } });
    }

    [Fact]
    public async Task Should_WriteTheGuessesUnderTheEvilKey_When_TheEvilGameIsSaved()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();

        await CreateStorage().SaveEvilGameAsync(new SavedEvilGame([[CodePeg.Red, CodePeg.Red, CodePeg.Blue, CodePeg.Blue]]));

        var save = JSInterop.VerifyInvoke("localStorage.setItem");
        save.Arguments[0].Should().Be(EvilKey);
        save.Arguments[1].Should().Be("""{"guesses":[["Red","Red","Blue","Blue"]]}""");
    }

    [Fact]
    public async Task Should_LoadTheEvilGameThatWasSaved_When_ItIsReadBack()
    {
        JSInterop.Setup<string?>("localStorage.getItem", EvilKey).SetResult("""{"guesses":[["Red","Red","Blue","Blue"]]}""");

        var saved = await CreateStorage().LoadEvilGameAsync();

        saved.Should().BeEquivalentTo(new { Guesses = new[] { new[] { CodePeg.Red, CodePeg.Red, CodePeg.Blue, CodePeg.Blue } } });
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("{}")]
    [InlineData("""{"guesses":null}""")]
    [InlineData("""{"guesses":[["Red","Red","Blue","Purple"]]}""")]
    public async Task Should_ReturnNull_When_TheSavedEvilGameIsDamaged(string json)
    {
        JSInterop.Setup<string?>("localStorage.getItem", EvilKey).SetResult(json);

        var saved = await CreateStorage().LoadEvilGameAsync();

        saved.Should().BeNull();
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("""{"guesses":[]}""")]
    [InlineData("""{"date":"2026-13-40","guesses":[]}""")]
    [InlineData("""{"date":"2026-11-03","guesses":null}""")]
    public async Task Should_ReturnNull_When_TheSavedDailyGameIsDamaged(string json)
    {
        JSInterop.Setup<string?>("localStorage.getItem", DailyKey).SetResult(json);

        var saved = await CreateStorage().LoadDailyGameAsync();

        saved.Should().BeNull();
    }
}
