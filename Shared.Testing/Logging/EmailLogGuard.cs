using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shared.Testing.Logging;

/// <summary>
/// Finds log statements that would write a raw email address. Logs ship to a store with its own
/// access and retention, so an address may reach one only through
/// <c>ILogPseudonymizer.PseudonymizeEmail</c>, under the <c>{EmailRef}</c> placeholder. It reads
/// what a reviewer reads — placeholder names and the names of the values passed — so a statement
/// is flagged for either an address-shaped placeholder or an address-shaped value.
/// </summary>
public static partial class EmailLogGuard
{
    private const string SanctionedPlaceholder = "EmailRef";
    private const string SanctionedCallPrefix = "Pseudonymize";

    private static readonly HashSet<string> LoggerMethods =
        ["Log", "LogTrace", "LogDebug", "LogInformation", "LogWarning", "LogError", "LogCritical", "BeginScope"];

    private static readonly HashSet<string> LoggerMessageFactories = ["Define", "DefineScope"];

    private static readonly string[] BuildOutput = ["bin", "obj"];

    /// <summary>One entry per offending statement, as <c>relative/path.cs:line: statement</c>.</summary>
    /// <param name="skippedDirectories">Nested repositories that answer for their own sources.</param>
    public static IReadOnlyList<string> FindRawEmailLogStatements(string sourceRoot, params string[] skippedDirectories)
    {
        var skipped = BuildOutput.Concat(skippedDirectories).ToHashSet(StringComparer.Ordinal);

        return Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(sourceRoot, file))
            .Where(file => !file.Split(Path.DirectorySeparatorChar).SkipLast(1).Any(skipped.Contains))
            .SelectMany(file => Inspect(File.ReadAllText(Path.Combine(sourceRoot, file)), file))
            .ToList();
    }

    public static IEnumerable<string> Inspect(string source, string path)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();

        var calls = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsLogging)
            .Select(call => (Statement: (SyntaxNode)call, Arguments: (SyntaxNode)call.ArgumentList));

        var attributes = root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(attribute => SimpleName(attribute.Name) is "LoggerMessage" or "LoggerMessageAttribute")
            .Where(attribute => attribute.ArgumentList is not null)
            .Select(attribute => (Statement: (SyntaxNode)attribute, Arguments: (SyntaxNode)attribute.ArgumentList!));

        return calls.Concat(attributes)
            .Where(found => NamesAnAddress(found.Arguments) || PassesAnAddress(found.Arguments))
            .Select(found => $"{path}:{LineOf(found.Statement)}: {Whitespace().Replace(found.Statement.ToString(), " ")}");
    }

    /// <summary>
    /// The nearest directory above the running tests that contains <paramref name="marker"/>,
    /// such as a repository's solution file.
    /// </summary>
    public static string LocateSourceRoot(string marker)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, marker)))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException($"No directory above {AppContext.BaseDirectory} contains {marker}");
    }

    private static bool IsLogging(InvocationExpressionSyntax call)
    {
        var name = InvokedName(call);
        return LoggerMethods.Contains(name)
            || (LoggerMessageFactories.Contains(name)
                && call.Expression is MemberAccessExpressionSyntax { Expression: var receiver }
                && receiver.ToString().EndsWith("LoggerMessage", StringComparison.Ordinal));
    }

    private static bool NamesAnAddress(SyntaxNode arguments) =>
        Templates(arguments)
            .SelectMany(template => Placeholder().Matches(template))
            .Select(placeholder => placeholder.Groups["name"].Value)
            .Any(name => name != SanctionedPlaceholder && name.Contains("mail", StringComparison.OrdinalIgnoreCase));

    // Raw source text for interpolated strings, where "{{" is still the message-template escape.
    private static IEnumerable<string> Templates(SyntaxNode arguments) =>
        arguments.DescendantNodes()
            .Select(node => node switch
            {
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal.Token.ValueText,
                InterpolatedStringTextSyntax text => text.TextToken.Text,
                _ => null
            })
            .OfType<string>();

    private static bool PassesAnAddress(SyntaxNode arguments) =>
        arguments.DescendantNodes(descendIntoChildren: node => !IsSanctioned(node))
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => AddressLikeName().IsMatch(identifier.Identifier.ValueText));

    private static bool IsSanctioned(SyntaxNode node) =>
        node is InvocationExpressionSyntax call
        && (InvokedName(call).StartsWith(SanctionedCallPrefix, StringComparison.Ordinal) || InvokedName(call) == "nameof");

    private static string InvokedName(InvocationExpressionSyntax call) => call.Expression switch
    {
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
        SimpleNameSyntax name => name.Identifier.ValueText,
        _ => string.Empty
    };

    private static string SimpleName(NameSyntax name) => name switch
    {
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax aliased => aliased.Name.Identifier.ValueText,
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        _ => string.Empty
    };

    private static int LineOf(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    [GeneratedRegex(@"(?<!\{)\{(?!\{)[@$]?(?<name>\w+)")]
    private static partial Regex Placeholder();

    [GeneratedRegex(@"^\w*emails?$", RegexOptions.IgnoreCase)]
    private static partial Regex AddressLikeName();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
