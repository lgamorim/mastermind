using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Mastermind.Core;
using Mastermind.WebApp;
using Mastermind.WebApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<ISecretCodeGenerator, RandomSecretCodeGenerator>();
builder.Services.AddScoped<GameStateService>();

await builder.Build().RunAsync();
