# Generic Parsing Engine

A config-driven file parsing engine for .NET 8 that can parse **PDF, CSV/TSV (delimited), fixed-width, and XML** files using external YAML or JSON layout files — no code changes per client layout.

---

## Project Overview

The engine reads a layout configuration file (YAML or JSON) that describes:
- What file type to expect (`pdf`, `delimited`, `fixed`, `xml`)
- How records are bounded within the file (by row, regex pattern, blank lines, or XPath)
- Which fields to extract, where to find them, and what CLR type to convert them to

The output is a strongly-typed `List<ParsedRecord>` (a `Dictionary<string, object?>` with helper accessors) that downstream code can consume immediately.

---

## Supported File Types

| File Type | Config `fileType` | Notes |
|-----------|-------------------|-------|
| PDF | `pdf` | Text extracted via PdfPig; supports regex, label-value, and table extraction |
| Delimited | `delimited` | CSV, TSV, pipe-separated; RFC-4180 quoted fields; header or index mapping |
| Fixed-width | `fixed` | Each field defined by `startPos` + `length`; optional `skipRows` for headers |
| XML | `xml` | XPath record selection; element text and attribute extraction |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

Verify your installation:

```bash
dotnet --version
# Should print 8.x.x
```

---

## Getting Started

```bash
# Clone / copy the project
cd /tmp/GenericParsingEngine

# Restore NuGet packages
dotnet restore

# Build
dotnet build

# Run with a sample file
dotnet run --project src/GenericParsingEngine -- \
  samples/sample-employees.csv \
  configs/sample-delimited.yaml
```

### Running all four sample file types

```bash
# CSV
dotnet run --project src/GenericParsingEngine -- \
  samples/sample-employees.csv configs/sample-delimited.yaml

# Fixed-width
dotnet run --project src/GenericParsingEngine -- \
  samples/sample-employees.txt configs/sample-fixed.yaml

# XML
dotnet run --project src/GenericParsingEngine -- \
  samples/sample-employees.xml configs/sample-xml.yaml

# PDF (requires an actual PDF file)
dotnet run --project src/GenericParsingEngine -- \
  path/to/report.pdf configs/sample-pdf.yaml
```

### Running without arguments (demo mode)

```bash
dotnet run --project src/GenericParsingEngine
```

Runs the CSV sample automatically.

---

## Running Tests

```bash
dotnet test
```

Tests cover DelimitedParser, FixedWidthParser, and XmlParser with happy-path and edge-case scenarios. All tests use temporary files and clean up after themselves.

---

## Config File Reference

Config files can be YAML (`.yaml` / `.yml`) or JSON (`.json`). All field names use camelCase.

### Top-level fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `fileType` | string | Yes | `pdf` \| `delimited` \| `fixed` \| `xml` |
| `description` | string | No | Human-readable label for this layout |
| `delimiter` | string | Delimited only | Column separator character(s). Default: `,` |
| `hasHeader` | bool | Delimited only | Whether the first data row is a header. Default: `true` |
| `skipRows` | int | No | Number of leading rows to skip. Default: `0` |
| `recordBoundary` | object | Yes | See below |
| `fields` | array | Yes | List of field definitions |

### recordBoundary

| Field | Type | Description |
|-------|------|-------------|
| `type` | string | `row` \| `pattern` \| `blanklines` \| `pageBreak` \| `xpath` |
| `pattern` | string | Regex pattern for `pattern` type |
| `xpath` | string | XPath expression for `xpath` type (XML) |
| `blankLineCount` | int | Consecutive blank lines for `blanklines` type. Default: `2` |

### Field definition — common fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `name` | string | — | Output key name in the ParsedRecord |
| `extractor` | string | — | `column` \| `regex` \| `labelValue` \| `table` \| `xpath` \| `attribute` |
| `type` | string | `string` | `string` \| `int` \| `decimal` \| `bool` \| `date` |
| `dateFormat` | string | — | Format string for `date` type (e.g. `MM/dd/yyyy`) |
| `trim` | bool | `true` | Trim surrounding whitespace from raw value |
| `defaultValue` | string | — | Used when raw value is null or empty |

### Extractor-specific fields

#### `column` (delimited files)

| Field | Description |
|-------|-------------|
| `sourceColumn` | Header name to match (case-insensitive) |
| `columnIndex` | Zero-based column index when `hasHeader: false` |

#### `regex` (PDF / text files)

| Field | Description |
|-------|-------------|
| `pattern` | Regular expression |
| `group` | Capture group index to use (default: `1`) |

#### `labelValue` (PDF)

| Field | Description |
|-------|-------------|
| `label` | Label text to search for |
| `direction` | `right` (same line) or `below` (next line) |

#### `table` (PDF)

| Field | Description |
|-------|-------------|
| `tableHeader` | Text identifying the table block |
| `column` | Column header within the table |
| `row` | Row label within the table |

#### `xpath` / `attribute` (XML)

| Field | Description |
|-------|-------------|
| `xpath` | XPath relative to the record node |
| `attribute` | Attribute name (for `attribute` extractor only) |

#### Fixed-width fields

