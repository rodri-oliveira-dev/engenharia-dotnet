using Xunit;
using Xunit.Sdk;

namespace EngenhariaDotnet.Architecture.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Solucao_existente_respeita_as_fronteiras_declaradas()
    {
        var graph = ProjectGraph.Load(FindSolution());

        var violations = ArchitectureRules.ValidateSolution(graph);

        AssertNoViolations(violations);
    }

    [Fact]
    public void Fixture_positiva_respeita_a_direcao_das_camadas()
    {
        var graph = ProjectGraph.Create(
            Project("Politicas", architectureLayer: "Domain"),
            Project("CasosDeUso", references: ["Politicas"], architectureLayer: "Application"),
            Project(
                "BancoSql",
                references: ["CasosDeUso", "Politicas"],
                architectureLayer: "Infrastructure"),
            Project(
                "HostHttp",
                references: ["CasosDeUso", "BancoSql"],
                architectureLayer: "EntryPoint"));

        var violations = ArchitectureRules.LayerDependencies(graph);

        AssertNoViolations(violations);
    }

    [Fact]
    public void Fixture_negativa_detecta_dependencias_apontando_para_fora()
    {
        var graph = ProjectGraph.Create(
            Project("Politicas", references: ["CasosDeUso"], architectureLayer: "Domain"),
            Project("CasosDeUso", references: ["BancoSql"], architectureLayer: "Application"),
            Project("BancoSql", architectureLayer: "Infrastructure"));

        var violations = ArchitectureRules.LayerDependencies(graph);

        var failure = Assert.ThrowsAny<XunitException>(() => AssertNoViolations(violations));
        Assert.Contains("layer-dependency-direction", failure.Message, StringComparison.Ordinal);
        Assert.Collection(
            violations.OrderBy(violation => violation.Message),
            violation => Assert.Equal(
                "CasosDeUso (Application) não pode depender de BancoSql (Infrastructure).",
                violation.Message),
            violation => Assert.Equal(
                "Politicas (Domain) não pode depender de CasosDeUso (Application).",
                violation.Message));
    }

    [Fact]
    public void Fixture_positiva_aceita_alteracoes_legitimas_sem_depender_de_nomes()
    {
        var graph = ProjectGraph.Create(
            Project(
                "ModeloCentral",
                packages: ["System.Collections.Immutable"],
                architectureLayer: "Core"),
            Project("Verificacao", references: ["ModeloCentral"], isTestProject: true),
            Project("Medicoes", references: ["ModeloCentral"]),
            Project("TestesDaArquitetura", isTestProject: true, isArchitectureTestProject: true));

        var violations = ArchitectureRules.ValidateSolution(graph);

        AssertNoViolations(violations);
    }

    [Fact]
    public void Fixture_negativa_detecta_vazamentos_no_grafo_de_projetos()
    {
        var graph = ProjectGraph.Create(
            Project(
                "ModeloCentral",
                references: ["Pedidos"],
                packages: ["Microsoft.EntityFrameworkCore.Sqlite"],
                architectureLayer: "Core"),
            Project(
                "Pedidos",
                references: ["Pedidos.Tests"],
                packages: ["xunit"]),
            Project("Pedidos.Tests", isTestProject: true),
            Project("ProjetoSolto", references: ["ForaDaSolucao"]),
            Project("CamadaComErro", architectureLayer: "UseCases"));

        var violations = ArchitectureRules.ValidateSolution(graph);

        var failure = Assert.ThrowsAny<XunitException>(() => AssertNoViolations(violations));
        Assert.Contains("core-infrastructure-package", failure.Message, StringComparison.Ordinal);
        Assert.Equal(7, violations.Count);
        Assert.Contains(violations, violation => violation.Rule == "project-reference-outside-solution");
        Assert.Contains(violations, violation => violation.Rule == "production-to-test");
        Assert.Contains(violations, violation => violation.Rule == "test-package-in-production");
        Assert.Contains(violations, violation => violation.Rule == "test-project-target");
        Assert.Contains(violations, violation => violation.Rule == "unknown-architecture-layer");
        Assert.Contains(violations, violation => violation.Rule == "core-project-reference");
        Assert.Contains(violations, violation => violation.Rule == "core-infrastructure-package");
    }

    private static void AssertNoViolations(IReadOnlyCollection<ArchitectureViolation> violations) =>
        Assert.True(
            violations.Count == 0,
            string.Join(Environment.NewLine, violations.Select(violation => $"[{violation.Rule}] {violation.Message}")));

    private static ProjectNode Project(
        string name,
        string[]? references = null,
        string[]? packages = null,
        bool isTestProject = false,
        bool isArchitectureTestProject = false,
        string? architectureLayer = null) => new(
        name,
        isTestProject,
        isArchitectureTestProject,
        architectureLayer,
        (references ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase),
        (packages ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase));

    private static string FindSolution()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, "EngenhariaDotnet.slnx");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"Não foi possível localizar EngenhariaDotnet.slnx a partir de {AppContext.BaseDirectory}.");
    }
}
