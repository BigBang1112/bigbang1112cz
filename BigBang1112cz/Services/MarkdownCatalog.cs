using Markdig;
using Markdig.Extensions.Yaml;
using Markdown.ColorCode;
using Markdig.Syntax;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BigBang1112cz.Services;

/// <summary>
/// Content written as Markdown files with YAML front matter in a directory of the content root.
/// Files starting with an underscore are ignored. In development, the files are reloaded when they change.
/// </summary>
public abstract class MarkdownCatalog<T> : IDisposable
{
    // Code blocks are highlighted here on the server, as CSS classes styled in base.css
    protected static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UseAdvancedExtensions()
        .UseColorCode(HtmlFormatterType.Css, additionalLanguages: [new CSharpLanguage()])
        .Build();

    // The ColorCode formatter in the pipeline writes through shared state, so only one document can be rendered at a time
    private static readonly Lock RenderLock = new();

    // Unknown front matter keys throw, so typos don't go unnoticed
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .Build();

    private readonly string directoryPath;
    private readonly string cacheTag;
    private readonly IFileProvider files;
    private readonly IOutputCacheStore cache;
    private readonly ILogger logger;
    private readonly IDisposable? watcher;

    protected IReadOnlyList<T> Items { get; private set; }

    protected MarkdownCatalog(string directoryPath, string cacheTag, IWebHostEnvironment env, IOutputCacheStore cache, ILogger logger)
    {
        this.directoryPath = directoryPath;
        this.cacheTag = cacheTag;
        files = env.ContentRootFileProvider;
        this.cache = cache;
        this.logger = logger;

        Items = Load();

        if (env.IsDevelopment())
        {
            watcher = ChangeToken.OnChange(() => files.Watch($"{directoryPath}/*.md"), Reload);
        }
    }

    /// <summary>
    /// Parses a single file. Must not depend on instance state, it is called from the constructor.
    /// </summary>
    protected abstract T ParseFile(string fileName, string markdown);

    /// <summary>
    /// Orders the parsed items and validates them as a whole. Must not depend on instance state, it is called from the constructor.
    /// </summary>
    protected abstract IEnumerable<T> Arrange(IEnumerable<T> items);

    private void Reload()
    {
        try
        {
            Items = Load();
        }
        catch (Exception ex)
        {
            // Editors can trigger this mid-save, the next save reloads again
            logger.LogError(ex, "Failed to reload {Directory}, keeping the previous content", directoryPath);
            return;
        }

        logger.LogInformation("Reloaded {Count} items from {Directory}", Items.Count, directoryPath);
        OnItemsReloaded();
        EvictCache();
    }

    /// <summary>
    /// Called after the files were reloaded in development.
    /// </summary>
    protected virtual void OnItemsReloaded()
    {
    }

    protected void EvictCache()
    {
        _ = cache.EvictByTagAsync(cacheTag, CancellationToken.None).AsTask();
    }

    private IReadOnlyList<T> Load()
    {
        var directory = files.GetDirectoryContents(directoryPath);

        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"Directory '{directoryPath}' not found in the content root.");
        }

        var items = directory
            .Where(x => !x.IsDirectory && x.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && !x.Name.StartsWith('_'))
            .Select(x =>
            {
                using var reader = new StreamReader(x.CreateReadStream());
                return ParseFile(x.Name, reader.ReadToEnd());
            });

        return Arrange(items).ToList();
    }

    protected static (TFrontMatter FrontMatter, MarkdownDocument Document) ParseMarkdown<TFrontMatter>(string fileName, string markdown)
        where TFrontMatter : class, new()
    {
        var document = Markdig.Markdown.Parse(markdown, Pipeline);

        var frontMatterBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault()
            ?? throw new InvalidDataException($"{fileName}: missing front matter, the file has to start with a --- block.");

        try
        {
            return (Yaml.Deserialize<TFrontMatter?>(frontMatterBlock.Lines.ToString()) ?? new(), document);
        }
        catch (YamlException ex)
        {
            throw new InvalidDataException($"{fileName}: invalid front matter. {ex.Message}", ex);
        }
    }

    protected static string ToHtml(MarkdownDocument document)
    {
        lock (RenderLock)
        {
            return document.ToHtml(Pipeline);
        }
    }

    protected static string Required(string fileName, string key, string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidDataException($"{fileName}: '{key}' is required.")
            : value;
    }

    protected static DateOnly RequiredDate(string fileName, string key, string? value)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new InvalidDataException($"{fileName}: '{key}' is required in the yyyy-MM-dd format.");
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            watcher?.Dispose();
        }
    }
}