| Field | Description |
|-------|-------------|
| `startPos` | Zero-based character position where the field starts |
| `length` | Number of characters in the field |

---

## Sample Configs

### Delimited (CSV)

```yaml
fileType: delimited
delimiter: ","
hasHeader: true
skipRows: 0
recordBoundary:
  type: row
fields:
  - name: employeeId
    extractor: column
    sourceColumn: "Employee ID"
  - name: hoursWorked
    extractor: column
    sourceColumn: "Hours Worked"
    type: decimal
  - name: startDate
    extractor: column
    sourceColumn: "Start Date"
    type: date
    dateFormat: "MM/dd/yyyy"
```

### Fixed-width

```yaml
fileType: fixed
skipRows: 1
recordBoundary:
  type: row
fields:
  - name: employeeId
    startPos: 0
    length: 8
    trim: true
  - name: hoursWorked
    startPos: 58
    length: 8
    type: decimal
    trim: true
```

### XML

```yaml
fileType: xml
recordBoundary:
  type: xpath
  xpath: "//Employees/Employee"
fields:
  - name: employeeId
    extractor: xpath
    xpath: "EmployeeId"
  - name: departmentCode
    extractor: attribute
    xpath: "Department"
    attribute: "code"
  - name: hoursWorked
    extractor: xpath
    xpath: "Hours/Regular"
    type: decimal
```

### PDF

```yaml
fileType: pdf
recordBoundary:
  type: pattern
  pattern: "EMP-\\d{5}"
fields:
  - name: employeeId
    extractor: regex
    pattern: "(EMP-\\d{5})"
    group: 1
  - name: fullName
    extractor: labelValue
    label: "Name:"
    direction: right
  - name: regularHours
    extractor: table
    tableHeader: "Weekly Hours"
    column: "Hours"
    row: "Regular"
    type: decimal
```

---

## Extending the Engine

### Adding a new file type

1. Create a new class in `src/GenericParsingEngine/Parsers/` that implements `IFileParser`:

```csharp
public class ExcelParser : IFileParser
{
    public List<ParsedRecord> Parse(string filePath, ParserConfig config)
    {
        // ... your implementation
    }
}
```

2. Register it in `Engine/ParserEngine.cs`:

```csharp
private static IFileParser ResolveParser(string fileType)
{
    return fileType.ToLowerInvariant() switch
    {
        "pdf"       => new PdfParser(),
        "delimited" => new DelimitedParser(),
        "fixed"     => new FixedWidthParser(),
        "xml"       => new XmlParser(),
        "excel"     => new ExcelParser(),   // <-- add here
        _ => throw new NotSupportedException(...)
    };
}
```

3. Add a NuGet dependency if needed (e.g. `ClosedXML` for Excel).
4. Add a corresponding sample YAML config and a test class.

### Adding a new field extractor

Add a new `case` to the `ExtractField` switch in your parser and a matching extractor property on `FieldDefinition`.

---

## Project Structure

```
GenericParsingEngine/
├── .gitignore
├── README.md
├── GenericParsingEngine.sln
├── src/
│   └── GenericParsingEngine/
│       ├── GenericParsingEngine.csproj
│       ├── Program.cs                      # Entry point / CLI
│       ├── Config/
│       │   ├── ParserConfig.cs             # Top-level config model
│       │   ├── FieldDefinition.cs          # Per-field extraction spec
│       │   ├── RecordBoundary.cs           # Record boundary config
│       │   └── ConfigLoader.cs             # YAML/JSON deserializer
│       ├── Models/
│       │   ├── ParsedRecord.cs             # Output record (Dictionary + typed accessors)
│       │   └── TextLine.cs                 # PDF text line with positional metadata
│       ├── Parsers/
│       │   ├── IFileParser.cs              # Parser contract
│       │   ├── PdfParser.cs                # PDF (PdfPig)
│       │   ├── DelimitedParser.cs          # CSV / TSV
│       │   ├── FixedWidthParser.cs         # Fixed-width text
│       │   └── XmlParser.cs               # XML (XDocument + XPath)
│       ├── Extractors/
│       │   └── FieldValueConverter.cs      # Type conversion (string → int/decimal/bool/date)
│       └── Engine/
│           └── ParserEngine.cs             # Orchestrator; resolves parser by fileType
├── tests/
│   └── GenericParsingEngine.Tests/
│       ├── GenericParsingEngine.Tests.csproj
│       ├── DelimitedParserTests.cs
│       ├── FixedWidthParserTests.cs
│       └── XmlParserTests.cs
├── configs/
│   ├── sample-delimited.yaml
│   ├── sample-fixed.yaml
│   ├── sample-xml.yaml
│   └── sample-pdf.yaml
└── samples/
    ├── sample-employees.csv
    ├── sample-employees.txt
    └── sample-employees.xml
```

---

## NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `UglyToad.PdfPig` | 0.1.9 | PDF text and layout extraction |
| `YamlDotNet` | 15.3.0 | YAML config deserialization |
| `xunit` | 2.9.0 | Unit testing framework |
| `Microsoft.NET.Test.Sdk` | 17.10.0 | Test runner integration |
