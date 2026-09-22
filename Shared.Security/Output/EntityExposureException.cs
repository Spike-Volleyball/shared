namespace Shared.Security.Output;

public sealed class EntityExposureException(Type entityType)
    : InvalidOperationException($"{entityType.FullName} is an entity and is never serialised; map it to a DTO.")
{
    public Type EntityType { get; } = entityType;
}
