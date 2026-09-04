namespace GenericParsingEngine.Config;

/// <summary>
/// Describes how to extract and convert a single field from the source data.
/// </summary>
public class FieldDefinition
{
    // -------------------------------------------------------------------------
    // Common properties
    // -------------------------------------------------------------------------

    /// <summary>Output field name in the ParsedRecord dictionary.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Extractor strategy:
    /// - column     : Delimited — by header name or index
    /// - regex      : Match a regular expression pattern (PDF / text)
    /// - labelValue : Find a label and read the value to its right or below (PDF)
    /// - table      : Extract a cell from a structured table block (PDF)
    /// - xpath      : Select an XML element value
    /// - attribute  : Select an XML element attribute value
    /// </summary>
    public string Extractor { get; set; } = string.Empty;

    /// <summary>
    /// Target CLR type for the extracted value:
    /// string | int | decimal | bool | date
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>Date/time format string used when Type = "date".</summary>
    public string? DateFormat { get; set; }

    /// <summary>Trim whitespace from the raw value before conversion. Default: true</summary>
    public bool Trim { get; set; } = true;

    /// <summary>Value used when the raw value is null or empty.</summary>
    public string? DefaultValue { get; set; }

    // -------------------------------------------------------------------------
    // Regex extractor
    // -------------------------------------------------------------------------

    /// <summary>Regular expression pattern. The matched group specified by Group is extracted.</summary>
    public string? Pattern { get; set; }

    /// <summary>Regex capture group index to use. Default: 1</summary>
    public int Group { get; set; } = 1;

    // -------------------------------------------------------------------------
    // LabelValue extractor (PDF)
    // -------------------------------------------------------------------------

    /// <summary>Text label to search for on the page.</summary>
    public string? Label { get; set; }

    /// <summary>
    /// Direction to read the value relative to the label:
    /// - right : value follows the label on the same line
    /// - below : value is on the next line
    /// </summary>
    public string Direction { get; set; } = "right";

    // -------------------------------------------------------------------------
    // Table extractor (PDF)
    // -------------------------------------------------------------------------

    /// <summary>Text that identifies the table's title or header row.</summary>
    public string? TableHeader { get; set; }

    /// <summary>Column header text within the table.</summary>
    public string? Column { get; set; }

    /// <summary>Row label text within the table.</summary>
    public string? Row { get; set; }

    // -------------------------------------------------------------------------
    // Delimited parser
    // -------------------------------------------------------------------------

    /// <summary>Header column name to map from (case-insensitive).</summary>
    public string? SourceColumn { get; set; }

    /// <summary>Zero-based column index (used when HasHeader = false).</summary>
    public int ColumnIndex { get; set; } = -1;

    // -------------------------------------------------------------------------
    // Fixed-width parser
    // -------------------------------------------------------------------------

    /// <summary>Zero-based character position where this field starts.</summary>
    public int StartPos { get; set; }

    /// <summary>Number of characters in this field.</summary>
    public int Length { get; set; }

    // -------------------------------------------------------------------------
    // XML parser
    // -------------------------------------------------------------------------

    /// <summary>XPath expression relative to the record node.</summary>
    public string? Xpath { get; set; }

    /// <summary>Attribute name to read from the XPath-selected element.</summary>
    public string? Attribute { get; set; }
}
