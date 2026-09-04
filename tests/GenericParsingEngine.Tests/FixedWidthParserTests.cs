using GenericParsingEngine.Config;
using GenericParsingEngine.Parsers;
using Xunit;

namespace GenericParsingEngine.Tests;

public class FixedWidthParserTests : IDisposable
{
    private readonly string _tempDir;

    public FixedWidthParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FixedTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string WriteTempFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Parse_FixedWidth_ReturnsCorrectRecordCount()
    {
        // ID(0-7)  Name(8-37)                    Hours(38-45)
        var filePath = WriteTempFile("fixed.txt",
            "HEADER  Header Line Goes Here              \n" +
            "EMP001  Alice Johnson                 40.50  \n" +
            "EMP002  Bob Smith                     38.00  \n");

        var config = BuildConfig(skipRows: 1,
            ("employeeId", 0,  8,  "string"),
            ("name",       8,  30, "string"),
            ("hours",      38, 8,  "decimal"));

        var records = new FixedWidthParser().Parse(filePath, config);

        Assert.Equal(2, records.Count);
    }

    [Fact]
    public void Parse_FixedWidth_ExtractsStringField()
    {
        var filePath = WriteTempFile("fixed.txt",
            "HEADER  Header Line Goes Here              \n" +
            "EMP001  Alice Johnson                 40.50  \n" +
            "EMP002  Bob Smith                     38.00  \n");

        var config = BuildConfig(skipRows: 1,
            ("employeeId", 0,  8,  "string"),
            ("name",       8,  30, "string"),
            ("hours",      38, 8,  "decimal"));

        var records = new FixedWidthParser().Parse(filePath, config);

        Assert.Equal("EMP001", records[0].GetString("employeeId"));
        Assert.Equal("Alice Johnson", records[0].GetString("name"));
        Assert.Equal("EMP002", records[1].GetString("employeeId"));
        Assert.Equal("Bob Smith", records[1].GetString("name"));
    }

    [Fact]
    public void Parse_FixedWidth_ConvertsDecimalField()
    {
        var filePath = WriteTempFile("fixed.txt",
            "HEADER  Header Line Goes Here              \n" +
            "EMP001  Alice Johnson                 40.50  \n");

        var config = BuildConfig(skipRows: 1,
            ("employeeId", 0,  8,  "string"),
            ("name",       8,  30, "string"),
            ("hours",      38, 8,  "decimal"));

        var records = new FixedWidthParser().Parse(filePath, config);

        Assert.Equal(40.50m, records[0].GetDecimal("hours"));
    }

    [Fact]
    public void Parse_SkipRows_IgnoresHeaderLines()
    {
        var filePath = WriteTempFile("fixed2.txt",
            "SkipThisHeader\n" +
            "EMP003  Charlie   \n");

        var config = BuildConfig(skipRows: 1,
            ("id",   0, 8,  "string"),
            ("name", 8, 10, "string"));

        var records = new FixedWidthParser().Parse(filePath, config);

        Assert.Single(records);
        Assert.Equal("EMP003", records[0].GetString("id"));
        Assert.Equal("Charlie", records[0].GetString("name"));
    }

    [Fact]
    public void Parse_LineShorterThanField_DoesNotThrow()
    {
        // Line is only 5 chars; field at pos 10 should yield null/default
        var filePath = WriteTempFile("short.txt", "ABCDE\n");

        var config = BuildConfig(skipRows: 0,
            ("field1", 0,  5,  "string"),
            ("field2", 10, 10, "string"));   // beyond line length

        var records = new FixedWidthParser().Parse(filePath, config);

        Assert.Single(records);
        Assert.Equal("ABCDE", records[0].GetString("field1"));
        Assert.Equal(string.Empty, records[0].GetString("field2"));
    }

    [Fact]
    public void Parse_BlankLines_AreSkipped()
    {
        var filePath = WriteTempFile("blanks.txt",
            "EMP001  Alice     \n" +
            "         \n" +
            "EMP002  Bob       \n");

        var config = BuildConfig(skipRows: 0,
            ("id",   0, 8,  "string"),
            ("name", 8, 10, "string"));

        var records = new FixedWidthParser().Parse(filePath, config);

        Assert.Equal(2, records.Count);
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private static ParserConfig BuildConfig(
        int skipRows,
        params (string name, int start, int length, string type)[] fields)
    {
        return new ParserConfig
        {
            FileType       = "fixed",
            SkipRows       = skipRows,
            RecordBoundary = new RecordBoundary { Type = "row" },
            Fields         = fields.Select(f => new FieldDefinition
            {
                Name     = f.name,
                StartPos = f.start,
                Length   = f.length,
                Type     = f.type,
                Trim     = true
            }).ToList()
        };
    }
}
