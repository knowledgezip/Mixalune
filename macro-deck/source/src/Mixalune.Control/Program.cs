using MacroDeck.Plugin.Hosting;
using MacroDeck.Plugin.Serilog;
using Microsoft.Extensions.DependencyInjection;
using Mixalune.Control;

var builder = MacroDeckPlugin.CreatePlugin(args).UseMacroDeckLogging();
builder.Services.AddSingleton<MixaluneStateManager>();
builder.Services.AddSingleton<MixaluneConnectionManager>();
var plugin = builder.RegisterIntegration<MixaluneIntegration>().Build();
await plugin.RunAsync();
