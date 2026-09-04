using GenericParsingEngine.Config;
using GenericParsingEngine.Parsers;
using Xunit;

namespace GenericParsingEngine.Tests;

public class XmlParserTests : IDisposable
{
    private readonly string _tempDir;

    public XmlParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "XmlTests_" + Guid.NewGuid());
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

    private const string SampleXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Employees>
          <Employee>
            <EmployeeId>EMP001</EmployeeId>
            <PersonalInfo>
              <FullName>Alice Johnson</FullName>
              <StartDate>2019-03-15</StartDate>
            </PersonalInfo>
            <Department code="ENG">Engineering</Department>
            <Hours>
              <Regular>40.5</Regular>
            </Hours>
          </Employee>
          <Employee>
            <EmployeeId>EMP002</EmployeeId>
            <PersonalInfo>
              <FullName>Bob Smith</FullName>
              <StartDate>2021-07-22</StartDate>
            </PersonalInfo>
            <Department code="HR">Human Resources</Department>
            <Hours>
              <Regular>38.0</Regular>
            </Hours>
          </Employee>
        </Employees>
        """;

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Parse_XmlElements_ReturnsCorrectRecordCount()
    {
        var filePath = WriteTempFile("employees.xml", SampleXml);
        var config   = BuildXPathConfig();

        var records = new XmlParser().Parse(filePath, config);

        Assert.Equal(2, records.Count);
    }

    [Fact]
    public void Parse_XmlElements_ExtractsEmployeeId()
    {
        var filePath = WriteTempFile("employees.xml", SampleXml);
        var config   = BuildXPathConfig();

        var records = new XmlParser().Parse(filePath, config);

        Assert.Equal("EMP001", records[0].GetString("employeeId"));
        Assert.Equal("EMP002", records[1].GetString("employeeId"));
    }

    [Fact]
    public void Parse_XmlElements_ExtractsNestedElement()
    {
        var filePath = WriteTempFile("employees.xml", SampleXml);
        var config   = BuildXPathConfig();

        var records = new XmlParser().Parse(filePath, config);

        Assert.Equal("Alice Johnson", records[0].GetString("fullName"));
        Assert.Equal("Bob Smith",     records[1].GetString("fullName"));
    }

    [Fact]
    public void Parse_XmlElements_ExtractsDepartmentText()
    {
        var filePath = WriteTempFile("employees.xml", SampleXml);
        var config   = BuildXPathConfig();

        var records = new XmlParser().Parse(filePath, config);

        Assert.Equal("Engineering",    records[0].GetString("department"));
        Assert.Equal("Human Resources", records[1].GetString("department"));
    }

    [Fact]
    public void Parse_XmlElements_ConvertsDecimalField()
    {
        var filePath = WriteTempFile("employees.xml", SampleXml);
        var config   = BuildXPathConfig();

        var records = new XmlParser().Parse(filePath, config);

        Assert.Equal(40.5m, records[0].GetDecimal("hours"));
        Assert.Equal(38.0m, records[1].GetDecimal("hours"));
    }

    [Fact]
    public void Parse_XmlAttribute_ExtractsAttributeValue()
    {
        var filePath = WriteTempFile("employees2.xml", SampleXml);
        var config   = new ParserConfig
        {
            FileType       = "xml",
            RecordBoundary = new RecordBoundary { Type = "xpath", Xpath = "//Employees/Employee" },
            Fields         = new List<FieldDefinition>
            {
                new() { Name = "deptCode", Extractor = "attribute",
                        Xpath = "Department", Attribute = "code" }
            }
        };

        var records = new XmlParser().Parse(filePath, config);

        Assert.Equal("ENG", records[0].GetString("deptCode"));
        Assert.Equal("HR",  records[1].GetString("deptCode"));
    }

    [Fact]
    public void Parse_MissingXPath_ThrowsInvalidOperation()
    {
        var filePath = WriteTempFile("employees3.xml", SampleXml);
        var config   = new ParserConfig
        {
            FileType       = "xml",
            RecordBoundary = new RecordBoundary { Type = "xpath", Xpath = null },
            Fields         = new List<FieldDefinition>()
        };

        Assert.Throws<InvalidOperationException>(() =>
            new XmlParser().Parse(filePath, config));
    }

    [Fact]
    public void Parse_XmlDateField_ConvertsToDateTime()
    {
        var filePath = WriteTempFile("employees.xml", SampleXml);
        var config   = new ParserConfig
        {
            FileType       = "xml",
            RecordBoundary = new RecordBoundary { Type = "xpath", Xpath = "//Employees/Employee" },
            Fields         = new List<FieldDefinition>
            {
                new() { Name = "startDate", Extractor = "xpath",
                        Xpath = "PersonalInfo/StartDate",
                        Type = "date", DateFormat = "yyyy-MM-dd" }
            }
        };

        var records = new XmlParser().Parse(filePath, config);

        Assert.Equal(new DateTime(2019, 3, 15), records[0].GetDate("startDate"));
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private static ParserConfig BuildXPathConfig() => new()
    {
        FileType       = "xml",
        RecordBoundary = new RecordBoundary { Type = "xpath", Xpath = "//Employees/Employee" },
        Fields         = new List<FieldDefinition>
        {
            new() { Name = "employeeId", Extractor = "xpath", Xpath = "EmployeeId" },
            new() { Name = "fullName",   Extractor = "xpath", Xpath = "PersonalInfo/FullName" },
            new() { Name = "department", Extractor = "xpath", Xpath = "Department" },
            new() { Name = "hours",      Extractor = "xpath", Xpath = "Hours/Regular", Type = "decimal" }
        }
    };
}
