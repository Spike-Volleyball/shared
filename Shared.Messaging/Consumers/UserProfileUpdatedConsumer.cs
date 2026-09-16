using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Messaging.Contracts.Events.Profiles;
using Shared.Models;

namespace Shared.Messaging.Consumers
{
    /// <summary>
    /// Keeps this service's copy of a profile in step with profiles-service. A repeated delivery
    /// rewrites the same values. When two deliveries for an unseen profile arrive at once, both
    /// find no row and both insert; the one that loses on the primary key updates the winner's row
    /// instead of failing.
    /// </summary>
    public class UserProfileUpdatedConsumer : IConsumer<UserProfileUpdatedEvent>
    {
        private readonly IRepository<UserProfile> _repository;

        public UserProfileUpdatedConsumer(IRepository<UserProfile> repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<UserProfileUpdatedEvent> context)
        {
            var userProfile = context.Message.UserProfile;

            var existingProfile = await _repository.GetByIdAsync(userProfile.Id);
            if (existingProfile != null)
            {
                await UpdateAsync(existingProfile, userProfile);
                return;
            }

            _repository.Add(userProfile);
            try
            {
                await _repository.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                               {
                                                   SqlState: PostgresErrorCodes.UniqueViolation
                                               })
            {
                // Still tracked as Added, the failed insert would be re-issued by the next save.
                _repository.Detach(userProfile);

                // No row under this id means another unique index failed, not the race.
                existingProfile = await _repository.GetByIdAsync(userProfile.Id);
                if (existingProfile == null) throw;

                await UpdateAsync(existingProfile, userProfile);
            }
        }

        private async Task UpdateAsync(UserProfile existingProfile, UserProfile userProfile)
        {
            existingProfile.Name = userProfile.Name;
            existingProfile.Surname = userProfile.Surname;
            existingProfile.ImageUrl = userProfile.ImageUrl;
            existingProfile.Email = userProfile.Email;
            existingProfile.DateOfBirth = userProfile.DateOfBirth;
            existingProfile.IsActive = userProfile.IsActive;
            existingProfile.IsEmailVerified = userProfile.IsEmailVerified;
            _repository.Update(existingProfile);

            await _repository.SaveChangesAsync();
        }
    }
}
