using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Options;
using Shared.Services.Extensions;
using Shared.Services.Logging;
using Shared.Tests.Services.Analytics;

namespace Shared.Tests.Services.Logging;

[TestFixture]
[Category("Unit")]
public class LogPseudonymizationKeyCheckTests
{
    private static async Task<RecordingLogger<LogPseudonymizationKeyCheck>> StartWith(string? key)
    {
        var logger = new RecordingLogger<LogPseudonymizationKeyCheck>();
        var check = new LogPseudonymizationKeyCheck(
            Microsoft.Extensions.Options.Options.Create(new LogPseudonymizationSettings { HmacKey = key }), logger);

        await check.StartAsync(CancellationToken.None);
        return logger;
    }

    [TestCase(null)]
    [TestCase("short-key")]
    public async Task StartAsync_WithoutAUsableKey_WarnsOnceAndNamesTheSetting(string? key)
    {
        // Act
        var logger = await StartWith(key);

        // Assert
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Warning);
        logger.Entries[0].Message.Should().Contain("LogPseudonymization:HmacKey");
    }

    [Test]
    public async Task StartAsync_WithAUsableKey_StaysQuiet()
    {
        // Act
        var logger = await StartWith("spike-test-log-pseudonymization-key-0001");

        // Assert
        logger.Entries.Should().BeEmpty();
    }

    /// <summary>
    /// Every service registers the shared services, and every one that sends email resolves the
    /// pseudonymizer through them — including the ones with no key yet, which must still start.
    /// </summary>
    [Test]
    public void AddSharedServices_WithoutAKey_RegistersAMaskingPseudonymizerAndTheStartupCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        // Act
        services.AddSharedServices();
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<ILogPseudonymizer>().PseudonymizeEmail("john.doe@example.com")
            .Should().Be("j***@e***.com");
        provider.GetServices<IHostedService>().Should().ContainSingle(s => s is LogPseudonymizationKeyCheck);
    }

    [Test]
    public void AddSharedServices_WithAKey_RegistersAPseudonymizerThatHashes()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LogPseudonymization:HmacKey"] = "spike-test-log-pseudonymization-key-0001"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddSharedServices(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<ILogPseudonymizer>().PseudonymizeEmail("john.doe@example.com")
            .Should().Be("j***@e***.com hmac:52447c619733b888");
    }
}
