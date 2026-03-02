// ============================================================
// Aegis.Core — Layer 2: Architecture Model Builder
//
// Converts Roslyn semantic analysis → ArchitectureModel
// No output. No reporting. Pure model construction.
// Single responsibility: build the typed graph.
// ============================================================

// ── Building/ArchitectureModelBuilder.cs ─────────────────────
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

    // ── Public entry points ──────────────────────────────────────────────────

    /// <summary>
    /// Smart entry point. Dispatches based on path type:
    ///   .sln file  → BuildFromSolutionAsync  (preferred for real solutions)
    ///   .csproj    → single-project load via BuildFromDirectoryAsync
    ///   directory  → recursive .csproj scan via BuildFromDirectoryAsync
    /// </summary>
    public Task<ArchitectureModel> BuildAsync(string path, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            ? BuildFromSolutionAsync(path, ct)
            : BuildFromDirectoryAsync(path, ct);
    }

    /// <summary>
    /// Loads all projects from a .sln file using Roslyn's MSBuildWorkspace.OpenSolutionAsync.
    /// Projects are processed in topological dependency order so cross-project references resolve.
    /// Project names from the solution form the boundary for cross-service edge detection.
    /// </summary>
    public async Task<ArchitectureModel> BuildFromSolutionAsync(
        string solutionPath, CancellationToken ct = default)
    {
        if (!File.Exists(solutionPath))
            throw new FileNotFoundException($"Solution file not found: {solutionPath}");

        var workspace = CreateWorkspace();

        Console.Error.WriteLine($"[Aegis] Loading solution: {solutionPath}");
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: ct);

        // First pass: collect ALL project names from the solution for cross-service detection.
        // Using solution project names is more accurate than assembly-scanning:
        // it excludes NuGet packages and SDK projects not in the solution.
        var projectNameSet = solution.Projects
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Process in topological order so <ProjectReference> edges resolve correctly.
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

    /// Scans a directory recursively for .csproj files and loads each independently.
    /// Falls back when no .sln is available. Less accurate for cross-project references.
    public async Task<ArchitectureModel> BuildFromDirectoryAsync(
        string rootPath, CancellationToken ct = default)
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
            var project = await workspace.OpenProjectAsync(csproj, cancellationToken: ct);
            var svc     = await ExtractServiceModelAsync(project, ct, projectNameSet);
            if (svc != null) services.Add(svc);
        }

        return FinalizeModel(services);
    }

    // ── Core extraction ──────────────────────────────────────────────────────

    private async Task<ServiceModel?> ExtractServiceModelAsync(
        Microsoft.CodeAnalysis.Project project,
        CancellationToken ct,
        HashSet<string>? loadedProjectNames)
    {
        var compilation = await project.GetCompilationAsync(ct);
        if (compilation == null) return null;

        // Infer root directory from the project's first source file.
        // Used by LayerClassifier to map file paths → architecture layers.
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
            // Skip generated files (obj/, .g.cs) — they contain noise, not architecture
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private MSBuildWorkspace CreateWorkspace()
    {
        var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) =>
        {
            // Suppress low-signal Info diagnostics; always surface Failures
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
        // Skip Roslyn source generators, designer files, and build artefacts
        var name = Path.GetFileName(filePath);
        return filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
            || filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
            || name.EndsWith(".g.cs",        StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".g.i.cs",      StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
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
        {
            foreach (var edge in svc.Edges)
            {
                if (edge.ToProjectName != null
                    && !edge.ToProjectName.Equals(svc.ProjectName, StringComparison.OrdinalIgnoreCase)
                    && projectNames.Contains(edge.ToProjectName))
                {
                    crossEdges.Add(edge);
                }
            }
        }

        return crossEdges;
    }
}

// ── Building/FileModelExtractor.cs ───────────────────────────
namespace Aegis.Core.Building;

using Aegis.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal sealed class FileModelExtractor(
    SyntaxNode root,
    SemanticModel model,
    ArchitectureLayer layer,
    string projectName,
    string filePath,
    HashSet<string>? loadedProjectNames = null)
{
    public FileExtractionResult Extract()
    {
        var types  = new List<TypeNode>();
        var edges  = new List<DependencyEdge>();
        var diRegs = new List<DiRegistration>();
        var kafkaP = new List<KafkaProduction>();
        var kafkaC = new List<KafkaConsumption>();

        foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var sym = model.GetDeclaredSymbol(cls);
            if (sym == null) continue;

            var node = TypeNodeFactory.FromClass(sym, layer, projectName);
            if (node == null) continue;

            types.Add(node);
            edges.AddRange(EdgeExtractor.FromConstructors(sym, node, model, projectName, loadedProjectNames));
            edges.AddRange(EdgeExtractor.FromInheritance(sym, node, projectName, loadedProjectNames));

            // DI registrations from Program.cs / extension methods
            if (IsDiFile(filePath))
                diRegs.AddRange(DiExtractor.Extract(cls, model, projectName));

            // Kafka
            kafkaP.AddRange(KafkaExtractor.ExtractProducers(cls, sym, model, projectName));
            var consumer = KafkaExtractor.ExtractConsumer(cls, sym, model, projectName);
            if (consumer != null) kafkaC.Add(consumer);
        }

        foreach (var iface in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            var sym  = model.GetDeclaredSymbol(iface);
            if (sym == null) continue;
            var node = TypeNodeFactory.FromInterface(sym, layer, projectName);
            if (node != null) types.Add(node);
        }

        foreach (var rec in root.DescendantNodes().OfType<RecordDeclarationSyntax>())
        {
            var sym  = model.GetDeclaredSymbol(rec);
            if (sym == null) continue;
            var node = TypeNodeFactory.FromRecord(sym, layer, projectName);
            if (node != null) types.Add(node);
        }

        foreach (var e in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            var sym  = model.GetDeclaredSymbol(e);
            if (sym == null) continue;
            types.Add(TypeNodeFactory.FromEnum(sym, layer, projectName));
        }

        return new FileExtractionResult(types, edges, diRegs, kafkaP, kafkaC);
    }

    private static bool IsDiFile(string path) =>
        path.EndsWith("Program.cs",    StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("Startup.cs",    StringComparison.OrdinalIgnoreCase) ||
        path.Contains("Extensions",    StringComparison.OrdinalIgnoreCase) ||
        path.Contains("ServiceCollection", StringComparison.OrdinalIgnoreCase);
}

internal record FileExtractionResult(
    List<TypeNode>        Types,
    List<DependencyEdge>  Edges,
    List<DiRegistration>  DiRegistrations,
    List<KafkaProduction> KafkaProducers,
    List<KafkaConsumption> KafkaConsumers);

// ── Building/TypeNodeFactory.cs ──────────────────────────────
namespace Aegis.Core.Building;

using Aegis.Core.Model;
using Microsoft.CodeAnalysis;

internal static class TypeNodeFactory
{
    public static TypeNode? FromClass(INamedTypeSymbol sym, ArchitectureLayer layer, string project)
    {
        var kind = ResolveKind(sym);
        var bases = sym.BaseType != null
            ? [sym.BaseType.ToDisplayString(), ..sym.Interfaces.Select(i => i.ToDisplayString())]
            : sym.Interfaces.Select(i => i.ToDisplayString()).ToArray();

        var rawAttributes = sym.GetAttributes().Select(a => a.AttributeClass?.Name ?? "").ToList();

        // For event types, extract public properties as the contract surface (used by EventSchemaCompatibilityRule).
        // For all other types, extract public methods as usual.
        var isEventType = kind is NodeKind.DomainEvent or NodeKind.IntegrationEvent
            || sym.AllInterfaces.Any(i => i.Name is "IDomainEvent" or "IIntegrationEvent" or "ITenantEvent");

        // Detect static mutable state — emit synthetic marker for StaticStateRule
        var hasStaticState = HasStaticMutableState(sym);
        if (hasStaticState) rawAttributes.Add("HasStaticState");

        var node = new TypeNode
        {
            FullName    = sym.ToDisplayString(),
            ShortName   = sym.Name,
            Namespace   = sym.ContainingNamespace.ToDisplayString(),
            ProjectName = project,
            Layer       = layer,
            Kind        = kind,
            IsAbstract  = sym.IsAbstract,
            IsGeneric   = sym.IsGenericType,
            BaseTypes   = bases.Where(b => !b.StartsWith("System.Object")).ToList(),
            Interfaces  = sym.Interfaces.Select(i => i.ToDisplayString()).ToList(),
            Methods     = isEventType ? ExtractEventProperties(sym) : ExtractMethods(sym),
            Attributes  = rawAttributes,

            // Controller
            RouteTemplate = sym.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "RouteAttribute")
                ?.ConstructorArguments.FirstOrDefault().Value?.ToString(),
            RequiresAuth = sym.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "AuthorizeAttribute"),
            Endpoints    = ExtractEndpoints(sym),

            // DbContext
            DbSets    = sym.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.Type.Name == "DbSet")
                .Select(p => p.Name).ToList(),
            DbProvider = null, // Enriched by DbProviderResolver

            // MediatR
            RequestType  = ResolveHandlerTypeArg(sym, 0),
            ResponseType = ResolveHandlerTypeArg(sym, 1),
        };

        return node;
    }

    public static TypeNode? FromInterface(INamedTypeSymbol sym, ArchitectureLayer layer, string project) =>
        new TypeNode
        {
            FullName    = sym.ToDisplayString(),
            ShortName   = sym.Name,
            Namespace   = sym.ContainingNamespace.ToDisplayString(),
            ProjectName = project,
            Layer       = layer,
            Kind        = NodeKind.Interface,
            Methods     = ExtractMethods(sym),
        };

    public static TypeNode? FromRecord(INamedTypeSymbol sym, ArchitectureLayer layer, string project)
    {
        var isDomainEvent = sym.AllInterfaces.Any(i =>
            i.Name is "IDomainEvent" or "IIntegrationEvent" or "ITenantEvent");

        return new TypeNode
        {
            FullName    = sym.ToDisplayString(),
            ShortName   = sym.Name,
            Namespace   = sym.ContainingNamespace.ToDisplayString(),
            ProjectName = project,
            Layer       = layer,
            Kind        = isDomainEvent ? NodeKind.DomainEvent : NodeKind.Record,
            // For event types, extract public properties as zero-parameter MethodContracts.
            // EventSchemaCompatibilityRule reads these to compare against stored baseline fields.
            // For non-event records, also extract methods as normal.
            Methods     = isDomainEvent
                ? ExtractEventProperties(sym)
                : ExtractMethods(sym),
        };
    }

    /// Extracts public instance properties as MethodContract entries with zero parameters.
    /// This is the canonical representation used by EventSchemaCompatibilityRule for field comparison.
    private static IReadOnlyList<MethodContract> ExtractEventProperties(INamedTypeSymbol sym) =>
        sym.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
            .Select(p => new MethodContract
            {
                Name       = p.Name,
                ReturnType = p.Type.ToDisplayString(),
                IsPublic   = true,
                IsAsync    = false,
                Parameters = [],
            }).ToList();

    public static TypeNode FromEnum(INamedTypeSymbol sym, ArchitectureLayer layer, string project) =>
        new TypeNode
        {
            FullName    = sym.ToDisplayString(),
            ShortName   = sym.Name,
            Namespace   = sym.ContainingNamespace.ToDisplayString(),
            ProjectName = project,
            Layer       = layer,
            Kind        = NodeKind.Enum,
        };

    private static NodeKind ResolveKind(INamedTypeSymbol sym)
    {
        if (sym.GetAttributes().Any(a => a.AttributeClass?.Name
            is "ApiControllerAttribute" or "ControllerAttribute")) return NodeKind.Controller;

        if (sym.BaseType?.Name == "DbContext") return NodeKind.DbContext;

        if (sym.AllInterfaces.Any(i => i.Name
            is "IRequestHandler" or "ICommandHandler" or "IQueryHandler")) return NodeKind.MediatRHandler;

        return NodeKind.Class;
    }

    private static IReadOnlyList<MethodContract> ExtractMethods(INamespaceOrTypeSymbol sym) =>
        sym.GetMembers().OfType<IMethodSymbol>()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public && m.MethodKind == MethodKind.Ordinary)
            .Select(m => new MethodContract
            {
                Name       = m.Name,
                ReturnType = m.ReturnType.ToDisplayString(),
                IsPublic   = true,
                IsAsync    = m.IsAsync,
                Parameters = m.Parameters.Select(p => new ParameterContract
                {
                    Name         = p.Name,
                    TypeName     = p.Type.Name,
                    TypeFullName = p.Type.ToDisplayString(),
                }).ToList(),
            }).ToList();

    private static IReadOnlyList<EndpointContract> ExtractEndpoints(INamedTypeSymbol sym)
    {
        return sym.GetMembers().OfType<IMethodSymbol>()
            .SelectMany(m => m.GetAttributes()
                .Where(a => a.AttributeClass?.Name is
                    "HttpGetAttribute" or "HttpPostAttribute" or "HttpPutAttribute" or
                    "HttpDeleteAttribute" or "HttpPatchAttribute")
                .Select(a => new EndpointContract
                {
                    MethodName    = m.Name,
                    HttpVerb      = a.AttributeClass!.Name.Replace("Attribute", "").Replace("Http", ""),
                    RouteTemplate = a.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "",
                }))
            .ToList();
    }

    private static string? ResolveHandlerTypeArg(INamedTypeSymbol sym, int index)
    {
        var handlerIface = sym.AllInterfaces
            .FirstOrDefault(i => i.Name is "IRequestHandler" or "ICommandHandler" or "IQueryHandler");
        return handlerIface?.TypeArguments.ElementAtOrDefault(index)?.Name;
    }

    /// Detects static mutable state: static fields, static properties with setters,
    /// and static fields whose types are known mutable collection interfaces.
    private static bool HasStaticMutableState(INamedTypeSymbol sym)
    {
        foreach (var member in sym.GetMembers())
        {
            if (member is IFieldSymbol field && field.IsStatic && !field.IsConst && !field.IsReadOnly)
                return true;

            if (member is IPropertySymbol prop && prop.IsStatic && prop.SetMethod != null)
                return true;

            // Static readonly field holding a mutable collection (List<T>, Dictionary<K,V>, etc.)
            if (member is IFieldSymbol roField && roField.IsStatic && roField.IsReadOnly)
            {
                var typeName = roField.Type.Name;
                if (typeName is "List" or "Dictionary" or "HashSet" or "ConcurrentDictionary"
                             or "ConcurrentBag" or "ConcurrentQueue" or "Queue" or "Stack")
                    return true;
            }
        }
        return false;
    }
}

