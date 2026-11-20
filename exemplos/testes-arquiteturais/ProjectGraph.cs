using System.Xml.Linq;

namespace EngenhariaDotnet.Architecture.Tests;

internal sealed record ProjectNode(
    string Name,
    bool IsTestProject,
    bool IsArchitectureTestProject,
    string? ArchitectureLayer,
    IReadOnlySet<string> ProjectReferences,
    IReadOnlySet<string> PackageReferences);

internal sealed class ProjectGraph
{
    private readonly IReadOnlyDictionary<string, ProjectNode> projects;

    private ProjectGraph(IEnumerable<ProjectNode> projects)
    {
        this.projects = projects.ToDictionary(project => project.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<ProjectNode> Projects => projects.Values.ToArray();

    public bool TryGetProject(string name, out ProjectNode project) => projects.TryGetValue(name, out project!);

    public static ProjectGraph Load(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath)
            ?? throw new InvalidOperationException($"A solução não tem diretório: {solutionPath}");
        var solution = XDocument.Load(solutionPath, LoadOptions.SetLineInfo);

        var projects = solution
            .Descendants("Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => LoadProject(Path.GetFullPath(path!, solutionDirectory)))
            .ToArray();

        return new ProjectGraph(projects);
    }

    public static ProjectGraph Create(params ProjectNode[] projects) => new(projects);

    private static ProjectNode LoadProject(string projectPath)
    {
        var document = XDocument.Load(projectPath, LoadOptions.SetLineInfo);
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"O projeto não tem diretório: {projectPath}");
        var name = Path.GetFileNameWithoutExtension(projectPath);

        var properties = document
            .Descendants()
            .Where(element => element.Parent?.Name.LocalName == "PropertyGroup")
            .GroupBy(element => element.Name.LocalName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);

        var references = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFileNameWithoutExtension(Path.GetFullPath(include!, projectDirectory)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var packages = document
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new ProjectNode(
            name,
            ReadBoolean(properties, "IsTestProject"),
            ReadBoolean(properties, "ArchitectureTestProject"),
            ReadValue(properties, "ArchitectureLayer"),
            references,
            packages);
    }

    private static bool ReadBoolean(IReadOnlyDictionary<string, string> properties, string name) =>
        properties.TryGetValue(name, out var value) && bool.TryParse(value, out var parsed) && parsed;

    private static string? ReadValue(IReadOnlyDictionary<string, string> properties, string name) =>
        properties.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
