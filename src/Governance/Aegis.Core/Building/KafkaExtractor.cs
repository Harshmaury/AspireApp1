namespace Aegis.Core.Building;
using Aegis.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class KafkaExtractor
{
    private static readonly HashSet<string> _producerMethods =
        ["Produce", "ProduceAsync", "PublishAsync", "SendAsync"];

    public static IEnumerable<KafkaProduction> ExtractProducers(
        ClassDeclarationSyntax cls, INamedTypeSymbol sym,
        SemanticModel model, string project)
    {
        foreach (var inv in cls.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var methodSym = model.GetSymbolInfo(inv).Symbol as IMethodSymbol;
            if (methodSym == null || !_producerMethods.Contains(methodSym.Name)) continue;

            var eventType = (inv.Expression as MemberAccessExpressionSyntax)
                is { Name: GenericNameSyntax g } && g.TypeArgumentList.Arguments.Count > 0
                ? model.GetTypeInfo(g.TypeArgumentList.Arguments[0]).Type
                : null;
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
        // Pattern A: BackgroundService / IHostedService subclass
        var isBackgroundService = sym.BaseType?.Name == "BackgroundService"
            || sym.AllInterfaces.Any(i => i.Name is "IHostedService");

        // Pattern B: class directly injecting IConsumer<TKey, TValue> in constructor
        var injectedConsumerTypes = sym.Constructors
            .SelectMany(c => c.Parameters)
            .Where(p =>
            {
                var typeName = p.Type.Name;
                return typeName == "IConsumer" ||
                       (p.Type is INamedTypeSymbol nt &&
                        nt.IsGenericType &&
                        nt.OriginalDefinition.Name == "IConsumer");
            })
            .Select(p => p.Type)
            .OfType<INamedTypeSymbol>()
            .ToList();

        if (!isBackgroundService && injectedConsumerTypes.Count == 0) return null;

        // Event types from ConsumeResult<T> usage (Pattern A)
        var eventTypesFromResult = cls.DescendantNodes()
            .OfType<GenericNameSyntax>()
            .Where(g => g.Identifier.Text == "ConsumeResult")
            .SelectMany(g => g.TypeArgumentList.Arguments)
            .Select(t => model.GetTypeInfo(t).Type?.Name ?? t.ToString())
            .ToList();

        // Event types from IConsumer<Ignore, T> or IConsumer<string, T> type args (Pattern B)
        var eventTypesFromInjection = injectedConsumerTypes
            .SelectMany(t => t.TypeArguments.Skip(1)) // skip key type (Ignore/string)
            .Select(t => t.Name)
            .ToList();

        var allEventTypes = eventTypesFromResult
            .Concat(eventTypesFromInjection)
            .Distinct()
            .ToList();

        return new KafkaConsumption
        {
            ConsumerClass   = sym.Name,
            EventTypes      = allEventTypes,
            ProjectName     = project,
            ConsumerGroupId = ExtractConsumerGroupId(cls, model),
        };
    }

    private static string? ExtractConsumerGroupId(ClassDeclarationSyntax cls, SemanticModel model)
    {
        var objInit = cls.DescendantNodes()
            .OfType<InitializerExpressionSyntax>()
            .SelectMany(i => i.Expressions.OfType<AssignmentExpressionSyntax>())
            .FirstOrDefault(a => a.Left is IdentifierNameSyntax id && id.Identifier.Text == "GroupId");

        if (objInit != null)
        {
            var val = model.GetConstantValue(objInit.Right);
            if (val.HasValue && val.Value is string s) return s;
        }

        var assignExpr = cls.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .FirstOrDefault(a =>
                a.Left is MemberAccessExpressionSyntax m && m.Name.Identifier.Text == "GroupId");

        if (assignExpr != null)
        {
            var val = model.GetConstantValue(assignExpr.Right);
            if (val.HasValue && val.Value is string s) return s;
        }
        return null;
    }
}



