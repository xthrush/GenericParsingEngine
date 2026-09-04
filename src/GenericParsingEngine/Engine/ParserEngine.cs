using GenericParsingEngine.Config;
using GenericParsingEngine.Models;
using GenericParsingEngine.Parsers;

namespace GenericParsingEngine.Engine;

/// <summary>
/// Orchestrates the parsing pipeline:
/// 1. Load the YAML/JSON config file via <see cref="ConfigLoader"/>.
/// 2. Resolve the appropriate <see cref="IFileParser"/> based on <c>fileType</c>.
/// 3. Delegate parsing and return the resulting records.
/// </summary>
public class ParserEngine
{
    private readonly ConfigLoader _configLoader;

    public ParserEngine()
    {
        _configLoader = new ConfigLoader();
    }

    /// <summary>
    /// Parse <paramref name="filePath"/> using the layout defined in <paramref name="configPath"/>.
    /// </summary>
    /// <param name="filePath">Source data file (PDF, CSV, TXT, XML …).</param>
    /// <param name="configPath">YAML or JSON config file that describes the layout.</param>
    /// <returns>List of extracted <see cref="ParsedRecord"/> instances.</returns>
    /// <exception cref="FileNotFoundException">If the source file does not exist.</exception>
    /// <exception cref="NotSupportedException">If the fileType in the config is unrecognized.</exception>
    public List<ParsedRecord> Process(string filePath, string configPath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Input file not found: {filePath}");

        var config = _configLoader.Load(configPath);
        var parser = ResolveParser(config.FileType);

        return parser.Parse(filePath, config);
    }

    // -------------------------------------------------------------------------
    // Parser registry — add new file types here
    // -------------------------------------------------------------------------

    private static IFileParser ResolveParser(string fileType)
    {
        return fileType.ToLowerInvariant() switch
        {
            "pdf"       => new PdfParser(),
            "delimited" => new DelimitedParser(),
            "fixed"     => new FixedWidthParser(),
            "xml"       => new XmlParser(),
            _ => throw new NotSupportedException(
                $"File type '{fileType}' is not supported. " +
                "Valid types: pdf, delimited, fixed, xml")
        };
    }
}
