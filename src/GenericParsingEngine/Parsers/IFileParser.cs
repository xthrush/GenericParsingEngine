using GenericParsingEngine.Config;
using GenericParsingEngine.Models;

namespace GenericParsingEngine.Parsers;

/// <summary>
/// Contract for all file-type-specific parsers.
/// Implement this interface to add support for a new file format.
/// </summary>
public interface IFileParser
{
    /// <summary>
    /// Parse the given file using the supplied configuration and return all extracted records.
    /// </summary>
    /// <param name="filePath">Absolute or relative path to the source file.</param>
    /// <param name="config">Layout configuration loaded from YAML or JSON.</param>
    /// <returns>A list of <see cref="ParsedRecord"/> instances, one per logical record.</returns>
    List<ParsedRecord> Parse(string filePath, ParserConfig config);
}
