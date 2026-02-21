using Faculty.Domain.Enums;
using Faculty.Domain.Exceptions;
using FacultyEntity = Faculty.Domain.Entities.Faculty;
using FluentAssertions;

namespace Faculty.Tests.Domain;

public sealed class FacultyTests
{
    private static FacultyEntity ValidFaculty() => FacultyEntity.Create(
        tenantId: Guid.NewGuid(),
        userId: Guid.NewGuid(),
        departmentId: Guid.NewGuid(),
        employeeId: "EMP001",
        firstName: "John",
        lastName: "Doe",
        email: "john.doe@uni.edu",
        designation: Designation.AssistantProfessor,
        specialization: "Computer Science",
        highestQualification: "PhD",
        experienceYears: 5,
        isPhD: true,
        joiningDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
    );

    [Fact]
    public void Create_ValidInput_ReturnsFaculty()
    {
        var f = ValidFaculty();
        f.Should().NotBeNull();
        f.EmployeeId.Should().Be("EMP001");
    }

    [Fact]
    public void Create_EmptyEmployeeId_ThrowsFacultyDomainException()
    {
        var act = () => FacultyEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "", "John", "Doe", "j@uni.edu",
            Designation.AssistantProfessor, "CS", "PhD", 5, true,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        act.Should().Throw<FacultyDomainException>()
           .Which.Code.Should().Be("INVALID_EMPLOYEE_ID");
    }

    [Fact]
    public void Create_EmptyFirstName_ThrowsFacultyDomainException()
    {
        var act = () => FacultyEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "EMP002", "", "Doe", "j@uni.edu",
            Designation.AssistantProfessor, "CS", "PhD", 5, true,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        act.Should().Throw<FacultyDomainException>()
           .Which.Code.Should().Be("INVALID_NAME");
    }

    [Fact]
    public void Create_NegativeExperience_ThrowsFacultyDomainException()
    {
        var act = () => FacultyEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "EMP003", "John", "Doe", "j@uni.edu",
            Designation.AssistantProfessor, "CS", "PhD", -1, true,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        act.Should().Throw<FacultyDomainException>()
           .Which.Code.Should().Be("INVALID_EXPERIENCE");
    }

    [Fact]
    public void Create_FutureJoiningDate_ThrowsFacultyDomainException()
    {
        var act = () => FacultyEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "EMP004", "John", "Doe", "j@uni.edu",
            Designation.AssistantProfessor, "CS", "PhD", 5, true,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

        act.Should().Throw<FacultyDomainException>()
           .Which.Code.Should().Be("INVALID_JOINING_DATE");
    }

    [Fact]
    public void Create_RaisesFacultyCreatedEvent()
    {
        var f = ValidFaculty();
        f.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "FacultyCreatedEvent");
    }

    [Fact]
    public void SetStatus_SameStatus_ThrowsFacultyDomainException()
    {
        var f = ValidFaculty();
        var act = () => f.SetStatus(f.Status);
        act.Should().Throw<FacultyDomainException>()
           .Which.Code.Should().Be("SAME_STATUS");
    }

    [Fact]
    public void SetStatus_NewStatus_RaisesFacultyStatusChangedEvent()
    {
        var f = ValidFaculty();
        var newStatus = f.Status == FacultyStatus.Active ? FacultyStatus.OnLeave : FacultyStatus.Active;
        f.SetStatus(newStatus);
        f.DomainEvents.Should().Contain(e => e.GetType().Name == "FacultyStatusChangedEvent");
    }

    [Fact]
    public void UpdateExperience_Negative_ThrowsFacultyDomainException()
    {
        var f = ValidFaculty();
        var act = () => f.UpdateExperience(-1);
        act.Should().Throw<FacultyDomainException>()
           .Which.Code.Should().Be("INVALID_EXPERIENCE");
    }
}
