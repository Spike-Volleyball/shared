using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Microservices.Extensions;

namespace Shared.Tests.Microservices;

[TestFixture]
[Category("Unit")]
public class MetricsServerRegistrationTests
{
    [Test]
    public void RemovePrometheusMetricsServer_AfterItWasAdded_LeavesNoHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPrometheusMetrics(new ConfigurationBuilder().Build());

        // Act
        services.RemovePrometheusMetricsServer();

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetServices<IHostedService>().Should().BeEmpty();
    }

    [Test]
    public void RemovePrometheusMetricsServer_LeavesTheOtherHostedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHostedService<AnotherHostedService>();
        services.AddPrometheusMetrics(new ConfigurationBuilder().Build());

        // Act
        services.RemovePrometheusMetricsServer();

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetServices<IHostedService>().Should().ContainSingle().Which.Should().BeOfType<AnotherHostedService>();
    }

    [Test]
    public void RemovePrometheusMetricsServer_WhenItWasNeverAdded_ChangesNothing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHostedService<AnotherHostedService>();

        // Act
        services.RemovePrometheusMetricsServer();

        // Assert
        services.Should().ContainSingle();
    }

    private sealed class AnotherHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
