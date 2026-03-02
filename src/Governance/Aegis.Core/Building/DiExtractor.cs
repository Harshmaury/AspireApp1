namespace Aegis.Core.Building;

using Aegis.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        var resilienceDecorated = CollectResilienceDecoratedClients(cls, model);

        foreach (var inv in cls.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var sym = model.GetSymbolInfo(inv).Symbol as IMethodSymbol;
            if (sym == null) continue;

            if (_httpClientMethods.Contains(sym.Name))
            {
                foreach (var reg in ExtractHttpClientRegistration(inv, sym, model, project, resilienceDecorated))
                    yield return reg;
                continue;
            }

            if (!_lifetimeMethods.Contains(sym.Name)) continue;

            var lifetime = sym.Name switch
            {
                "AddScoped"    => DiLifetime.Scoped,
                "AddTransient" => DiLifetime.Transient,
                _              => DiLifetime.Singleton,
            };

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

            var args = inv.ArgumentList.Arguments;
            if (args.Count == 2
                && args[0].Expression is TypeOfExpressionSyntax svcTypeOf
                && args[1].Expression is TypeOfExpressionSyntax implTypeOf)
            {
                var svcSym  = model.GetTypeInfo(svcTypeOf.Type).Type;
                var implSym = model.GetTypeInfo(implTypeOf.Type).Type;
                if (svcSym != null && implSym != null)
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

    private static IEnumerable<DiRegistration> ExtractHttpClientRegistration(
        InvocationExpressionSyntax inv, IMethodSymbol sym,
        SemanticModel model, string project, HashSet<string> resilienceDecorated)
    {
        if (sym.TypeArguments.Length == 2)
        {
            var svcType  = sym.TypeArguments[0].ToDisplayString();
            var implType = sym.TypeArguments[1].ToDisplayString();
            yield return new DiRegistration
            {
                ServiceType         = svcType,
                ImplementationType  = implType,
                Lifetime            = DiLifetime.Transient,
                ProjectName         = project,
                HasResiliencePolicy = resilienceDecorated.Contains(svcType) || resilienceDecorated.Contains(implType),
            };
            yield break;
        }

        if (sym.TypeArguments.Length == 1)
        {
            var typeName = sym.TypeArguments[0].ToDisplayString();
            yield return new DiRegistration
            {
                ServiceType         = typeName,
                ImplementationType  = typeName,
                Lifetime            = DiLifetime.Transient,
                ProjectName         = project,
                HasResiliencePolicy = resilienceDecorated.Contains(typeName),
            };
            yield break;
        }

        var firstArg = inv.ArgumentList.Arguments.FirstOrDefault();
        if (firstArg != null)
        {
            var clientName = model.GetConstantValue(firstArg.Expression).Value?.ToString() ?? "HttpClient";
            yield return new DiRegistration
            {
                ServiceType         = $"IHttpClientFactory[{clientName}]",
                ImplementationType  = $"HttpClient[{clientName}]",
                Lifetime            = DiLifetime.Transient,
                ProjectName         = project,
                HasResiliencePolicy = resilienceDecorated.Contains(clientName),
            };
        }
    }

    private static HashSet<string> CollectResilienceDecoratedClients(
        ClassDeclarationSyntax cls, SemanticModel model)
    {
        var decorated = new HashSet<string>(StringComparer.Ordinal);

        foreach (var inv in cls.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var sym = model.GetSymbolInfo(inv).Symbol as IMethodSymbol;
            if (sym == null || !_resilienceMethods.Contains(sym.Name)) continue;

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
                        {
                            decorated.Add(typeArg.ToDisplayString());
                            decorated.Add(typeArg.Name);
                        }
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