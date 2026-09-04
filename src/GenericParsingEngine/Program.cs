using GenericParsingEngine.Engine;

// ---------------------------------------------------------------------------
// Generic Parsing Engine — entry point
//
// Usage:
//   dotnet run -- <filePath> <configPath>   Parse one file/config pair directly
//   dotnet run                              Interactive menu — pick a sample file type
//
// Examples:
//   dotnet run -- ./samples/sample-employees.csv  ./configs/sample-delimited.yaml
//   dotnet run -- ./samples/sample-employees.txt  ./configs/sample-fixed.yaml
//   dotnet run -- ./samples/sample-employees.xml  ./configs/sample-xml.yaml
// ---------------------------------------------------------------------------

Console.WriteLine("=== Generic Parsing Engine ===");
Console.WriteLine();

if (args.Length == 2)
{
    RunParse(args[0], args[1]);
    return;
}

Console.WriteLine("Usage: GenericParsingEngine <filePath> <configPath>");
Console.WriteLine();
RunMenu();
return;

// ---------------------------------------------------------------------------
// Interactive menu
// ---------------------------------------------------------------------------

void RunMenu()
{
    var baseDir = ResolveBaseDir();

    while (true)
    {
        Console.WriteLine("Select a sample file type to parse:");
        Console.WriteLine("  1) Delimited (CSV)");
        Console.WriteLine("  2) Fixed-width");
        Console.WriteLine("  3) XML");
        Console.WriteLine("  4) PDF");
        Console.WriteLine("  0) Exit");
        Console.Write("> ");

        var choice = Console.ReadLine()?.Trim();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                RunParse(Path.Combine(baseDir, "samples", "sample-employees.csv"),
                          Path.Combine(baseDir, "configs", "sample-delimited.yaml"));
                break;

            case "2":
                RunParse(Path.Combine(baseDir, "samples", "sample-employees.txt"),
                          Path.Combine(baseDir, "configs", "sample-fixed.yaml"));
                break;

            case "3":
                RunParse(Path.Combine(baseDir, "samples", "sample-employees.xml"),
                          Path.Combine(baseDir, "configs", "sample-xml.yaml"));
                break;

            case "4":
                RunPdfOption(baseDir);
                break;

            case "0":
            case "q":
            case "Q":
                return;

            default:
                Console.WriteLine($"Unrecognized option: '{choice}'. Please enter 0-4.");
                Console.WriteLine();
                break;
        }
    }
}

void RunPdfOption(string baseDir)
{
    // No PDF sample ships with the repo (see README) — ask for a real file.
    Console.Write("Path to a PDF file to parse: ");
    var pdfPath = Console.ReadLine()?.Trim();
    Console.WriteLine();

    if (string.IsNullOrWhiteSpace(pdfPath))
    {
        Console.WriteLine("No path entered — returning to menu.");
        Console.WriteLine();
        return;
    }

    RunParse(pdfPath, Path.Combine(baseDir, "configs", "sample-pdf.yaml"));
}

string ResolveBaseDir()
{
    // Locate the sample/config files relative to the build output directory,
    // falling back to the working directory (for `dotnet run`).
    var baseDir = AppContext.BaseDirectory;
    if (File.Exists(Path.Combine(baseDir, "samples", "sample-employees.csv")))
        return baseDir;

    return Directory.GetCurrentDirectory();
}

// ---------------------------------------------------------------------------
// Parse + display
// ---------------------------------------------------------------------------

void RunParse(string filePath, string configPath)
{
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

        if (args.Length == 2)
            Environment.Exit(1);
    }

    Console.WriteLine();
}
