using System.Xml.Linq;
using System.Xml.XPath;
using GenericParsingEngine.Config;
using GenericParsingEngine.Extractors;
using GenericParsingEngine.Models;

namespace GenericParsingEngine.Parsers;

/// <summary>
/// Parses XML files using XPath expressions to select record nodes and
/// extract individual field values.
/// </summary>
public class XmlParser : IFileParser
{
    /// <inheritdoc/>
    public List<ParsedRecord> Parse(string filePath, ParserConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.RecordBoundary?.Xpath))
            throw new InvalidOperationException(
                "XML parser requires 'recordBoundary.xpath' to be set in the config file.");

        var records     = new List<ParsedRecord>();
        var doc         = XDocument.Load(filePath);
        var recordNodes = doc.XPathSelectElements(config.RecordBoundary.Xpath);

        foreach (var node in recordNodes)
        {
            var record = new ParsedRecord();

            foreach (var field in config.Fields)
            {
                var rawValue = ExtractField(node, field);
                record[field.Name] = FieldValueConverter.Convert(rawValue, field);
            }

            records.Add(record);
        }

        return records;
    }

    // -------------------------------------------------------------------------
    // Field extraction helpers
    // -------------------------------------------------------------------------

    private static string? ExtractField(XElement node, FieldDefinition field)
    {
        return field.Extractor?.ToLowerInvariant() switch
        {
            "attribute" => ExtractAttribute(node, field),
            _           => ExtractByXPath(node, field)   // "xpath" or default
        };
    }

    /// <summary>Extract the inner text of an element selected by XPath.</summary>
    private static string? ExtractByXPath(XElement node, FieldDefinition field)
    {
        if (string.IsNullOrWhiteSpace(field.Xpath)) return null;
        return node.XPathSelectElement(field.Xpath)?.Value;
    }

    /// <summary>
    /// Select an element by XPath and return the value of a named attribute.
    /// If <see cref="FieldDefinition.Attribute"/> is empty, falls back to inner text.
    /// </summary>
    private static string? ExtractAttribute(XElement node, FieldDefinition field)
    {
        if (string.IsNullOrWhiteSpace(field.Xpath)) return null;
        var element = node.XPathSelectElement(field.Xpath);

        if (element is null) return null;

        return string.IsNullOrWhiteSpace(field.Attribute)
            ? element.Value
            : element.Attribute(field.Attribute)?.Value;
    }
}
