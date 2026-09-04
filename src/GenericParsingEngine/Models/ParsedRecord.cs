namespace GenericParsingEngine.Models;

/// <summary>
/// Represents a single parsed record as a dynamic key-value store.
/// Keys are the field names defined in the config; values are typed CLR objects.
/// </summary>
public class ParsedRecord : Dictionary<string, object?>
{
    /// <summary>
    /// Attempt to retrieve a field value cast to type <typeparamref name="T"/>.
    /// Returns <c>default(T)</c> if the key is missing, null, or unconvertible.
    /// </summary>
    public T? Get<T>(string fieldName)
    {
        if (!TryGetValue(fieldName, out var value) || value is null)
            return default;

        if (value is T typed)
            return typed;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>Returns the field value as a string, or empty string if absent/null.</summary>
    public string GetString(string fieldName) => Get<string>(fieldName) ?? string.Empty;

    /// <summary>Returns the field value as a decimal (0 if absent or unconvertible).</summary>
    public decimal GetDecimal(string fieldName) => Get<decimal>(fieldName);

    /// <summary>Returns the field value as an int (0 if absent or unconvertible).</summary>
    public int GetInt(string fieldName) => Get<int>(fieldName);

    /// <summary>Returns the field value as a bool (false if absent or unconvertible).</summary>
    public bool GetBool(string fieldName) => Get<bool>(fieldName);

    /// <summary>Returns the field value as a DateTime (default if absent or unconvertible).</summary>
    public DateTime GetDate(string fieldName) => Get<DateTime>(fieldName);
}
