using System.Data.Common;
using FluentAssertions;
using FluentAssertions.Equivalency;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shared.DataAccess;
using Shared.DataAccess.Repositories;
using Shared.Messaging.Consumers;
using Shared.Messaging.Contracts.Events.Profiles;
using Shared.Models;
using Shared.Testing.Base;

namespace Shared.Tests.Messaging;

/// <summary>
/// Real Postgres, because the race is the database's: only a real primary key makes the second of
/// two inserts fail. Each delivery gets a context of its own, as it would on its own endpoint scope.
/// </summary>
public class UserProfileUpdatedConsumerTests : IntegrationTestBase
{
    public enum ReplicaShape
    {
        /// <summary>A DbSet names the table "UserProfiles" (events, clubs, notifications, payments, social).</summary>
        PluralTable,

        /// <summary>
        /// coaching reaches the entity only through navigations, so EF names its table after the
        /// class: "UserProfile".
        /// </summary>
        SingularTable,

        /// <summary>No service has this; it gives an insert something other than the key to fail on.</summary>
        UniqueEmail
    }

    private static readonly DateTimeOffset T0 = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private FakeTimeProvider _time = null!;

    [OneTimeSetUp]
    public async Task CreateReplicaTables()
    {
        foreach (var shape in Enum.GetValues<ReplicaShape>())
        {
            await using var context = CreateContext(shape, TimeProvider.System);
            await context.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
        }
    }

    [SetUp]
    public void SetUp() => _time = new FakeTimeProvider(T0);

    [TestCase(ReplicaShape.PluralTable)]
    [TestCase(ReplicaShape.SingularTable)]
    public async Task Consume_NewProfile_InsertsTheWholePayload(ReplicaShape shape)
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        await ConsumeAsync(shape, Profile(id));

