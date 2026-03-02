namespace Aegis.Core.Snapshot;

using Aegis.Core.Model;

public sealed class PersistedSnapshot
{
    public required string               FormatVersion { get; init; }
    public required DateTime             CapturedAt    { get; init; }
    public          string?              ContentHash   { get; init; }
    public          List<ServiceSnapshot> Services     { get; init; } = [];
}

public sealed class ServiceSnapshot
{
    public required string                     Name            { get; init; }
    public          string?                    ServiceHash     { get; init; }
    public          List<PersistedTypeSnapshot> Types          { get; init; } = [];
    public          List<string>               KafkaProducers  { get; init; } = [];
    public          List<string>               KafkaConsumers  { get; init; } = [];
    public          List<string>               DiRegistrations { get; init; } = [];

    public static ServiceSnapshot From(ServiceModel s)
    {
        var types = s.Types.Select(t => new PersistedTypeSnapshot
        {
            FullName   = t.FullName,
            Layer      = t.Layer.ToString(),
            Kind       = t.Kind.ToString(),
            Methods    = t.Methods.Select(m => $"{m.Name}:{m.ReturnType}").ToList(),
            Interfaces = t.Interfaces.ToList(),
            BaseTypes  = t.BaseTypes.ToList(),
            Attributes = t.Attributes.ToList(),
            Endpoints  = t.Endpoints.Select(e => $"{e.HttpVerb}:{e.RouteTemplate}").ToList(),
        }).ToList();

        var producers  = s.KafkaProducers.Select(k => $"{k.ProducerClass}:{k.EventFullName}").ToList();
        var consumers  = s.KafkaConsumers.Select(k => $"{k.ConsumerClass}:{string.Join(",", k.EventTypes)}").ToList();
        var diRegs     = s.DiRegistrations.Select(d => $"{d.Lifetime}:{d.ServiceType}->{d.ImplementationType}").ToList();

        var snap = new ServiceSnapshot
        {
            Name            = s.ProjectName,
            Types           = types,
            KafkaProducers  = producers,
            KafkaConsumers  = consumers,
            DiRegistrations = diRegs,
        };

        return new ServiceSnapshot
        {
            Name            = snap.Name,
            ServiceHash     = SnapshotStore.ComputeServiceHash(snap),
            Types           = snap.Types,
            KafkaProducers  = snap.KafkaProducers,
            KafkaConsumers  = snap.KafkaConsumers,
            DiRegistrations = snap.DiRegistrations,
        };
    }
}

public sealed class PersistedTypeSnapshot
{
    public required string      FullName   { get; init; }
    public required string      Layer      { get; init; }
    public required string      Kind       { get; init; }
    public          List<string> Methods   { get; init; } = [];
    public          List<string> Interfaces { get; init; } = [];
    public          List<string> BaseTypes  { get; init; } = [];
    public          List<string> Attributes { get; init; } = [];
    public          List<string> Endpoints  { get; init; } = [];
}