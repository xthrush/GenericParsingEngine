using GenericParsingEngine.Engine;

// ---------------------------------------------------------------------------
// Generic Parsing Engine — entry point
//
// Usage:
//   dotnet run -- <filePath> <configPath>
//
// Examples:
//   dotnet run -- ./samples/sample-employees.csv  ./configs/sample-delimited.yaml
//   dotnet run -- ./samples/sample-employees.txt  ./configs/sample-fixed.yaml
//   dotnet run -- ./samples/sample-employees.xml  ./configs/sample-xml.yaml
// ---------------------------------------------------------------------------

Console.WriteLine("=== Generic Parsing Engine ===");
Console.WriteLine();

string filePath;
string configPath;

if (args.Length == 2)
{
    filePath   = args[0];
    configPath = args[1];
}
else
{
    Console.WriteLine("Usage: GenericParsingEngine <filePath> <configPath>");
    Console.WriteLine();
    Console.WriteLine("No arguments supplied — running built-in demo with sample CSV...");
    Console.WriteLine();

    // Locate the sample files relative to the build output directory
    var baseDir   = AppContext.BaseDirectory;
    filePath      = Path.Combine(baseDir, "samples", "sample-employees.csv");
    configPath    = Path.Combine(baseDir, "configs", "sample-delimited.yaml");

    // Fallback: try paths relative to the working directory (for dotnet run)
    if (!File.Exists(filePath))
    {
        filePath   = Path.Combine("samples", "sample-employees.csv");
        configPath = Path.Combine("configs", "sample-delimited.yaml");
    }
}

try
{
    var engine  = new ParserEngine();
    var records = engine.Process(filePath, configPath);

    Console.WriteLine($"Parsed {records.Count} record(s) from: {Path.GetFileName(filePath)}");
    Console.WriteLine(new string('-', 60));

    foreach (var record in records)
    {
        foreach (var kvp in record)
            Console.WriteLine($"  {kvp.Key,-22} : {kvp.Value}");

        Console.WriteLine(new string('-', 60));
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.ResetColor();
    Environment.Exit(1);
}