        // Assert
        var row = (await RowsAsync(shape, id)).Should().ContainSingle().Subject;
        row.Should().BeEquivalentTo(Profile(id), ExcludingAuditColumns);
        row.CreatedAt.Should().Be(T0.UtcDateTime);
        row.UpdatedAt.Should().Be(T0.UtcDateTime);
    }

    [TestCase(ReplicaShape.PluralTable)]
    [TestCase(ReplicaShape.SingularTable)]
    public async Task Consume_TwoDeliveriesOfANewProfileAtOnce_WriteOneRowWithoutFailing(ReplicaShape shape)
    {
        // Arrange
        var id = Guid.NewGuid();
        var bothInserting = new InsertBarrier(parties: 2);

        // Act
        var act = () => Task.WhenAll(
            ConsumeAsync(shape, Profile(id), bothInserting),
            ConsumeAsync(shape, Profile(id), bothInserting));

        // Assert
        await act.Should().NotThrowAsync();
        (await RowsAsync(shape, id)).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(Profile(id), ExcludingAuditColumns);
    }

    [TestCase(ReplicaShape.PluralTable)]
    [TestCase(ReplicaShape.SingularTable)]
    public async Task Consume_ProfileAlreadyReplicated_UpdatesTheReplicatedFields(ReplicaShape shape)
    {
        // Arrange
        var id = Guid.NewGuid();
        await ConsumeAsync(shape, Profile(id));
        _time.Advance(TimeSpan.FromHours(1));
        var changed = new UserProfile
        {
            Id = id,
            Name = "Lucía",
            Surname = "Ferrer",
            ImageUrl = "https://cdn.example/lucia-2.png",
            Email = "lucia.ferrer@example.com",
            DateOfBirth = new DateTime(2007, 5, 9, 0, 0, 0, DateTimeKind.Utc),
            IsActive = false,
            IsEmailVerified = false
        };

        // Act
        await ConsumeAsync(shape, changed);

        // Assert
        var row = (await RowsAsync(shape, id)).Should().ContainSingle().Subject;
        row.Should().BeEquivalentTo(changed, o => o
            .Including(p => p.Name)
            .Including(p => p.Surname)
            .Including(p => p.ImageUrl)
            .Including(p => p.Email)
            .Including(p => p.DateOfBirth)
            .Including(p => p.IsActive)
            .Including(p => p.IsEmailVerified));
        row.CreatedAt.Should().Be(T0.UtcDateTime);
        row.UpdatedAt.Should().Be(T0.AddHours(1).UtcDateTime);
    }

    [Test]
    public async Task Consume_InsertCollidesWithAnotherProfilesRow_Throws()
    {
        // Arrange
        var other = Profile(Guid.NewGuid());
        await ConsumeAsync(ReplicaShape.UniqueEmail, other);
        var sameEmail = Profile(Guid.NewGuid());

        // Act
        var act = () => ConsumeAsync(ReplicaShape.UniqueEmail, sameEmail);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
        (await RowsAsync(ReplicaShape.UniqueEmail, sameEmail.Id)).Should().BeEmpty();
    }

    private static EquivalencyAssertionOptions<UserProfile> ExcludingAuditColumns(EquivalencyAssertionOptions<UserProfile> options) =>
        options.Excluding(p => p.CreatedAt).Excluding(p => p.UpdatedAt);

    /// <summary>The same payload for an id every time, as a redelivery would carry.</summary>
    private static UserProfile Profile(Guid id) => new()
    {
        Id = id,
        Name = "Lucia",
        Surname = "Moreno",
        ImageUrl = "https://cdn.example/lucia.png",
        ImageThumbHash = "lucia-thumb",
        Email = "lucia@example.com",
        PhoneNumber = "+447700900123",
        DateOfBirth = new DateTime(2008, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        IsActive = true,
        IsEmailVerified = true
    };

    private async Task ConsumeAsync(ReplicaShape shape, UserProfile profile, params IInterceptor[] interceptors)
    {
        await using var context = CreateContext(shape, _time, interceptors);
        var sut = new UserProfileUpdatedConsumer(new BaseRepository<UserProfile>(context));
        var delivery = Substitute.For<ConsumeContext<UserProfileUpdatedEvent>>();
        delivery.Message.Returns(new UserProfileUpdatedEvent { UserProfile = profile });

        await sut.Consume(delivery);
    }

    private async Task<List<UserProfile>> RowsAsync(ReplicaShape shape, Guid id)
    {
        await using var context = CreateContext(shape, _time);
        return await context.Set<UserProfile>().AsNoTracking().Where(p => p.Id == id).ToListAsync();
    }

    private static BaseDbContext CreateContext(ReplicaShape shape, TimeProvider time, params IInterceptor[] interceptors) =>
        shape switch
        {
            ReplicaShape.PluralTable => new PluralTableContext(OptionsFor<PluralTableContext>(interceptors), time),
            ReplicaShape.SingularTable => new SingularTableContext(OptionsFor<SingularTableContext>(interceptors), time),
            ReplicaShape.UniqueEmail => new UniqueEmailContext(OptionsFor<UniqueEmailContext>(interceptors), time),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
        };

    private static DbContextOptions<TContext> OptionsFor<TContext>(IInterceptor[] interceptors)
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(PostgresFixture.ConnectionString)
            .AddInterceptors(interceptors)
            .Options;

    private sealed class PluralTableContext(DbContextOptions<PluralTableContext> options, TimeProvider time)
        : BaseDbContext(options, time)
    {
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    }

    private sealed class SingularTableContext(DbContextOptions<SingularTableContext> options, TimeProvider time)
        : BaseDbContext(options, time)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<UserProfile>();
    }

    private sealed class UniqueEmailContext(DbContextOptions<UniqueEmailContext> options, TimeProvider time)
        : BaseDbContext(options, time)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<UserProfile>(profile =>
            {
                profile.ToTable("UniqueEmailProfiles");
                profile.HasIndex(p => p.Email).IsUnique();
            });
    }

    /// <summary>
    /// Holds every INSERT until all parties have reached one, so each delivery has already found no
    /// row before any of them writes it: the interleaving production hits only by chance.
    /// </summary>
    private sealed class InsertBarrier(int parties) : DbCommandInterceptor
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

        private readonly TaskCompletionSource _allArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await WaitIfInsertAsync(command, cancellationToken);
            return result;
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await WaitIfInsertAsync(command, cancellationToken);
            return result;
        }

        private Task WaitIfInsertAsync(DbCommand command, CancellationToken cancellationToken)
        {
            if (!command.CommandText.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            if (Interlocked.Increment(ref _arrived) == parties)
                _allArrived.TrySetResult();

            return _allArrived.Task.WaitAsync(Patience, cancellationToken);
        }
    }
}
