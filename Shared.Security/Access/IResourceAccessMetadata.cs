namespace Shared.Security.Access;

/// <summary>An endpoint's declaration that the resource its route names is checked.</summary>
public interface IResourceAccessMetadata
{
    string RouteParameter { get; }
}
