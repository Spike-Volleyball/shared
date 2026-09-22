using System.Text.Json;

namespace Shared.Testing.Security;

/// <summary>
/// Reads what a response actually carried on the wire. A privacy assertion made on the C# type a
/// test deserialises into passes while the JSON still holds the field, because the type simply
/// has no property to receive it.
/// </summary>
public static class ResponseJson
{
    /// <summary>A person's contact details and age - never part of how someone is shown to others.</summary>
    public static readonly string[] PersonalDataKeys =
        ["email", "isEmailVerified", "phoneNumber", "dateOfBirth"];

    /// <summary>How a participant is paying - the organiser's business, not a fellow player's.</summary>
    public static readonly string[] PaymentKeys =
        ["paymentId", "paymentKey", "paymentStatus", "paymentMethod", "reservedUntilAt"];

    /// <summary>
    /// The JSON path of every property named in <paramref name="keys"/> that holds a value, at any
    /// depth. A null is a field the viewer was not given, not one they were.
    /// </summary>
    public static async Task<IReadOnlyList<string>> PathsOfAsync(HttpResponseMessage response, IEnumerable<string> keys)
    {
        var wanted = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var found = new List<string>();
        Collect(document.RootElement, "$", wanted, found);
        return found;
    }

    private static void Collect(JsonElement element, string path, HashSet<string> wanted, List<string> found)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = $"{path}.{property.Name}";
                    if (wanted.Contains(property.Name) && property.Value.ValueKind != JsonValueKind.Null)
                        found.Add(propertyPath);
                    Collect(property.Value, propertyPath, wanted, found);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                    Collect(item, $"{path}[{index++}]", wanted, found);
                break;
        }
    }
}
