using GenericParsingEngine.Config;
using GenericParsingEngine.Extractors;
using GenericParsingEngine.Models;

namespace GenericParsingEngine.Parsers;

/// <summary>
/// Parses fixed-width text files where each field occupies a predetermined
/// character range defined by <see cref="FieldDefinition.StartPos"/> and
/// <see cref="FieldDefinition.Length"/>.
/// </summary>
public class FixedWidthParser : IFileParser
{
    /// <inheritdoc/>
    public List<ParsedRecord> Parse(string filePath, ParserConfig config)
    {
        var records = new List<ParsedRecord>();
        var lines   = File.ReadAllLines(filePath);

        // Start reading after any header/metadata rows
        for (int lineIdx = config.SkipRows; lineIdx < lines.Length; lineIdx++)
        {
            var line = lines[lineIdx];

            // Skip completely blank lines
            if (string.IsNullOrWhiteSpace(line)) continue;

            var record = new ParsedRecord();

            foreach (var field in config.Fields)
            {
                string? rawValue = null;

                if (field.StartPos < line.Length)
                {
                    // Guard against lines shorter than the field definition
                    var endPos = Math.Min(field.StartPos + field.Length, line.Length);
                    rawValue   = line.Substring(field.StartPos, endPos - field.StartPos);
                }

                record[field.Name] = FieldValueConverter.Convert(rawValue, field);
            }

            records.Add(record);
        }

        return records;
    }
}
