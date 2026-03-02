namespace Aegis.Core.Building;

using Aegis.Core.Model;
using Microsoft.CodeAnalysis;

internal static class TypeNodeFactory
{
    public static TypeNode? FromClass(INamedTypeSymbol sym, ArchitectureLayer layer, string project)
    {
        var kind = ResolveKind(sym);
        var bases = sym.BaseType != null
            ? new[] { sym.BaseType.ToDisplayString() }.Concat(sym.Interfaces.Select(i => i.ToDisplayString())).ToArray()
            : sym.Interfaces.Select(i => i.ToDisplayString()).ToArray();

        var rawAttributes = sym.GetAttributes().Select(a => a.AttributeClass?.Name ?? "").ToList();
        var isEventType   = kind is NodeKind.DomainEvent or NodeKind.IntegrationEvent
            || sym.AllInterfaces.Any(i => i.Name is "IDomainEvent" or "IIntegrationEvent" or "ITenantEvent");

        if (HasStaticMutableState(sym)) rawAttributes.Add("HasStaticState");

        return new TypeNode
        {
            FullName      = sym.ToDisplayString(),
            ShortName     = sym.Name,
            Namespace     = sym.ContainingNamespace.ToDisplayString(),
            ProjectName   = project,
            Layer         = layer,
            Kind          = kind,
            IsAbstract    = sym.IsAbstract,
            IsGeneric     = sym.IsGenericType,
            BaseTypes     = bases.Where(b => !b.StartsWith("System.Object")).ToList(),
            Interfaces    = sym.Interfaces.Select(i => i.ToDisplayString()).ToList(),
            Methods       = isEventType ? ExtractEventProperties(sym) : ExtractMethods(sym),
            Attributes    = rawAttributes,
            RouteTemplate = sym.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "RouteAttribute")
                ?.ConstructorArguments.FirstOrDefault().Value?.ToString(),
            RequiresAuth  = sym.GetAttributes().Any(a => a.AttributeClass?.Name == "AuthorizeAttribute"),
            Endpoints     = ExtractEndpoints(sym),
            DbSets        = sym.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.Type.Name == "DbSet").Select(p => p.Name).ToList(),
            RequestType   = ResolveHandlerTypeArg(sym, 0),
            ResponseType  = ResolveHandlerTypeArg(sym, 1),
        };
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
            Methods     = isDomainEvent ? ExtractEventProperties(sym) : ExtractMethods(sym),
        };
    }

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

    private static IReadOnlyList<MethodContract> ExtractEventProperties(INamedTypeSymbol sym) =>
        sym.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
            .Select(p => new MethodContract
            {
                Name       = p.Name,
                ReturnType = p.Type.ToDisplayString(),
                IsPublic   = true,
                IsAsync    = false,
                Parameters = [],
            }).ToList();

    private static IReadOnlyList<EndpointContract> ExtractEndpoints(INamedTypeSymbol sym) =>
        sym.GetMembers().OfType<IMethodSymbol>()
            .SelectMany(m => m.GetAttributes()
                .Where(a => a.AttributeClass?.Name is
                    "HttpGetAttribute" or "HttpPostAttribute" or "HttpPutAttribute" or
                    "HttpDeleteAttribute" or "HttpPatchAttribute")
                .Select(a => new EndpointContract
                {
                    MethodName    = m.Name,
                    HttpVerb      = a.AttributeClass!.Name.Replace("Attribute","").Replace("Http",""),
                    RouteTemplate = a.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "",
                }))
            .ToList();

    private static string? ResolveHandlerTypeArg(INamedTypeSymbol sym, int index)
    {
        var iface = sym.AllInterfaces
            .FirstOrDefault(i => i.Name is "IRequestHandler" or "ICommandHandler" or "IQueryHandler");
        return iface?.TypeArguments.ElementAtOrDefault(index)?.Name;
    }

    private static bool HasStaticMutableState(INamedTypeSymbol sym)
    {
        foreach (var member in sym.GetMembers())
        {
            if (member is IFieldSymbol f && f.IsStatic && !f.IsConst && !f.IsReadOnly) return true;
            if (member is IPropertySymbol p && p.IsStatic && p.SetMethod != null) return true;
            if (member is IFieldSymbol rf && rf.IsStatic && rf.IsReadOnly)
            {
                if (rf.Type.Name is "List" or "Dictionary" or "HashSet" or "ConcurrentDictionary"
                    or "ConcurrentBag" or "ConcurrentQueue" or "Queue" or "Stack") return true;
            }
        }
        return false;
    }
}