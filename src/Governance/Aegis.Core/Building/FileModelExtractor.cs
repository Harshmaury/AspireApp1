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
            if (model.GetDeclaredSymbol(cls) is not INamedTypeSymbol sym) continue;
            var node = TypeNodeFactory.FromClass(sym, layer, projectName);
            if (node == null) continue;

            types.Add(node);
            edges.AddRange(EdgeExtractor.FromConstructors(sym, node, model, projectName, loadedProjectNames));
            edges.AddRange(EdgeExtractor.FromInheritance(sym, node, projectName, loadedProjectNames));

            if (IsDiFile(filePath))
                diRegs.AddRange(DiExtractor.Extract(cls, model, projectName));

            kafkaP.AddRange(KafkaExtractor.ExtractProducers(cls, sym, model, projectName));
            var consumer = KafkaExtractor.ExtractConsumer(cls, sym, model, projectName);
            if (consumer != null) kafkaC.Add(consumer);
        }

        foreach (var iface in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(iface) is not INamedTypeSymbol sym) continue;
            var node = TypeNodeFactory.FromInterface(sym, layer, projectName);
            if (node != null) types.Add(node);
        }

        foreach (var rec in root.DescendantNodes().OfType<RecordDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(rec) is not INamedTypeSymbol sym) continue;
            var node = TypeNodeFactory.FromRecord(sym, layer, projectName);
            if (node != null) types.Add(node);
        }

        foreach (var e in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(e) is not INamedTypeSymbol sym) continue;
            types.Add(TypeNodeFactory.FromEnum(sym, layer, projectName));
        }

        return new FileExtractionResult(types, edges, diRegs, kafkaP, kafkaC);
    }

    private static bool IsDiFile(string path) =>
        path.EndsWith("Program.cs",        StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("Startup.cs",        StringComparison.OrdinalIgnoreCase) ||
        path.Contains("Extensions",        StringComparison.OrdinalIgnoreCase) ||
        path.Contains("ServiceCollection", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("DependencyInjection", StringComparison.OrdinalIgnoreCase);
}

internal record FileExtractionResult(
    List<TypeNode>         Types,
    List<DependencyEdge>   Edges,
    List<DiRegistration>   DiRegistrations,
    List<KafkaProduction>  KafkaProducers,
    List<KafkaConsumption> KafkaConsumers);

