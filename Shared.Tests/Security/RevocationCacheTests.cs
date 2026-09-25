using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Security.Authentication;
using Shared.Services;

namespace Shared.Tests.Security;

[TestFixture]
[Category("Unit")]
public class RevocationCacheTests
{
    [Test]
    public void WhereTheServicesCachePrefixesItsKeys_RevocationsAreReadThroughAnotherRedisCache()
    {
        // Arrange — social's cache puts "social:" before every key, and auth writes none
        using var provider = Build(services => services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379";
            options.InstanceName = "social:";
        }));

        // Act
        var revocations = provider.GetRequiredService<RevocationCache>().Cache;

        // Assert
        revocations.Should().BeOfType<RedisCache>()
            .And.NotBeSameAs(provider.GetRequiredService<IDistributedCache>());
    }

    [Test]
    public void WhereTheServicesRedisCacheAddsNoPrefix_RevocationsShareIt()
    {
        // Arrange
        using var provider = Build(services => services.AddStackExchangeRedisCache(options =>
            options.Configuration = "localhost:6379"));

        // Act
        var revocations = provider.GetRequiredService<RevocationCache>().Cache;

        // Assert
        revocations.Should().BeSameAs(provider.GetRequiredService<IDistributedCache>());
    }

    [Test]
    public void WhereTheServicesCacheIsInMemory_RevocationsShareIt()
    {
        // Arrange — as every service's test host has it, so a test that revokes is seen
        using var provider = Build(services => services.AddDistributedMemoryCache());

        // Act
        var revocations = provider.GetRequiredService<RevocationCache>().Cache;

        // Assert
        revocations.Should().BeSameAs(provider.GetRequiredService<IDistributedCache>());
    }

    private static ServiceProvider Build(Action<IServiceCollection> addCache)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-for-security-tests-minimum-32-characters"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSpikeAuthentication(configuration);
        addCache(services);
        return services.BuildServiceProvider();
    }
}
