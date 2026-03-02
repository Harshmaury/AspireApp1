namespace Aegis.Core.Model;

public sealed class ArchitectureModel
{
    public required string                        FormatVersion     { get; init; }
    public required DateTime                      CapturedAt        { get; init; }
    public required List<ServiceModel>            Services          { get; init; }
    public required IReadOnlyList<DependencyEdge> CrossServiceEdges { get; init; }
}

public sealed class ServiceModel
{
    public required string              ProjectName     { get; init; }
    public required string              ProjectPath     { get; init; }
    public          List<TypeNode>      Types           { get; init; } = [];
    public          List<DependencyEdge> Edges          { get; init; } = [];
    public          List<DiRegistration> DiRegistrations { get; init; } = [];
    public          List<KafkaProduction> KafkaProducers { get; init; } = [];
    public          List<KafkaConsumption> KafkaConsumers { get; init; } = [];
}

public sealed class TypeNode
{
    public required string           FullName      { get; init; }
    public required string           ShortName     { get; init; }
    public required string           Namespace     { get; init; }
    public required string           ProjectName   { get; init; }
    public required ArchitectureLayer Layer        { get; init; }
    public required NodeKind         Kind          { get; init; }
    public          bool             IsAbstract    { get; init; }
    public          bool             IsGeneric     { get; init; }
    public          List<string>     BaseTypes     { get; init; } = [];
    public          List<string>     Interfaces    { get; init; } = [];
    public          IReadOnlyList<MethodContract>   Methods    { get; init; } = [];
    public          IReadOnlyList<EndpointContract> Endpoints  { get; init; } = [];
    public          List<string>     Attributes    { get; init; } = [];
    public          string?          RouteTemplate { get; init; }
    public          bool             RequiresAuth  { get; init; }
    public          List<string>     DbSets        { get; init; } = [];
    public          string?          DbProvider    { get; init; }
    public          string?          RequestType   { get; init; }
    public          string?          ResponseType  { get; init; }
}

public sealed class DependencyEdge
{
    public required TypeNode         From          { get; init; }
    public required string           ToFullName    { get; init; }
    public required string           ToNamespace   { get; init; }
    public required EdgeKind         Kind          { get; init; }
    public          string?          ToProjectName { get; init; }
    public          ArchitectureLayer ToLayer      { get; init; }
}

public sealed class DiRegistration
{
    public required string      ServiceType        { get; init; }
    public required string      ImplementationType { get; init; }
    public required DiLifetime  Lifetime           { get; init; }
    public required string      ProjectName        { get; init; }
    public          bool        HasResiliencePolicy { get; init; }
}

public sealed class KafkaProduction
{
    public required string ProducerClass { get; init; }
    public required string EventTypeName { get; init; }
    public required string EventFullName { get; init; }
    public required string ProjectName   { get; init; }
}

public sealed class KafkaConsumption
{
    public required string       ConsumerClass   { get; init; }
    public required string       ProjectName     { get; init; }
    public          List<string> EventTypes      { get; init; } = [];
    public          string?      ConsumerGroupId { get; init; }
}

public sealed class MethodContract
{
    public required string                    Name       { get; init; }
    public required string                    ReturnType { get; init; }
    public          bool                      IsPublic   { get; init; }
    public          bool                      IsAsync    { get; init; }
    public          List<ParameterContract>   Parameters { get; init; } = [];
}

public sealed class ParameterContract
{
    public required string Name         { get; init; }
    public required string TypeName     { get; init; }
    public required string TypeFullName { get; init; }
}

public sealed class EndpointContract
{
    public required string MethodName    { get; init; }
    public required string HttpVerb      { get; init; }
    public required string RouteTemplate { get; init; }
}

public enum ArchitectureLayer
{
    Unknown, Domain, Application, Infrastructure, API, SharedKernel
}

public enum NodeKind
{
    Class, Interface, Record, Enum,
    Controller, DbContext, MediatRHandler,
    DomainEvent, IntegrationEvent
}

public enum EdgeKind
{
    ConstructorInjection, Inheritance, InterfaceImplementation
}

public enum DiLifetime
{
    Scoped, Transient, Singleton
}