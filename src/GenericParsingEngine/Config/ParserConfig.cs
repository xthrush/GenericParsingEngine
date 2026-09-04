namespace GenericParsingEngine.Config;

/// <summary>
/// Top-level configuration for the parsing engine, loaded from YAML or JSON.
/// </summary>
public class ParserConfig
{
    /// <summary>File type: pdf | delimited | fixed | xml</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>Human-readable description of this layout configuration.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Defines how records are delimited within the file.</summary>
    public RecordBoundary RecordBoundary { get; set; } = new();

    // --- Delimited-specific ---

    /// <summary>Column delimiter character(s). Default: ","</summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>Whether the first data row (after SkipRows) is a header row.</summary>
    public bool HasHeader { get; set; } = true;

    /// <summary>Number of leading rows to skip before reading data (e.g. file-level headers).</summary>
    public int SkipRows { get; set; } = 0;

    /// <summary>Field extraction definitions.</summary>
    public List<FieldDefinition> Fields { get; set; } = new();
}
