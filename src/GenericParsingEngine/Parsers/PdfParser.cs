using System.Text.RegularExpressions;
using GenericParsingEngine.Config;
using GenericParsingEngine.Extractors;
using GenericParsingEngine.Models;
using UglyToad.PdfPig;

namespace GenericParsingEngine.Parsers;

/// <summary>
/// Parses PDF files using PdfPig for text extraction.
/// Supports three extraction strategies per field:
///   - regex      : regular expression match against line text
///   - labelValue : locate a label token and read the adjacent value
///   - table      : navigate a text-based table by header/column/row
/// </summary>
public class PdfParser : IFileParser
{
    /// <inheritdoc/>
    public List<ParsedRecord> Parse(string filePath, ParserConfig config)
    {
        var allLines = ExtractLines(filePath);
        var blocks   = SplitIntoBlocks(allLines, config.RecordBoundary);

        var records = new List<ParsedRecord>();

        foreach (var block in blocks)
        {
            var record = new ParsedRecord();

            foreach (var field in config.Fields)
            {
                var rawValue = ExtractField(block, field);
                record[field.Name] = FieldValueConverter.Convert(rawValue, field);
            }

            records.Add(record);
        }

        return records;
    }

    // =========================================================================
    // PDF text extraction
    // =========================================================================

    /// <summary>
    /// Open the PDF with PdfPig, group words into visual lines by Y coordinate,
    /// and return them in top-to-bottom reading order across all pages.
    /// </summary>
    private static List<TextLine> ExtractLines(string filePath)
    {
        var lines = new List<TextLine>();

        using var pdf = PdfDocument.Open(filePath);

        foreach (var page in pdf.GetPages())
        {
            // Group words by their rounded bottom-Y coordinate → one group = one visual line
            var lineGroups = page.GetWords()
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                .OrderByDescending(g => g.Key);   // top of page first

            foreach (var lineGroup in lineGroups)
            {
                var words = lineGroup.OrderBy(w => w.BoundingBox.Left).ToList();

                lines.Add(new TextLine
                {
                    Text = string.Join(" ", words.Select(w => w.Text)),
                    Y    = lineGroup.Key,
                    MinX = words.Min(w => w.BoundingBox.Left),
                    MaxX = words.Max(w => w.BoundingBox.Right)
                });
            }
        }

        return lines;
    }

    // =========================================================================
    // Record boundary detection
    // =========================================================================

    private static List<List<TextLine>> SplitIntoBlocks(List<TextLine> lines, RecordBoundary boundary)
    {
        return boundary.Type.ToLowerInvariant() switch
        {
            "pattern"    => SplitByPattern(lines, boundary.Pattern!),
            "blanklines" => SplitByBlankLines(lines, boundary.BlankLineCount),
            _            => new List<List<TextLine>> { lines }  // entire file is one record
        };
    }

    /// <summary>
    /// Each time a line matches <paramref name="pattern"/> a new record block begins.
    /// </summary>
    private static List<List<TextLine>> SplitByPattern(List<TextLine> lines, string pattern)
    {
        var blocks  = new List<List<TextLine>>();
        var regex   = new Regex(pattern, RegexOptions.IgnoreCase);
        List<TextLine>? current = null;

        foreach (var line in lines)
        {
            if (regex.IsMatch(line.Text))
            {
                if (current is not null)
                    blocks.Add(current);

                current = new List<TextLine>();
            }

            current?.Add(line);
        }

        if (current?.Count > 0)
            blocks.Add(current);

        return blocks;
    }

    /// <summary>
    /// A run of <paramref name="threshold"/> or more consecutive blank lines
    /// terminates the current record block.
    /// </summary>
    private static List<List<TextLine>> SplitByBlankLines(List<TextLine> lines, int threshold)
    {
        var blocks     = new List<List<TextLine>>();
        var current    = new List<TextLine>();
        int blankCount = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Text))
            {
                blankCount++;
                if (blankCount >= threshold && current.Count > 0)
                {
                    blocks.Add(current);
                    current    = new List<TextLine>();
                    blankCount = 0;
                }
            }
            else
            {
                blankCount = 0;
                current.Add(line);
            }
        }

        if (current.Count > 0)
            blocks.Add(current);

        return blocks;
    }

    // =========================================================================
    // Field extraction
    // =========================================================================

    private static string? ExtractField(List<TextLine> block, FieldDefinition field)
    {
        return field.Extractor?.ToLowerInvariant() switch
        {
            "regex"      => ExtractByRegex(block, field),
            "labelvalue" => ExtractByLabel(block, field),
            "table"      => ExtractFromTable(block, field),
            _            => null
        };
    }

    // --- regex ---

    private static string? ExtractByRegex(List<TextLine> block, FieldDefinition field)
    {
        if (string.IsNullOrWhiteSpace(field.Pattern)) return null;
        var regex = new Regex(field.Pattern, RegexOptions.IgnoreCase);

        foreach (var line in block)
        {
            var match = regex.Match(line.Text);
            if (match.Success && match.Groups.Count > field.Group)
                return match.Groups[field.Group].Value;
        }

        return null;
    }

    // --- labelValue ---

    private static string? ExtractByLabel(List<TextLine> block, FieldDefinition field)
    {
        if (string.IsNullOrWhiteSpace(field.Label)) return null;

        for (int i = 0; i < block.Count; i++)
        {
            var line = block[i];
            var idx  = line.Text.IndexOf(field.Label, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;

            if (string.Equals(field.Direction, "below", StringComparison.OrdinalIgnoreCase))
            {
                // Value is on the very next non-empty line
                for (int j = i + 1; j < block.Count; j++)
                {
                    if (!string.IsNullOrWhiteSpace(block[j].Text))
                        return block[j].Text.Trim();
                }
            }
            else
            {
                // Value follows the label on the same line
                var after = line.Text[(idx + field.Label.Length)..].Trim();
                if (!string.IsNullOrEmpty(after)) return after;
            }
        }

        return null;
    }

    // --- table ---

    private static string? ExtractFromTable(List<TextLine> block, FieldDefinition field)
    {
        if (string.IsNullOrWhiteSpace(field.TableHeader) ||
            string.IsNullOrWhiteSpace(field.Column)      ||
            string.IsNullOrWhiteSpace(field.Row))
            return null;

        bool   inTable      = false;
        string[]? headers   = null;
        int    targetColIdx = -1;

        foreach (var line in block)
        {
            // Find the table header line
            if (!inTable)
            {
                if (line.Text.Contains(field.TableHeader, StringComparison.OrdinalIgnoreCase))
                    inTable = true;
                continue;
            }

            // First line after the table header is the column header row
            if (headers is null)
            {
                headers       = SplitTableRow(line.Text);
                targetColIdx  = Array.FindIndex(headers,
                    h => h.Equals(field.Column, StringComparison.OrdinalIgnoreCase));
                continue;
            }

            // Subsequent lines are data rows; match by row label
            if (line.Text.StartsWith(field.Row, StringComparison.OrdinalIgnoreCase))
            {
                var cols = SplitTableRow(line.Text);
                if (targetColIdx >= 0 && targetColIdx < cols.Length)
                    return cols[targetColIdx];
            }
        }

        return null;
    }

    /// <summary>
    /// Split a PDF text-table row on runs of two or more spaces (poor-man's column separator).
    /// </summary>
    private static string[] SplitTableRow(string text) =>
        text.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .ToArray();
}