// ── Building/EdgeExtractor.cs ─────────────────────────────────
namespace Aegis.Core.Building;

using Aegis.Core.Model;
using Microsoft.CodeAnalysis;

internal static class EdgeExtractor
{
    public static IEnumerable<DependencyEdge> FromConstructors(
        INamedTypeSymbol sym, TypeNode fromNode,
        SemanticModel model, string projectName, HashSet<string>? loadedProjectNames = null)
    {
        return sym.Constructors
            .SelectMany(c => c.Parameters)
            .Select(p => new DependencyEdge
            {
                From         = fromNode,
                ToFullName   = p.Type.ToDisplayString(),
                ToNamespace  = p.Type.ContainingNamespace.ToDisplayString(),
                Kind         = EdgeKind.ConstructorInjection,
                ToProjectName = ResolveProject(p.Type, loadedProjectNames),
                ToLayer      = ResolveLayer(p.Type),
            });
    }

    public static IEnumerable<DependencyEdge> FromInheritance(
        INamedTypeSymbol sym, TypeNode fromNode, string projectName, HashSet<string>? loadedProjectNames = null)
    {
        var edges = new List<DependencyEdge>();

        if (sym.BaseType != null && sym.BaseType.Name != "Object")
            edges.Add(new DependencyEdge
            {
                From          = fromNode,
                ToFullName    = sym.BaseType.ToDisplayString(),
                ToNamespace   = sym.BaseType.ContainingNamespace.ToDisplayString(),
                Kind          = EdgeKind.Inheritance,
                ToProjectName = ResolveProject(sym.BaseType, loadedProjectNames),
                ToLayer       = ResolveLayer(sym.BaseType),
            });

        edges.AddRange(sym.Interfaces.Select(i => new DependencyEdge
        {
            From          = fromNode,
            ToFullName    = i.ToDisplayString(),
            ToNamespace   = i.ContainingNamespace.ToDisplayString(),
            Kind          = EdgeKind.InterfaceImplementation,
            ToProjectName = ResolveProject(i, loadedProjectNames),
            ToLayer       = ResolveLayer(i),
        }));

        return edges;
    }

