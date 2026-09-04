namespace GenericParsingEngine.Config;

/// <summary>
/// Defines how records are separated within the source file.
/// </summary>
public class RecordBoundary
{
    /// <summary>
    /// Boundary detection strategy:
    /// - row       : Each line is one record (default for CSV/TSV/Fixed)
    /// - pattern   : A regex pattern marks the start of each new record (PDF)
    /// - blanklines: Two or more consecutive blank lines separate records (PDF)
    /// - pageBreak : Each PDF page is treated as one record
    /// - xpath     : XPath expression selects record nodes (XML)
    /// </summary>
    public string Type { get; set; } = "row";

    /// <summary>Regex pattern for the 'pattern' boundary type.</summary>
    public string? Pattern { get; set; }

    /// <summary>XPath expression for the 'xpath' boundary type (XML parser).</summary>
    public string? Xpath { get; set; }

    /// <summary>Number of consecutive blank lines that constitute a record separator (blanklines type).</summary>
    public int BlankLineCount { get; set; } = 2;
}
