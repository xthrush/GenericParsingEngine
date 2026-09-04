namespace GenericParsingEngine.Models;

/// <summary>
/// Represents a horizontal line of text extracted from a PDF page,
/// with positional bounding-box metadata for layout-aware extraction.
/// </summary>
public class TextLine
{
    /// <summary>Reconstructed text content of this line.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Vertical position (Y coordinate from bottom of page) of the line.</summary>
    public double Y { get; set; }

    /// <summary>Left edge X coordinate of the leftmost word on this line.</summary>
    public double MinX { get; set; }

    /// <summary>Right edge X coordinate of the rightmost word on this line.</summary>
    public double MaxX { get; set; }
}
