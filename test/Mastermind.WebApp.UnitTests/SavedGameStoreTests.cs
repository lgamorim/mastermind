using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Mastermind.Core;
using Mastermind.WebApp.Services;
using Microsoft.JSInterop;
using Xunit;

namespace Mastermind.WebApp.UnitTests;

public class SavedGameStoreTests : BunitContext
{
    private const string StorageKey = "mastermind.savedGame.v1";

    private static readonly SavedGame SampleGame = new(
        [CodePeg.Red, CodePeg.Blue, CodePeg.Yellow, CodePeg.Green],
        [[CodePeg.White, CodePeg.White, CodePeg.Black, CodePeg.Black]],
        RevealSecretFromStart: true);

    private SavedGameStore CreateStore() => new(JSInterop.JSRuntime);

    [Fact]
    public async Task Should_ReturnNull_When_NothingIsSaved()
    {
        JSInterop.Setup<string?>("localStorage.getItem", StorageKey).SetResult(null);

        var savedGame = await CreateStore().LoadAsync();

        savedGame.Should().BeNull();
    }

    [Fact]
    public async Task Should_WriteColorsByNameUnderAVersionedKey_When_AGameIsSaved()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();

        await CreateStore().SaveAsync(SampleGame);

        var invocation = JSInterop.VerifyInvoke("localStorage.setItem");
        invocation.Arguments[0].Should().Be(StorageKey);
        invocation.Arguments[1].Should().BeOfType<string>().Which.Should().Contain("\"Red\"").And.Contain("\"White\"");
    }

    [Fact]
    public async Task Should_LoadWhatWasSaved_When_ASaveIsReadBack()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();
        var store = CreateStore();
        await store.SaveAsync(SampleGame);
        var json = (string?)JSInterop.VerifyInvoke("localStorage.setItem").Arguments[1];
        JSInterop.Setup<string?>("localStorage.getItem", StorageKey).SetResult(json);

        var savedGame = await store.LoadAsync();

        savedGame.Should().BeEquivalentTo(SampleGame);
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("""{"secretCode":["Red","Blue","Yellow","Purple"],"guesses":[],"revealSecretFromStart":false}""")]
    [InlineData("""{"secretCode":["Red","Blue","Yellow","Green"],"revealSecretFromStart":false}""")]
    [InlineData("""{"secretCode":null,"guesses":[],"revealSecretFromStart":false}""")]
    public async Task Should_ReturnNull_When_TheSaveIsDamaged(string json)
    {
        JSInterop.Setup<string?>("localStorage.getItem", StorageKey).SetResult(json);

        var savedGame = await CreateStore().LoadAsync();

        savedGame.Should().BeNull();
    }

    [Fact]
    public async Task Should_ReturnNull_When_StorageIsUnavailable()
    {
        JSInterop.Setup<string?>("localStorage.getItem", StorageKey).SetException(new JSException("Storage is blocked."));

        var savedGame = await CreateStore().LoadAsync();

        savedGame.Should().BeNull();
    }

    [Fact]
    public async Task Should_KeepPlayingUnsaved_When_StorageRejectsTheSave()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetException(new JSException("Quota exceeded."));

        var save = () => CreateStore().SaveAsync(SampleGame);

        await save.Should().NotThrowAsync();
    }
}
