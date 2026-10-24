using MacroDeck.Plugin.Protocol.Capabilities.ConfigFlow;
using MacroDeck.Plugin.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Text.Json;

namespace Mixalune.Control.Tests;

[TestFixture]
public sealed class ConfigurationTests
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Configuration_tests_authentication_and_marks_token_secret(bool valid)
    {
        await using var server = new MockMixalune();
        await server.StartAsync();
        await using var harness = PluginTestHarness.Create(builder =>
        {
            builder.Services.AddSingleton<MixaluneStateManager>();
            builder.Services.AddSingleton<MixaluneConnectionManager>();
            builder.RegisterIntegration<MixaluneIntegration>();
        });
        await harness.InitializeIntegrationsAsync();
        var oauth = new ConfigFlowOAuthContextDto { RedirectUri = "http://localhost/callback", State = "test" };
        var start = (await harness.ConfigFlow.StartAsync(new FlowStartArguments { SessionId = "config-test", OAuth = oauth })).DataAs<ConfigFlowResultDto>();
        Assert.That(start!.NextStep!.Fields.Single(x => x.Name == "token").Type, Is.EqualTo("Secret"));
        var input = new Dictionary<string, object?>
        {
            ["port"] = server.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["token"] = valid ? MockMixalune.Token : new string('x', 40),
            ["connectionTimeout"] = "2", ["requestTimeout"] = "1"
        };
        var result = (await harness.ConfigFlow.SubmitAsync(new FlowSubmitArguments
        {
            SessionId = "config-test", StepId = "connection", OAuth = oauth,
            Input = input.ToDictionary(x => x.Key, x => JsonSerializer.SerializeToElement(x.Value))
        })).DataAs<ConfigFlowResultDto>();
        Assert.That(result!.Kind, Is.EqualTo(valid ? "Complete" : "Error"));
        if (valid)
        {
            Assert.That(result.Values!["token"].IsSecret, Is.True);
            Assert.That(result.Values["port"].IsSecret, Is.False);
        }
        else Assert.That(result.ErrorMessage?.Literal, Does.Not.Contain(MockMixalune.Token));
        Assert.That(harness.Logs.Events.Any(x => x.Message.Contains(MockMixalune.Token, StringComparison.Ordinal)), Is.False);
    }
}
