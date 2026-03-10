using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using Aegis.Core.Rules;
using Aegis.Core.Model;

namespace Aegis.Tests;

public class AGS016_CommandValidatorCoverageRuleTests
{
    private static ArchitectureModel BuildModel(List<TypeNode> types) => new()
    {
        FormatVersion = "2.0",
        CapturedAt    = DateTime.UtcNow,
        Services      = [ new ServiceModel { ProjectName = "TestService", ProjectPath = ".", Types = types } ],
        CrossServiceEdges = [],
    };

    private static TypeNode Command(string name) => new()
    {
        FullName    = $"TestService.Application.{name}",
        ShortName   = name,
        Namespace   = "TestService.Application",
        ProjectName = "TestService",
        Layer       = ArchitectureLayer.Application,
        Kind        = NodeKind.Record,
        Interfaces  = [ "IRequest<Unit>" ],
    };

    private static TypeNode Validator(string commandName) => new()
    {
        FullName    = $"TestService.Application.{commandName}Validator",
        ShortName   = $"{commandName}Validator",
        Namespace   = "TestService.Application",
        ProjectName = "TestService",
        Layer       = ArchitectureLayer.Application,
        Kind        = NodeKind.Class,
        BaseTypes   = [ $"AbstractValidator<{commandName}>" ],
    };

    [Fact]
    public void No_violations_when_every_command_has_validator()
    {
        var model = BuildModel([ Command("CreateUserCommand"), Validator("CreateUserCommand") ]);
        var result = new CommandValidatorCoverageRule().Evaluate(model);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Violation_when_command_missing_validator()
    {
        var model = BuildModel([ Command("DeleteUserCommand") ]);
        var result = new CommandValidatorCoverageRule().Evaluate(model);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].RuleId.Should().Be("AGS-016");
        result.Violations[0].Severity.Should().Be(RuleSeverity.Warning);
        result.Violations[0].Message.Should().Contain("DeleteUserCommand");
    }

    [Fact]
    public void No_violations_when_no_commands_exist()
    {
        var model = BuildModel([]);
        var result = new CommandValidatorCoverageRule().Evaluate(model);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Violation_only_for_command_missing_validator_not_the_paired_one()
    {
        var types = new List<TypeNode>
        {
            Command("CreateUserCommand"), Validator("CreateUserCommand"),
            Command("UpdateUserCommand"),  // no validator
        };
        var model = BuildModel(types);
        var result = new CommandValidatorCoverageRule().Evaluate(model);
        result.Violations.Should().HaveCount(1);
        result.Violations[0].Message.Should().Contain("UpdateUserCommand");
    }
}


