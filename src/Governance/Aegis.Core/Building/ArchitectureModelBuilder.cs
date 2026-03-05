namespace Aegis.Core.Building;

using Aegis.Core.Model;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

public sealed class ArchitectureModelBuilder
{
    private readonly LayerClassifier _classifier;

    public ArchitectureModelBuilder(LayerClassifier? classifier = null)
    {
        _classifier = classifier ?? LayerClassifier.Default();
    }

    public Task<ArchitectureModel> BuildAsync(string path, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            ? BuildFromSolutionAsync(path, ct)
            : BuildFromDirectoryAsync(path, ct);
    }

    public async Task<ArchitectureModel> BuildFromSolutionAsync(string solutionPath, CancellationToken ct = default)
    {
        if (!File.Exists(solutionPath))
            throw new FileNotFoundException($"Solution file not found: {solutionPath}");

        var workspace = CreateWorkspace();
        Console.Error.WriteLine($"[Aegis] Loading solution: {solutionPath}");
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: ct);

        var projectNameSet = solution.Projects
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ordered = solution
            .GetProjectDependencyGraph()
            .GetTopologicallySortedProjects(ct)
            .Select(id => solution.GetProject(id))
            .Where(p => p != null)
            .Cast<Microsoft.CodeAnalysis.Project>()
            .ToList();

        var services = new List<ServiceModel>();
        foreach (var project in ordered)
        {
            ct.ThrowIfCancellationRequested();
            Console.Error.WriteLine($"[Aegis]   Scanning: {project.Name}");
            var svc = await ExtractServiceModelAsync(project, ct, projectNameSet);
            if (svc != null) services.Add(svc);
        }

        return FinalizeModel(services);
    }

    public async Task<ArchitectureModel> BuildFromDirectoryAsync(string rootPath, CancellationToken ct = default)
    {
        var workspace = CreateWorkspace();

        var csprojFiles = File.Exists(rootPath) && rootPath.EndsWith(".csproj")
            ? [rootPath]
            : Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);

        var projectNameSet = csprojFiles
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => n != null)
            .Select(n => n!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var services = new List<ServiceModel>();

        foreach (var csproj in csprojFiles)
        {
            ct.ThrowIfCancellationRequested();

            Microsoft.CodeAnalysis.Project? project = null;

            // Check if already loaded as a transitive reference
            project = workspace.CurrentSolution.Projects
                .FirstOrDefault(p => string.Equals(p.FilePath, csproj, StringComparison.OrdinalIgnoreCase));

            if (project == null)
            {
                try
                {
                    project = await workspace.OpenProjectAsync(csproj, cancellationToken: ct);
                }
                catch (ArgumentException ex) when (ex.Message.Contains("already part of the workspace"))
                {
                    // Race: loaded as transitive reference during OpenProjectAsync of a prior project
                    project = workspace.CurrentSolution.Projects
                        .FirstOrDefault(p => string.Equals(p.FilePath, csproj, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (project == null) continue;

            var svc = await ExtractServiceModelAsync(project, ct, projectNameSet);
            if (svc != null) services.Add(svc);
        }

        return FinalizeModel(services);
    }

    private async Task<ServiceModel?> ExtractServiceModelAsync(
        Microsoft.CodeAnalysis.Project project,
        CancellationToken ct,
        HashSet<string>? loadedProjectNames)
    {
        var compilation = await project.GetCompilationAsync(ct);
        if (compilation == null) return null;

        var projectDir = project.Documents
            .Where(d => d.FilePath != null)
            .Select(d => Path.GetDirectoryName(d.FilePath!))
            .FirstOrDefault(d => d != null) ?? string.Empty;

        var types     = new List<TypeNode>();
        var edges     = new List<DependencyEdge>();
        var diRegs    = new List<DiRegistration>();
        var kafkaProd = new List<KafkaProduction>();
        var kafkaCons = new List<KafkaConsumption>();

        foreach (var doc in project.Documents)
        {
            if (doc.FilePath == null) continue;
            if (IsGeneratedFile(doc.FilePath)) continue;
            ct.ThrowIfCancellationRequested();

            var tree = await doc.GetSyntaxTreeAsync(ct);
            if (tree == null) continue;
            var root  = await tree.GetRootAsync(ct);
            var model = compilation.GetSemanticModel(tree);
            var layer = _classifier.Classify(doc.FilePath, projectDir);

            var extractor = new FileModelExtractor(
                root, model, layer, project.Name, doc.FilePath, loadedProjectNames);
            var result = extractor.Extract();

            types.AddRange(result.Types);
            edges.AddRange(result.Edges);
            diRegs.AddRange(result.DiRegistrations);
            kafkaProd.AddRange(result.KafkaProducers);
            kafkaCons.AddRange(result.KafkaConsumers);
        }

        return new ServiceModel
        {
            ProjectName     = project.Name,
            ProjectPath     = project.FilePath ?? project.Name,
            Types           = types,
            Edges           = edges,
            DiRegistrations = diRegs,
            KafkaProducers  = kafkaProd,
            KafkaConsumers  = kafkaCons,
        };
    }

    private MSBuildWorkspace CreateWorkspace()
    {
        if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();
        var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) =>
        {
            // Suppress NuGet audit noise (offline dev environment — no nuget.org access)
            if (e.Diagnostic.Message.Contains("vulnerability data") ||
                e.Diagnostic.Message.Contains("nuget.org"))
                return;

            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure ||
                !e.Diagnostic.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"[Aegis:Workspace] {e.Diagnostic.Kind}: {e.Diagnostic.Message}");
            }
        };
        return workspace;
    }

    private static bool IsGeneratedFile(string filePath)
    {
        var name = Path.GetFileName(filePath);
        return filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
            || filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
            || name.EndsWith(".g.cs",            StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".g.i.cs",          StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".Designer.cs",     StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static ArchitectureModel FinalizeModel(List<ServiceModel> services) => new()
    {
        FormatVersion     = "2.0",
        CapturedAt        = DateTime.UtcNow,
        Services          = services,
        CrossServiceEdges = BuildCrossServiceEdges(services),
    };

    private static IReadOnlyList<DependencyEdge> BuildCrossServiceEdges(List<ServiceModel> services)
    {
        var projectNames = services.Select(s => s.ProjectName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var crossEdges   = new List<DependencyEdge>();

        foreach (var svc in services)
        foreach (var edge in svc.Edges)
        {
            if (edge.ToProjectName != null
                && !edge.ToProjectName.Equals(svc.ProjectName, StringComparison.OrdinalIgnoreCase)
                && projectNames.Contains(edge.ToProjectName))
            {
                crossEdges.Add(edge);
            }
        }

        return crossEdges;
    }
}
