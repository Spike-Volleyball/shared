using System.Runtime.CompilerServices;

namespace Shared.Testing.Security;

/// <summary>
/// A list committed beside the test as <c>{name}.approved.txt</c> and reviewed like code. A
/// difference writes <c>{name}.received.txt</c> next to it and is reported; accepting a change is
/// renaming the received file over the approved one.
/// </summary>
public static class ApprovedSnapshot
{
    /// <returns>Null when the lines match the approved file, otherwise what differs.</returns>
    public static string? Compare(IReadOnlyList<string> received, string name, [CallerFilePath] string testSourceFile = "")
    {
        var directory = Path.GetDirectoryName(testSourceFile)!;
        var approvedPath = Path.Combine(directory, $"{name}.approved.txt");
        var receivedPath = Path.Combine(directory, $"{name}.received.txt");

        var approved = File.Exists(approvedPath) ? File.ReadAllLines(approvedPath) : null;
        if (approved is not null && approved.SequenceEqual(received))
        {
            File.Delete(receivedPath);
            return null;
        }

        File.WriteAllLines(receivedPath, received);
        if (approved is null)
            return $"No approved {name} yet. Review {receivedPath} and rename it to {name}.approved.txt.";

        var added = received.Except(approved).Select(line => "+ " + line);
        var removed = approved.Except(received).Select(line => "- " + line);
        return $"{name} changed; review {receivedPath}:{Environment.NewLine}"
            + string.Join(Environment.NewLine, added.Concat(removed));
    }
}
