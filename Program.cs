using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static string[] _outputExtensions = new[] { ".cs", ".cshtml", ".css", ".html", ".js", ".razor" };

    static void Main(string[] args)
    {
        string solutionPath;
        if (args.Length > 0)
        {
            solutionPath = args[0];
        }
        else
        {
            // Hard-coded solution path for debugging
            solutionPath = @"C:\dev\SlnToText\SlnToText.sln";
        }

        // Generate the output
        string output = GenerateOutput(solutionPath);

        // Write the output to a file
        string outputFileName = $"SlnToText_{Path.GetFileNameWithoutExtension(solutionPath)}_{DateTime.Now:yyyyMMddHHmmss}.txt";
        File.WriteAllText(outputFileName, output);

        Console.WriteLine($"Output written to {outputFileName}");
    }

    static string GenerateOutput(string solutionPath)
    {
        StringBuilder output = new StringBuilder();

        // Generate the solution explorer structure
        output.AppendLine("<solution_explorer>");
        GenerateSolutionStructure(solutionPath, output, 0);
        output.AppendLine("</solution_explorer>");

        // Generate the file contents
        string solutionDirectory = Path.GetDirectoryName(solutionPath);
        GenerateFileContents(solutionDirectory, solutionDirectory, output);

        return output.ToString();
    }

    static void GenerateSolutionStructure(string solutionPath, StringBuilder output, int indentLevel)
    {
        string solutionContent = File.ReadAllText(solutionPath);

        var projectRegex = new Regex(@"Project\(""{(.*?)}""\) = ""(.*?)"", ""(.*?)"", ""{(.*?)}""");
        var projectMatches = projectRegex.Matches(solutionContent);

        foreach (Match projectMatch in projectMatches)
        {
            string projectName = projectMatch.Groups[2].Value;
            string projectPath = projectMatch.Groups[3].Value;

            string indent = new string(' ', indentLevel * 2);
            output.AppendLine($"{indent}- {projectName}");

            string projectFilePath = Path.Combine(Path.GetDirectoryName(solutionPath), projectPath);
            GenerateProjectStructure(projectFilePath, output, indentLevel + 1);
        }
    }

    static void GenerateProjectStructure(string projectFilePath, StringBuilder output, int indentLevel)
    {
        string projectContent = File.ReadAllText(projectFilePath);

        var referenceRegex = new Regex(@"<ProjectReference\s+Include=""(.*?)""\s*/>");
        var referenceMatches = referenceRegex.Matches(projectContent);

        var packageRegex = new Regex(@"<PackageReference\s+Include=""(.*?)""\s+Version=""(.*?)""\s*/>");
        var packageMatches = packageRegex.Matches(projectContent);

        string indent = new string(' ', indentLevel * 2);
        output.AppendLine($"{indent}- Dependencies");

        foreach (Match referenceMatch in referenceMatches)
        {
            string referencePath = referenceMatch.Groups[1].Value;
            string referenceProjectName = Path.GetFileNameWithoutExtension(referencePath);
            output.AppendLine($"{indent}  - {referenceProjectName}");
        }

        foreach (Match packageMatch in packageMatches)
        {
            string packageName = packageMatch.Groups[1].Value;
            string packageVersion = packageMatch.Groups[2].Value;
            output.AppendLine($"{indent}  - {packageName} ({packageVersion})");
        }

        string projectDirectory = Path.GetDirectoryName(projectFilePath);
        GenerateProjectFolderStructure(projectDirectory, output, indentLevel);
    }

    static void GenerateProjectFolderStructure(string projectDirectory, StringBuilder output, int indentLevel)
    {
        var files = Directory.GetFiles(projectDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => _outputExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string indent = new string(' ', indentLevel * 2);
            output.AppendLine($"{indent}- {fileName}");
        }

        var directories = Directory.GetDirectories(projectDirectory)
            .Where(d => !Path.GetFileName(d).Equals("bin", StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(d).Equals("obj", StringComparison.OrdinalIgnoreCase));

        foreach (string directory in directories)
        {
            string directoryName = Path.GetFileName(directory);
            string indent = new string(' ', indentLevel * 2);
            output.AppendLine($"{indent}- {directoryName}");
            GenerateProjectFolderStructure(directory, output, indentLevel + 1);
        }
    }

    static void GenerateFileContents(string directory, string solutionDirectory, StringBuilder output)
    {
        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(f => _outputExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"));

        foreach (string file in files)
        {
            string fileContent = File.ReadAllText(file);
            string relativePath = Path.GetRelativePath(solutionDirectory, file);
            output.AppendLine($"<{relativePath}>");
            output.AppendLine(fileContent);
            output.AppendLine($"</{relativePath}>");
        }
    }
}