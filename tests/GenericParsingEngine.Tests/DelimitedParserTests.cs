using GenericParsingEngine.Config;
using GenericParsingEngine.Parsers;
using Xunit;

namespace GenericParsingEngine.Tests;

public class DelimitedParserTests : IDisposable
{
    private readonly string _tempDir;

    public DelimitedParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ParserTests_" + Guid.NewGuid());
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
    // Happy-path tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Parse_CsvWithHeader_ReturnsCorrectRecordCount()
    {
        var csvPath = WriteTempFile("test.csv",
            "EmployeeId,Name,Department,Hours\n" +
            "EMP001,Alice Johnson,Engineering,40.5\n" +
            "EMP002,Bob Smith,HR,38.0\n");

        var config = BuildConfig(",", hasHeader: true,
            ("employeeId", "EmployeeId", -1, "string"),
            ("name",       "Name",       -1, "string"),
            ("department", "Department", -1, "string"),
            ("hours",      "Hours",      -1, "decimal"));

        var records = new DelimitedParser().Parse(csvPath, config);

        Assert.Equal(2, records.Count);
    }

    [Fact]
    public void Parse_CsvWithHeader_ExtractsStringFields()
    {
        var csvPath = WriteTempFile("test.csv",
            "EmployeeId,Name,Department,Hours\n" +
            "EMP001,Alice Johnson,Engineering,40.5\n" +
            "EMP002,Bob Smith,HR,38.0\n");

        var config = BuildConfig(",", hasHeader: true,
            ("employeeId", "EmployeeId", -1, "string"),
            ("name",       "Name",       -1, "string"),
            ("department", "Department", -1, "string"),
            ("hours",      "Hours",      -1, "decimal"));

        var records = new DelimitedParser().Parse(csvPath, config);

        Assert.Equal("EMP001", records[0].GetString("employeeId"));
        Assert.Equal("Alice Johnson", records[0].GetString("name"));
        Assert.Equal("Engineering", records[0].GetString("department"));
        Assert.Equal("EMP002", records[1].GetString("employeeId"));
        Assert.Equal("Bob Smith", records[1].GetString("name"));
    }

    [Fact]
    public void Parse_CsvWithHeader_ConvertsDecimalField()
    {
        var csvPath = WriteTempFile("test.csv",
            "EmployeeId,Name,Department,Hours\n" +
            "EMP001,Alice Johnson,Engineering,40.5\n");

        var config = BuildConfig(",", hasHeader: true,
            ("hours", "Hours", -1, "decimal"));

        var records = new DelimitedParser().Parse(csvPath, config);

        Assert.Equal(40.5m, records[0].GetDecimal("hours"));
    }

    [Fact]
    public void Parse_CsvByColumnIndex_ReturnsCorrectValues()
    {
        var csvPath = WriteTempFile("test-noheader.csv",
            "EMP003,Charlie Brown,Sales,37.0\n");

        var config = BuildConfig(",", hasHeader: false,
            ("employeeId", null, 0, "string"),
            ("name",       null, 1, "string"));

        var records = new DelimitedParser().Parse(csvPath, config);

        Assert.Single(records);
        Assert.Equal("EMP003", records[0].GetString("employeeId"));
        Assert.Equal("Charlie Brown", records[0].GetString("name"));
    }

    [Fact]
    public void Parse_TsvDelimiter_ParsesCorrectly()
    {
        var tsvPath = WriteTempFile("test.tsv", "ID\tName\nE01\tDave\n");

        var config = BuildConfig("\t", hasHeader: true,
            ("id",   "ID",   -1, "string"),
            ("name", "Name", -1, "string"));

        var records = new DelimitedParser().Parse(tsvPath, config);

        Assert.Single(records);
        Assert.Equal("E01", records[0].GetString("id"));
        Assert.Equal("Dave", records[0].GetString("name"));
    }

    [Fact]
    public void Parse_QuotedFieldWithComma_HandlesEmbeddedDelimiter()
    {
        var csvPath = WriteTempFile("quoted.csv",
            "Id,Name,Title\n" +
            "1,\"Smith, John\",Engineer\n");

        var config = BuildConfig(",", hasHeader: true,
            ("id",    "Id",    -1, "string"),
            ("name",  "Name",  -1, "string"),
            ("title", "Title", -1, "string"));

        var records = new DelimitedParser().Parse(csvPath, config);

        Assert.Single(records);
        Assert.Equal("Smith, John", records[0].GetString("name"));
        Assert.Equal("Engineer", records[0].GetString("title"));
    }

    [Fact]
    public void Parse_QuotedFieldWithEscapedQuote_HandlesDoubleQuote()
    {
        var csvPath = WriteTempFile("escaped.csv",
            "Id,Description\n" +
            "1,\"Say \"\"hello\"\"\"\n");

        var config = BuildConfig(",", hasHeader: true,
            ("id",   "Id",          -1, "string"),
            ("desc", "Description", -1, "string"));

        var records = new DelimitedParser().Parse(csvPath, config);

        Assert.Single(records);
        Assert.Equal("Say \"hello\"", records[0].GetString("desc"));
    }

    [Fact]
    public void Parse_SkipRows_IgnoresLeadingRows()
    {
        var csvPath = WriteTempFile("skiprows.csv",
            "# This is a comment\n" +
            "Id,Name\n" +
            "42,Eve\n");

        var config = new ParserConfig
        {
            FileType        = "delimited",
            Delimiter       = ",",
            HasHeader       = true,
            SkipRows        = 1,
            RecordBoundary  = new RecordBoundary { Type = "row" },
            Fields          = new List<FieldDefinition>
            {
                new() { Name = "id",   SourceColumn = "Id" },
                new() { Name = "name", SourceColumn = "Name" }
            }
        };

        var records = new DelimitedParser().Parse(csvPath, config);

        Assert.Single(records);
        Assert.Equal("42", records[0].GetString("id"));
        Assert.Equal("Eve", records[0].GetString("name"));
    }

    [Fact]
    public void Parse_BlankLines_AreSkipped()
    {
        var csvPath = WriteTempFile("blanks.csv",
            "Id,Name\n" +
            "1,Alice\n" +
            "\n" +
            "2,Bob\n");

        var config = BuildConfig(",", hasHeader: true,
            ("id",   "Id",   -1, "string"),
            ("name", "Name", -1, "string"));

        var records = new DelimitedParser().Parse(csvPath, config);

        Assert.Equal(2, records.Count);
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private static ParserConfig BuildConfig(
        string delimiter,
        bool hasHeader,
        params (string name, string? sourceColumn, int colIndex, string type)[] fields)
    {
        return new ParserConfig
        {
            FileType       = "delimited",
            Delimiter      = delimiter,
            HasHeader      = hasHeader,
            RecordBoundary = new RecordBoundary { Type = "row" },
            Fields         = fields.Select(f => new FieldDefinition
            {
                Name         = f.name,
                Extractor    = "column",
                SourceColumn = f.sourceColumn,
                ColumnIndex  = f.colIndex,
                Type         = f.type
            }).ToList()
        };
    }
}
