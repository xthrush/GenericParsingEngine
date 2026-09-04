using System.Globalization;
using GenericParsingEngine.Config;

namespace GenericParsingEngine.Extractors;

/// <summary>
/// Converts a raw string value to the CLR type declared in a <see cref="FieldDefinition"/>.
/// </summary>
public static class FieldValueConverter
{
    /// <summary>
    /// Apply trimming, empty-value handling, and type conversion to a raw string.
    /// </summary>
    /// <param name="rawValue">The unprocessed string extracted from the source file.</param>
    /// <param name="field">The field definition that controls trimming and target type.</param>
    /// <returns>
    /// A typed CLR value (string, int, decimal, bool, DateTime) or the field's DefaultValue
    /// when <paramref name="rawValue"/> is null/empty. Returns the raw string if conversion fails.
    /// </returns>
    public static object? Convert(string? rawValue, FieldDefinition field)
    {
        if (field.Trim && rawValue is not null)
            rawValue = rawValue.Trim();

        if (string.IsNullOrEmpty(rawValue))
            return field.DefaultValue;

        return field.Type?.ToLowerInvariant() switch
        {
            "int" or "integer"               => ParseInt(rawValue),
            "decimal" or "double" or "float" => ParseDecimal(rawValue),
            "bool" or "boolean"              => ParseBool(rawValue),
            "date" or "datetime"             => ParseDate(rawValue, field.DateFormat),
            _                                => rawValue   // "string" or unrecognized
        };
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static object ParseInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
            ? i
            : (object)value;

    private static object ParseDecimal(string value) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d
            : (object)value;

    private static object ParseBool(string value) =>
        bool.TryParse(value, out var b) ? b : (object)value;

    private static object ParseDate(string value, string? format)
    {
        if (!string.IsNullOrWhiteSpace(format))
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                return dt;
        }

        // Fallback: let the runtime try to figure it out
        return DateTime.TryParse(value, CultureInfo.InvariantCulture,
                                  DateTimeStyles.None, out var dt2)
            ? dt2
            : (object)value; // return raw if still unparseable
    }
}
