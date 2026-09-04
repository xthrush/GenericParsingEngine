using System.Text;
using GenericParsingEngine.Config;
using GenericParsingEngine.Extractors;
using GenericParsingEngine.Models;

namespace GenericParsingEngine.Parsers;

/// <summary>
/// Parses delimiter-separated files (CSV, TSV, pipe-delimited, etc.).
/// Supports quoted fields with embedded delimiters and escaped double-quotes.
/// </summary>
public class DelimitedParser : IFileParser
{
    /// <inheritdoc/>
    public List<ParsedRecord> Parse(string filePath, ParserConfig config)
    {
        var records = new List<ParsedRecord>();
        var delimiter = config.Delimiter ?? ",";

        using var reader = new StreamReader(filePath);

        // Skip leading rows (e.g. file-level metadata lines before the data)
        for (int i = 0; i < config.SkipRows; i++)
            reader.ReadLine();

        // Optionally read a header row to build a column-name → index map
        string[]? headers = null;
        if (config.HasHeader)
        {
            var headerLine = reader.ReadLine();
            if (headerLine is not null)
                headers = SplitLine(headerLine, delimiter);
        }

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = SplitLine(line, delimiter);
            var record = new ParsedRecord();

            foreach (var field in config.Fields)
            {
                string? rawValue = null;

                if (!string.IsNullOrEmpty(field.SourceColumn) && headers is not null)
                {
                    // Map by column header name (case-insensitive)
                    var colIdx = Array.FindIndex(headers,
                        h => h.Trim().Equals(field.SourceColumn, StringComparison.OrdinalIgnoreCase));
                    if (colIdx >= 0 && colIdx < values.Length)
                        rawValue = values[colIdx];
                }
                else if (field.ColumnIndex >= 0 && field.ColumnIndex < values.Length)
                {
                    // Map by zero-based column index
                    rawValue = values[field.ColumnIndex];
                }

                record[field.Name] = FieldValueConverter.Convert(rawValue, field);
            }

            records.Add(record);
        }

        return records;
    }

    // -------------------------------------------------------------------------
    // RFC-4180-compliant line splitter
    // -------------------------------------------------------------------------

    private static string[] SplitLine(string line, string delimiter)
    {
        // Fast path: no quotes in line
        if (!line.Contains('"'))
            return line.Split(delimiter);

        var result   = new List<string>();
        var current  = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped double-quote ("") inside a quoted field
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (!inQuotes && line[i..].StartsWith(delimiter, StringComparison.Ordinal))
            {
                result.Add(current.ToString());
                current.Clear();
                i += delimiter.Length - 1;
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
