using Faculty.Application.Faculty.Commands;
using Faculty.Application.Interfaces;
using Faculty.Domain.Enums;
using Faculty.Domain.Exceptions;
using FacultyEntity = Faculty.Domain.Entities.Faculty;
using FluentAssertions;
using Moq;

namespace Faculty.Tests.Application;

public sealed class CreateFacultyHandlerTests
{
    private readonly Mock<IFacultyRepository> _repo = new();

    private CreateFacultyCommandHandler BuildHandler() => new(_repo.Object);

    private static CreateFacultyCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        DepartmentId: Guid.NewGuid(),
        EmployeeId: "EMP001",
        FirstName: "John",
        LastName: "Doe",
        Email: "john@uni.edu",
        Designation: "AssistantProfessor",
        Specialization: "CS",
        HighestQualification: "PhD",
        ExperienceYears: 5,
        IsPhD: true,
        JoiningDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
    );

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuid()
    {
        _repo.Setup(r => r.GetByEmployeeIdAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((FacultyEntity?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<FacultyEntity>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_DuplicateEmployeeId_ThrowsFacultyDomainException()
    {
        var existing = FacultyEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "EMP001", "Jane", "Smith", "jane@uni.edu",
            Designation.AssistantProfessor, "CS", "PhD", 3, false,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)));

        _repo.Setup(r => r.GetByEmployeeIdAsync("EMP001", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(existing);

        var act = async () => await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<FacultyDomainException>();
    }

    [Fact]
    public async Task Handle_InvalidDesignation_ThrowsException()
    {
        _repo.Setup(r => r.GetByEmployeeIdAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((FacultyEntity?)null);

        var cmd = ValidCommand() with { Designation = "NotARealDesignation" };

        var act = async () => await BuildHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