    // Resolves to a known project name by comparing the type's containing assembly name
    // against the set of loaded project names. Returns null for external/system types.
    private static string? ResolveProject(ITypeSymbol type, string currentProject)
        => ResolveProject(type, loadedProjectNames: null);

    internal static string? ResolveProject(ITypeSymbol type, HashSet<string>? loadedProjectNames)
    {
        var assemblyName = type.ContainingAssembly?.Name;
        if (assemblyName == null) return null;
        if (assemblyName.StartsWith("System") || assemblyName.StartsWith("Microsoft")) return null;
        // Only classify as internal project if name appears in the loaded project set.
        if (loadedProjectNames != null && !loadedProjectNames.Contains(assemblyName)) return null;
        return assemblyName;
    }

    /// Resolves layer from the type's containing assembly name (which maps 1:1 to a project),
    /// falling back to the namespace-segment heuristic only for external or ambiguous types.
    /// Call ResolveLayer(type, model) when an ArchitectureModel is available for full fidelity.
    private static ArchitectureLayer ResolveLayer(ITypeSymbol type)
    {
        // Primary: assembly name tells us the project; project folder structure tells us the layer.
        // This is resolved by the LayerClassifier during the build phase and stored in TypeNode.
        // Here in EdgeExtractor we don't have the model, so we fall back to folder-segment matching
        // on the assembly name itself (which conventionally encodes the layer in .NET projects).
        var assembly = type.ContainingAssembly?.Name ?? string.Empty;
        if (assembly.EndsWith(".Domain",          StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.Domain;
        if (assembly.EndsWith(".Application",     StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.Application;
        if (assembly.EndsWith(".Infrastructure",  StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.Infrastructure;
        if (assembly.EndsWith(".SharedKernel",    StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.SharedKernel;
        if (assembly.EndsWith(".Contracts",       StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.SharedKernel;
        if (assembly.EndsWith(".API",             StringComparison.OrdinalIgnoreCase)) return ArchitectureLayer.API;

        // Secondary: check namespace segments (covers projects whose name doesn't encode the layer)
        var ns = type.ContainingNamespace.ToDisplayString();
        foreach (var segment in ns.Split('.'))
        {
            switch (segment)
            {
                case "Domain":         return ArchitectureLayer.Domain;
                case "Application":    return ArchitectureLayer.Application;
                case "Infrastructure": return ArchitectureLayer.Infrastructure;
                case "SharedKernel":
                case "Contracts":      return ArchitectureLayer.SharedKernel;
                case "API":
                case "Controllers":    return ArchitectureLayer.API;
            }
        }

        return ArchitectureLayer.Unknown;
    }
}

// ── Building/LayerClassifier.cs ───────────────────────────────
namespace Aegis.Core.Building;

using Aegis.Core.Model;

public sealed class LayerClassifier
{
    private readonly Dictionary<string, ArchitectureLayer> _folderMap;

    public LayerClassifier(Dictionary<string, ArchitectureLayer> folderMap)
    {
        _folderMap = folderMap;
    }

    public static LayerClassifier Default() => new(new(StringComparer.OrdinalIgnoreCase)
    {
        ["Domain"]          = ArchitectureLayer.Domain,
        ["Application"]     = ArchitectureLayer.Application,
        ["Infrastructure"]  = ArchitectureLayer.Infrastructure,
        ["Persistence"]     = ArchitectureLayer.Infrastructure,
        ["API"]             = ArchitectureLayer.API,
        ["Controllers"]     = ArchitectureLayer.API,
        ["SharedKernel"]    = ArchitectureLayer.SharedKernel,
        ["Contracts"]       = ArchitectureLayer.SharedKernel,
    });

    public ArchitectureLayer Classify(string filePath, string projectRoot)
    {
        var relative = Path.GetRelativePath(projectRoot, filePath);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar))
            if (_folderMap.TryGetValue(segment, out var layer)) return layer;
        return ArchitectureLayer.Unknown;
    }
}

// ── Building/DiExtractor.cs ───────────────────────────────────
namespace Aegis.Core.Building;

using Aegis.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Extracts DI registrations from Program.cs / Startup.cs / extension methods.
///
/// Covers:
///   1. Generic:     AddScoped&lt;IFoo, FooImpl&gt;()
///   2. Non-generic: AddScoped(typeof(IFoo), typeof(FooImpl))
///   3. HttpClient:  AddHttpClient&lt;IFoo, FooImpl&gt;() — lifetime is Transient (HttpClient default)
///   4. Polly:       .AddResilienceHandler(...) / .AddPolicyHandler(...) chained after AddHttpClient
///                   → emits HasResiliencePolicy synthetic attribute on the implementation type
///   5. Single-type: AddSingleton&lt;Foo&gt;() — service == implementation
/// </summary>
internal static class DiExtractor
{
    private static readonly HashSet<string> _lifetimeMethods =
        ["AddScoped", "AddTransient", "AddSingleton"];

    private static readonly HashSet<string> _httpClientMethods =
        ["AddHttpClient", "AddHttpClientFactory"];

    private static readonly HashSet<string> _resilienceMethods =
        ["AddResilienceHandler", "AddPolicyHandler", "AddTransientHttpErrorPolicy",
         "AddRetryPolicy", "AddCircuitBreakerPolicy", "AddTimeoutPolicy", "AddFallbackPolicy"];

    public static IEnumerable<DiRegistration> Extract(
        ClassDeclarationSyntax cls, SemanticModel model, string project)
    {
        // Track which implementation types have a resilience policy chained onto them
        var resilienceDecorated = CollectResilienceDecoratedClients(cls, model);

        foreach (var inv in cls.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var sym = model.GetSymbolInfo(inv).Symbol as IMethodSymbol;
            if (sym == null) continue;

            // ── HttpClient registrations ─────────────────────────────────────────
            if (_httpClientMethods.Contains(sym.Name))
            {
                foreach (var reg in ExtractHttpClientRegistration(inv, sym, model, project, resilienceDecorated))
                    yield return reg;
                continue;
            }

            // ── Standard lifetime registrations ──────────────────────────────────
            if (!_lifetimeMethods.Contains(sym.Name)) continue;

            var lifetime = sym.Name switch
            {
                "AddScoped"    => DiLifetime.Scoped,
                "AddTransient" => DiLifetime.Transient,
                _              => DiLifetime.Singleton,
            };

            // Generic form: AddScoped<IFoo, FooImpl>()
            if (sym.TypeArguments.Length == 2)
            {
                yield return new DiRegistration
                {
                    ServiceType        = sym.TypeArguments[0].ToDisplayString(),
                    ImplementationType = sym.TypeArguments[1].ToDisplayString(),
                    Lifetime           = lifetime,
                    ProjectName        = project,
                };
                continue;
            }

            // Single-type generic: AddSingleton<Foo>()
            if (sym.TypeArguments.Length == 1)
            {
                yield return new DiRegistration
                {
                    ServiceType        = sym.TypeArguments[0].ToDisplayString(),
                    ImplementationType = sym.TypeArguments[0].ToDisplayString(),
                    Lifetime           = lifetime,
                    ProjectName        = project,
                };
                continue;
            }

            // Non-generic: AddScoped(typeof(IFoo), typeof(FooImpl))
            var args = inv.ArgumentList.Arguments;
            if (args.Count == 2
                && args[0].Expression is TypeOfExpressionSyntax svcTypeOf
                && args[1].Expression is TypeOfExpressionSyntax implTypeOf)
            {
                var svcSym  = model.GetTypeInfo(svcTypeOf.Type).Type;
                var implSym = model.GetTypeInfo(implTypeOf.Type).Type;
                if (svcSym != null && implSym != null)
                {
                    yield return new DiRegistration
                    {
                        ServiceType        = svcSym.ToDisplayString(),
                        ImplementationType = implSym.ToDisplayString(),
                        Lifetime           = lifetime,
                        ProjectName        = project,
                    };
                }
            }
        }
    }

    // ── HttpClient extraction ─────────────────────────────────────────────────

    private static IEnumerable<DiRegistration> ExtractHttpClientRegistration(
        InvocationExpressionSyntax inv, IMethodSymbol sym,
        SemanticModel model, string project,
        HashSet<string> resilienceDecorated)
    {
        // AddHttpClient<IFoo, FooImpl>() — typed client, Transient lifetime
        if (sym.TypeArguments.Length == 2)
        {
            var serviceType = sym.TypeArguments[0].ToDisplayString();
            var implType    = sym.TypeArguments[1].ToDisplayString();
            var implName    = sym.TypeArguments[1].Name;

            yield return new DiRegistration
            {
                ServiceType        = serviceType,
                ImplementationType = implType,
                Lifetime           = DiLifetime.Transient,
                ProjectName        = project,
                HasResiliencePolicy = resilienceDecorated.Contains(serviceType)
                                   || resilienceDecorated.Contains(implType)
                                   || resilienceDecorated.Contains(implName),
            };
            yield break;
        }

        // AddHttpClient<IFoo>() — single-type typed client
        if (sym.TypeArguments.Length == 1)
        {
            var typeName = sym.TypeArguments[0].ToDisplayString();
            yield return new DiRegistration
            {
                ServiceType        = typeName,
                ImplementationType = typeName,
                Lifetime           = DiLifetime.Transient,
                ProjectName        = project,
                HasResiliencePolicy = resilienceDecorated.Contains(typeName)
                                   || resilienceDecorated.Contains(sym.TypeArguments[0].Name),
            };
            yield break;
        }

        // AddHttpClient("named-client") — untyped named client
        var firstArg = inv.ArgumentList.Arguments.FirstOrDefault();
        if (firstArg != null)
        {
            var clientName = model.GetConstantValue(firstArg.Expression).Value?.ToString() ?? "HttpClient";
            yield return new DiRegistration
            {
                ServiceType        = $"IHttpClientFactory[{clientName}]",
                ImplementationType = $"HttpClient[{clientName}]",
                Lifetime           = DiLifetime.Transient,
                ProjectName        = project,
                HasResiliencePolicy = resilienceDecorated.Contains(clientName),
            };
        }
    }

    /// <summary>
    /// Walks the syntax tree to find method-chain calls that indicate a Polly / resilience policy
    /// is attached to an HttpClient registration. Collects the type name that the AddHttpClient
    /// call was typed against so we can set HasResiliencePolicy on that registration.
    ///
    /// Pattern detected:
    ///   services.AddHttpClient&lt;IFoo, FooImpl&gt;()
    ///            .AddResilienceHandler(...)   ← or AddPolicyHandler / AddTransientHttpErrorPolicy
    ///
    /// We walk all member-access chains. When we find a resilience method, we walk up the chain
    /// to find the AddHttpClient call and extract its type argument name(s).
    /// </summary>
    private static HashSet<string> CollectResilienceDecoratedClients(
        ClassDeclarationSyntax cls, SemanticModel model)
    {
        var decorated = new HashSet<string>(StringComparer.Ordinal);

        foreach (var inv in cls.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var sym = model.GetSymbolInfo(inv).Symbol as IMethodSymbol;
            if (sym == null || !_resilienceMethods.Contains(sym.Name)) continue;

            // Walk the method-chain expression upward to find AddHttpClient
            var chain = inv.Expression;
            while (chain is MemberAccessExpressionSyntax memberAccess)
            {
                chain = memberAccess.Expression;
                if (chain is InvocationExpressionSyntax chainedInv)
                {
                    var chainedSym = model.GetSymbolInfo(chainedInv).Symbol as IMethodSymbol;
                    if (chainedSym != null && _httpClientMethods.Contains(chainedSym.Name))
                    {
                        foreach (var typeArg in chainedSym.TypeArguments)
                            decorated.Add(typeArg.ToDisplayString());
                        // Also add short name for resilience rule matching
                        foreach (var typeArg in chainedSym.TypeArguments)
                            decorated.Add(typeArg.Name);

                        // Named client (string arg)
                        var firstArg = chainedInv.ArgumentList.Arguments.FirstOrDefault();
                        if (firstArg != null)
                        {
                            var name = model.GetConstantValue(firstArg.Expression).Value?.ToString();
                            if (name != null) decorated.Add(name);
                        }

                        break;
                    }
                    chain = chainedInv.Expression;
                }
            }
        }

        return decorated;
    }
}


// ── Building/KafkaExtractor.cs ────────────────────────────────
namespace Aegis.Core.Building;

using Aegis.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class KafkaExtractor
{
    private static readonly HashSet<string> _producerMethods =
        ["Produce", "ProduceAsync", "PublishAsync", "SendAsync"];

    private static readonly HashSet<string> _consumerBases =
        ["BackgroundService", "IHostedService"];

    public static IEnumerable<KafkaProduction> ExtractProducers(
        ClassDeclarationSyntax cls, INamedTypeSymbol sym,
        SemanticModel model, string project)
    {
        foreach (var inv in cls.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var methodSym = model.GetSymbolInfo(inv).Symbol as IMethodSymbol;
            if (methodSym == null || !_producerMethods.Contains(methodSym.Name)) continue;

            var firstArg = inv.ArgumentList.Arguments.FirstOrDefault();
            if (firstArg == null) continue;

            var eventType = model.GetTypeInfo(firstArg.Expression).Type;
            if (eventType == null) continue;

            yield return new KafkaProduction
            {
                ProducerClass = sym.Name,
                EventTypeName = eventType.Name,
                EventFullName = eventType.ToDisplayString(),
                ProjectName   = project,
            };
        }
    }

    public static KafkaConsumption? ExtractConsumer(
        ClassDeclarationSyntax cls, INamedTypeSymbol sym,
        SemanticModel model, string project)
    {
        var isConsumer = sym.BaseType?.Name == "BackgroundService"
            || sym.AllInterfaces.Any(i => _consumerBases.Contains(i.Name));

        if (!isConsumer) return null;

        // Resolve consumed event types from ConsumeResult<T> generic args
        var eventTypes = cls.DescendantNodes()
            .OfType<GenericNameSyntax>()
            .Where(g => g.Identifier.Text == "ConsumeResult")
            .SelectMany(g => g.TypeArgumentList.Arguments)
            .Select(t => model.GetTypeInfo(t).Type?.Name ?? t.ToString())
            .Distinct()
            .ToList();

        return new KafkaConsumption
        {
            ConsumerClass = sym.Name,
            EventTypes    = eventTypes,
            ProjectName   = project,
        };
    }
}

// ── Config/AegisConfig.cs ─────────────────────────────────────
namespace Aegis.Core.Config;

using Aegis.Core.Rules;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the optional aegis.config.json file.
/// Controls which rules are active, severity overrides, and allowed exceptions.
///
/// Minimal aegis.config.json:
/// {
///   "failLevel": "Error",
///   "disabledRules": ["AGS-007"],
///   "severityOverrides": { "AGS-004": "Warning" },
///   "allowedExceptions": [
///     { "ruleId": "AGS-001", "target": "LegacyService.Infrastructure.OldRepo", "reason": "Migration in progress" }
///   ]
/// }
/// </summary>
public sealed class AegisConfig
{
    /// Minimum severity that causes a non-zero exit code. Default: Error.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RuleSeverity FailLevel { get; init; } = RuleSeverity.Error;

    /// Rule IDs to skip entirely.
    public IReadOnlyList<string> DisabledRules { get; init; } = [];

    /// Rule categories to skip entirely (e.g. ["State", "Resilience"]).
    public IReadOnlyList<string> ExcludedCategories { get; init; } = [];

    /// Per-rule severity overrides (ruleId → severity string).
    public IReadOnlyDictionary<string, string> SeverityOverrides { get; init; } =
        new Dictionary<string, string>();

    /// Specific violation exceptions: the violation is demoted to Info.
    public IReadOnlyList<AllowedException> AllowedExceptions { get; init; } = [];

    /// Minimum rule version required for all active rules.
    /// Rules with a lower Version are treated as disabled.
    /// Example: "1.1" means rules still at "1.0" are excluded.
    public string? MinimumRuleVersion { get; init; }

    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas         = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
    };

    public static AegisConfig Default() => new();

    public static async Task<AegisConfig> LoadAsync(string configPath)
    {
        if (!File.Exists(configPath)) return Default();
        var json = await File.ReadAllTextAsync(configPath);
        return JsonSerializer.Deserialize<AegisConfig>(json, _opts) ?? Default();
    }

    /// Resolves the config file path from the project root (looks for aegis.config.json).
    public static string DefaultConfigPath(string projectRoot) =>
        Path.Combine(projectRoot, "aegis.config.json");

    /// Applies this config to a RuleEngineBuilder.
    public void Apply(Aegis.Core.Rules.RuleEngineBuilder builder)
    {
        foreach (var id in DisabledRules)
            builder.Disable(id);

        foreach (var (id, sevStr) in SeverityOverrides)
        {
            if (Enum.TryParse<RuleSeverity>(sevStr, ignoreCase: true, out var sev))
                builder.WithSeverityOverride(id, sev);
        }

        var cats = ExcludedCategories
            .Select(c => Enum.TryParse<Aegis.Core.Rules.RuleCategory>(c, ignoreCase: true, out var cat) ? (Aegis.Core.Rules.RuleCategory?)cat : null)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToArray();
        if (cats.Length > 0)
            builder.ExcludeCategories(cats);

        if (MinimumRuleVersion != null)
            builder.WithMinimumVersion(MinimumRuleVersion);
    }

    /// Returns true if a violation should be suppressed (demoted to Info) by an allowed exception.
    public bool IsSuppressed(Aegis.Core.Rules.RuleViolation v) =>
        AllowedExceptions.Any(e =>
            e.RuleId == v.RuleId &&
            (e.Target == null || e.Target == v.Subject?.FullName || e.Target == v.ProjectName));
}

public sealed class AllowedException
{
    public required string RuleId { get; init; }
    public string? Target         { get; init; }  // FullName or ProjectName; null = applies globally
    public string? Reason         { get; init; }  // Documentation only
}
