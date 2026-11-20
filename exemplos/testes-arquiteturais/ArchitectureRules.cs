namespace EngenhariaDotnet.Architecture.Tests;

internal sealed record ArchitectureViolation(string Rule, string Message);

internal static class ArchitectureRules
{
    private static readonly HashSet<string> KnownArchitectureLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Core",
        "Domain",
        "Application",
        "Infrastructure",
        "EntryPoint"
    };

    private static readonly HashSet<string> TestOnlyPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.NET.Test.Sdk",
        "NSubstitute",
        "Testcontainers.PostgreSql",
        "xunit",
        "xunit.runner.visualstudio"
    };

    public static IReadOnlyList<ArchitectureViolation> ValidateSolution(ProjectGraph graph)
    {
        var violations = new List<ArchitectureViolation>();
        violations.AddRange(ProjectReferencesMustResolve(graph));
        violations.AddRange(ProductionMustNotReferenceTests(graph));
        violations.AddRange(ProductionMustNotUseTestPackages(graph));
        violations.AddRange(TestProjectConventions(graph));
        violations.AddRange(LayerDependencies(graph));
        violations.AddRange(CoreMustRemainInfrastructureIndependent(graph));
        return violations;
    }

    public static IReadOnlyList<ArchitectureViolation> LayerDependencies(ProjectGraph graph)
    {
        var violations = new List<ArchitectureViolation>();

        foreach (var project in graph.Projects)
        {
            if (project.ArchitectureLayer is not null && !KnownArchitectureLayers.Contains(project.ArchitectureLayer))
            {
                violations.Add(new(
                    "unknown-architecture-layer",
                    $"{project.Name} declara a camada arquitetural desconhecida '{project.ArchitectureLayer}'."));
                continue;
            }

            foreach (var reference in project.ProjectReferences)
            {
                if (graph.TryGetProject(reference, out var referencedProject)
                    && IsForbiddenLayerDependency(project.ArchitectureLayer, referencedProject.ArchitectureLayer))
                {
                    violations.Add(new(
                        "layer-dependency-direction",
                        $"{project.Name} ({project.ArchitectureLayer}) não pode depender de "
                        + $"{referencedProject.Name} ({referencedProject.ArchitectureLayer})."));
                }
            }
        }

        return violations;
    }

    private static IEnumerable<ArchitectureViolation> ProjectReferencesMustResolve(ProjectGraph graph) =>
        from project in graph.Projects
        from reference in project.ProjectReferences
        where !graph.TryGetProject(reference, out _)
        select new ArchitectureViolation(
            "project-reference-outside-solution",
            $"{project.Name} referencia {DisplayReference(reference)}, que não está declarado em EngenhariaDotnet.slnx.");

    private static IEnumerable<ArchitectureViolation> ProductionMustNotReferenceTests(ProjectGraph graph)
    {
        foreach (var project in graph.Projects.Where(project => !project.IsTestProject))
        {
            foreach (var reference in project.ProjectReferences)
            {
                if (graph.TryGetProject(reference, out var referencedProject) && referencedProject.IsTestProject)
                {
                    yield return new(
                        "production-to-test",
                        $"{project.Name} não pode depender do projeto de teste {referencedProject.Name}.");
                }
            }
        }
    }

    private static IEnumerable<ArchitectureViolation> ProductionMustNotUseTestPackages(ProjectGraph graph) =>
        from project in graph.Projects
        where !project.IsTestProject
        from package in project.PackageReferences
        where TestOnlyPackages.Contains(package)
        select new ArchitectureViolation(
            "test-package-in-production",
            $"{project.Name} não pode depender do pacote de teste {package}.");

    private static IEnumerable<ArchitectureViolation> TestProjectConventions(ProjectGraph graph)
    {
        foreach (var project in graph.Projects.Where(project => project.IsTestProject))
        {
            if (!project.IsArchitectureTestProject && project.ProjectReferences.Count == 0)
            {
                yield return new(
                    "test-project-target",
                    $"{project.Name} precisa referenciar ao menos um projeto que exercita.");
            }
        }
    }

    private static IEnumerable<ArchitectureViolation> CoreMustRemainInfrastructureIndependent(
        ProjectGraph graph)
    {
        string[] forbiddenPackagePrefixes =
            ["Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite", "Npgsql", "OpenTelemetry", "ModelContextProtocol"];

        foreach (var core in graph.Projects.Where(project =>
                     string.Equals(project.ArchitectureLayer, "Core", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var reference in core.ProjectReferences)
            {
                var referencedName = graph.TryGetProject(reference, out var referencedProject)
                    ? referencedProject.Name
                    : DisplayReference(reference);
                yield return new(
                    "core-project-reference",
                    $"{core.Name} deve permanecer autocontido, mas referencia {referencedName}.");
            }

            foreach (var package in core.PackageReferences)
            {
                if (forbiddenPackagePrefixes.Any(prefix => package.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return new(
                        "core-infrastructure-package",
                        $"{core.Name} não pode depender do pacote de infraestrutura {package}.");
                }
            }
        }
    }

    private static bool IsForbiddenLayerDependency(string? source, string? target) =>
        (source?.ToUpperInvariant(), target?.ToUpperInvariant()) switch
    {
        ("DOMAIN", "APPLICATION" or "INFRASTRUCTURE" or "ENTRYPOINT") => true,
        ("APPLICATION", "INFRASTRUCTURE" or "ENTRYPOINT") => true,
        ("INFRASTRUCTURE", "ENTRYPOINT") => true,
        _ => false
    };

    private static string DisplayReference(string reference) => Path.GetFileNameWithoutExtension(reference);
}
