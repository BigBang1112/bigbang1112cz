using ColorCode;
using ColorCode.Common;

namespace BigBang1112cz.Services;

/// <summary>
/// The built-in ColorCode C# grammar, extended with type names, which it doesn't highlight on its own.
/// Types are guessed from PascalCase names and where they appear, as there's no compiler to ask.
/// </summary>
public sealed class CSharpLanguage : ILanguage
{
    // Generic arguments like <CGameCtnChallenge> or <string, List<int>>
    private const string GenericArgs = @"<[\w\s,.?<>\[\]]*>";

    // Names in using and namespace directives are namespaces, not types
    private const string NotNamespace = @"(?<!^\s*(?:global\s+)?using\s+(?:static\s+)?[\w.]*)(?<!\bnamespace\s+[\w.]*)";

    // Contextual keywords that can follow a name without it being a declaration, like "item is null"
    private const string NotKeyword = @"(?!(?:is|as|in|and|or|not|when|with|switch|where|on|equals|into|by)\b)";

    private static readonly Dictionary<int, string> TypeCapture = new() { [1] = ScopeName.Type };

    // Captured before this language replaces it under the same ID, after which Languages.CSharp returns this one
    private static readonly ILanguage BuiltIn = Languages.CSharp;

    public string Id => BuiltIn.Id;
    public string Name => BuiltIn.Name;
    public string CssClassName => BuiltIn.CssClassName;
    public string? FirstLinePattern => BuiltIn.FirstLinePattern;

    public IList<LanguageRule> Rules { get; } =
    [
        .. BuiltIn.Rules,

        // Declared types: class CGameCtnChallenge, interface IGameCtnMediaTrackTMF
        new(@"(?<=\b(?:class|struct|interface|enum|record)\s+)(@?[A-Za-z_]\w*)", TypeCapture),

        // Base types: class MapTests : TestBase, IDisposable
        new($@"(?<=\b(?:class|struct|interface|record)\s+\w+(?:{GenericArgs})?\s*:\s*(?:[\w.]+(?:{GenericArgs})?\s*,\s*)*)([A-Z]\w*)", TypeCapture),

        // Constructed and checked types: new CGameCtnChallenge(), typeof(CGameCtnChallenge), x is CGameCtnChallenge
        new(@"(?<=\b(?:new|typeof|sizeof|default|is|as)\s*\(?\s*)([A-Z]\w*)", TypeCapture),

        // Attributes: [Test], [Chunk(0x03043002)]
        // ColorCode counts groups by every unescaped "(", even in character classes, so it has to be escaped there
        new(@"(?<=^\s*\[)([A-Z]\w*)(?=[\]\(])", TypeCapture),

        // Type arguments: List<CGameCtnBlock>, Dictionary<string, CGameCtnBlock>
        new(@"(?<=<)([A-Z]\w*)", TypeCapture),
        new($@"(?<=,\s*)([A-Z]\w*)(?=(?:{GenericArgs})?\s*>)", TypeCapture),

        // Declarations, static access and generic types: CGameCtnChallenge map, Gbx.Parse, List<int>
        new($@"{NotNamespace}(?<![\w.])([A-Z]\w*)(?={GenericArgs}|(?:\[\])*\??\s+{NotKeyword}@?[A-Za-z_]|\.)", TypeCapture),
    ];

    public bool HasAlias(string lang) => BuiltIn.HasAlias(lang);
}
