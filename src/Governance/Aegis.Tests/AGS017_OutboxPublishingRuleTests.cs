using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using Aegis.Core.Rules;
using Aegis.Core.Model;

namespace Aegis.Tests;

public class AGS017_OutboxPublishingRuleTests
{
    private static ArchitectureModel BuildModel(List<TypeNode> types, List<KafkaProduction> producers) => new()
    {
        FormatVersion = "2.0",
        CapturedAt    = DateTime.UtcNow,
        Services      =
        [
            new ServiceModel
            {
                ProjectName    = "TestService",
                ProjectPath    = ".",
                Types          = types,
                KafkaProducers = producers,
            }
        ],
        CrossServiceEdges = [],
    };

    private static TypeNode Handler(string name) => new()
    {
        FullName    = $"TestService.Application.{name}",
        ShortName   = name,
        Namespace   = "TestService.Application",
        ProjectName = "TestService",
        Layer       = ArchitectureLayer.Application,
        Kind        = NodeKind.MediatRHandler,
        Interfaces  = [ "IRequestHandler<CreateUserCommand, Unit>" ],
    };

    private static KafkaProduction Production(string producerClass, string eventType) => new()
    {
        ProducerClass = producerClass,
        EventTypeName = eventType,
        EventFullName = $"TestService.Domain.{eventType}",
        ProjectName   = "TestService",
    };

    [Fact]
    public void Violation_when_handler_produces_directly_to_kafka()
    {
        var model = BuildModel(
            [ Handler("CreateUserCommandHandler") ],
            [ Production("CreateUserCommandHandler", "UserCreatedEvent") ]);

        var result = new OutboxPublishingRule().Evaluate(model);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].RuleId.Should().Be("AGS-017");
        result.Violations[0].Severity.Should().Be(RuleSeverity.Error);
        result.Violations[0].Message.Should().Contain("CreateUserCommandHandler");
        result.Violations[0].Message.Should().Contain("UserCreatedEvent");
    }

    [Fact]
    public void No_violation_when_relay_service_produces_to_kafka()
    {
        var model = BuildModel(
            [],
            [ Production("OutboxRelayService", "UserCreatedEvent") ]);

        var result = new OutboxPublishingRule().Evaluate(model);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void No_violation_when_no_kafka_producers()
    {
        var model = BuildModel([ Handler("CreateUserCommandHandler") ], []);
        var result = new OutboxPublishingRule().Evaluate(model);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Violation_detected_by_name_heuristic_when_type_not_in_model()
    {
        // Producer class not in Types list — falls back to name ending in "Handler"
        var model = BuildModel(
            [],
            [ Production("SomeUnknownHandler", "OrderPlacedEvent") ]);

        var result = new OutboxPublishingRule().Evaluate(model);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].Message.Should().Contain("SomeUnknownHandler");
    }
}


