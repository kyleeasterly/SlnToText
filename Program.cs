using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static string[] _outputExtensions = { ".cs", ".cshtml", ".css", ".html", ".js", ".razor" };

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
        string outputFileName = $"SlnToText_{Path.GetFileNameWithoutExtension(solutionPath)}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        File.WriteAllText(outputFileName, output);

        Console.WriteLine($"Output written to {outputFileName}");
    }

    static string GenerateOutput(string solutionPath)
    {
        StringBuilder output = new StringBuilder();
        List<string> filePaths = new List<string>();

        // Generate the solution explorer structure
        output.AppendLine("<solution_explorer>");
        GenerateSolutionStructure(solutionPath, output, 0, filePaths);
        output.AppendLine("</solution_explorer>");

        // Generate the file contents
        string solutionDirectory = Path.GetDirectoryName(solutionPath);
        GenerateFileContents(solutionDirectory, filePaths, output);

        return output.ToString();
    }

    static void GenerateSolutionStructure(string solutionPath, StringBuilder output, int indentLevel, List<string> filePaths)
    {
        string solutionContent = File.ReadAllText(solutionPath);
        var projectRegex = new Regex(@"Project\(""{(.*?)}""\) = ""(.*?)"", ""(.*?)"", ""{(.*?)}""");
        var projectMatches = projectRegex.Matches(solutionContent);

        // Create a list to store project information
        var projects = new List<(string Name, string Path)>();

        foreach (Match projectMatch in projectMatches)
        {
            string projectName = projectMatch.Groups[2].Value;
            string projectPath = projectMatch.Groups[3].Value;
            projects.Add((projectName, projectPath));
        }

        // Sort the projects alphabetically by name
        projects.Sort((p1, p2) => string.Compare(p1.Name, p2.Name, StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            string projectName = project.Name;
            string projectPath = project.Path;

            // Get the project file name and extension
            string projectFileName = Path.GetFileName(projectPath);

            string indent = new string(' ', indentLevel * 2);
            output.AppendLine($"{indent}- {projectName} ({projectFileName})");

            string projectFilePath = Path.Combine(Path.GetDirectoryName(solutionPath), projectPath);
            GenerateProjectStructure(projectFilePath, output, indentLevel + 1, filePaths);
        }
    }

    static void GenerateProjectStructure(string projectFilePath, StringBuilder output, int indentLevel, List<string> filePaths)
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
            string referenceProjectFileName = Path.GetFileName(referencePath);
            output.AppendLine($"{indent}  - {referenceProjectFileName}");
        }

        foreach (Match packageMatch in packageMatches)
        {
            string packageName = packageMatch.Groups[1].Value;
            string packageVersion = packageMatch.Groups[2].Value;
            output.AppendLine($"{indent}  - {packageName} ({packageVersion})");
        }

        string projectDirectory = Path.GetDirectoryName(projectFilePath);
        GenerateProjectFolderStructure(projectDirectory, output, indentLevel, filePaths);
    }

    static void GenerateProjectFolderStructure(string projectDirectory, StringBuilder output, int indentLevel, List<string> filePaths)
    {
        var directories = Directory.GetDirectories(projectDirectory)
            .Where(d => !Path.GetFileName(d).Equals("bin", StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(d).Equals("obj", StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(d).Equals(".git", StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(d).Equals(".vs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d);

        foreach (string directory in directories)
        {
            string directoryName = Path.GetFileName(directory);
            string indent = new string(' ', indentLevel * 2);
            output.AppendLine($"{indent}- {directoryName}");
            GenerateProjectFolderStructure(directory, output, indentLevel + 1, filePaths);
        }

        var files = Directory.GetFiles(projectDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => _outputExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f);

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string indent = new string(' ', indentLevel * 2);
            output.AppendLine($"{indent}- {fileName}");
            filePaths.Add(file);
        }
    }

    static void GenerateFileContents(string solutionDirectory, List<string> filePaths, StringBuilder output)
    {
        foreach (string file in filePaths)
        {
            string fileContent = File.ReadAllText(file);
            string relativePath = Path.GetRelativePath(solutionDirectory, file);
            output.AppendLine($"<{relativePath}>");
            output.AppendLine(fileContent);
            output.AppendLine($"</{relativePath}>");
        }
    }
}