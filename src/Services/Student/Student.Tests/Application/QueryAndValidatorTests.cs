using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using Moq;
using Student.Application.Features.Students.Commands;
using Student.Application.Features.Students.Queries;
using Student.Application.Interfaces;
using Student.Application.Validators;
using Student.Domain.Entities;

namespace Student.Tests.Application;

public sealed class GetStudentByIdQueryHandlerTests
{
    private readonly Mock<IStudentRepository> _repo = new();

    [Fact]
    public async Task Handle_ExistingStudent_ReturnsFullDto()
    {
        var student = StudentAggregate.Create(Guid.NewGuid(), Guid.NewGuid(), "John", "Doe", "j@uni.edu");

        // GetByIdReadOnlyAsync: query handlers use the untracked read path.
        _repo.Setup(r => r.GetByIdReadOnlyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(student);

        var result = await new GetStudentByIdQueryHandler(_repo.Object)
            .Handle(new GetStudentByIdQuery(student.Id, student.TenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("j@uni.edu");
        result.Status.Should().Be("Applicant");
        result.StudentNumber.Should().StartWith("STU-");
        result.AdmittedAt.Should().BeNull();
        result.SuspensionReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MissingStudent_ReturnsNull()
    {
        // GetByIdReadOnlyAsync: query handlers use the untracked read path.
        _repo.Setup(r => r.GetByIdReadOnlyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((StudentAggregate?)null);

        var result = await new GetStudentByIdQueryHandler(_repo.Object)
            .Handle(new GetStudentByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}

public sealed class GetAllStudentsQueryHandlerTests
{
    private readonly Mock<IStudentRepository> _repo = new();

    [Fact]
    public async Task Handle_NoFilter_ReturnsPaginatedResult()
    {
        var tenantId = Guid.NewGuid();
        var students = Enumerable.Range(1, 3)
            .Select(i => StudentAggregate.Create(tenantId, Guid.NewGuid(), $"First{i}", $"Last{i}", $"s{i}@uni.edu"))
            .ToList();

        _repo.Setup(r => r.GetAllAsync(tenantId, null, 1, 20, It.IsAny<CancellationToken>()))
             .ReturnsAsync((students, 3));

        var result = await new GetAllStudentsQueryHandler(_repo.Object)
            .Handle(new GetAllStudentsQuery(tenantId), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(1);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_PageSizeClamped_MaxIs100()
    {
        var tenantId = Guid.NewGuid();
        _repo.Setup(r => r.GetAllAsync(tenantId, null, 1, 100, It.IsAny<CancellationToken>()))
             .ReturnsAsync((new List<StudentAggregate>(), 0));

        var act = async () => await new GetAllStudentsQueryHandler(_repo.Object)
            .Handle(new GetAllStudentsQuery(tenantId, null, 1, 999), CancellationToken.None);

        await act.Should().NotThrowAsync();
        _repo.Verify(r => r.GetAllAsync(tenantId, null, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TotalPagesCalculatedCorrectly()
    {
        var tenantId = Guid.NewGuid();
        _repo.Setup(r => r.GetAllAsync(tenantId, null, 1, 10, It.IsAny<CancellationToken>()))
             .ReturnsAsync((new List<StudentAggregate>(), 25));

        var result = await new GetAllStudentsQueryHandler(_repo.Object)
            .Handle(new GetAllStudentsQuery(tenantId, null, 1, 10), CancellationToken.None);

        result.TotalPages.Should().Be(3);
        result.TotalCount.Should().Be(25);
    }
}

public sealed class StudentValidatorTests
{
    // CreateStudentCommandValidator
    [Fact]
    public void Create_ValidCommand_PassesValidation()
    {
        var validator = new CreateStudentCommandValidator();
        var cmd = new CreateStudentCommand(Guid.NewGuid(), Guid.NewGuid(), "John", "Doe", "j@uni.edu");
        validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Create_EmptyFirstName_FailsValidation()
    {
        var validator = new CreateStudentCommandValidator();
        var cmd = new CreateStudentCommand(Guid.NewGuid(), Guid.NewGuid(), "", "Doe", "j@uni.edu");
        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Create_InvalidEmail_FailsValidation()
    {
        var validator = new CreateStudentCommandValidator();
        var cmd = new CreateStudentCommand(Guid.NewGuid(), Guid.NewGuid(), "John", "Doe", "not-an-email");
        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Create_EmptyTenantId_FailsValidation()
    {
        var validator = new CreateStudentCommandValidator();
        var cmd = new CreateStudentCommand(Guid.Empty, Guid.NewGuid(), "John", "Doe", "j@uni.edu");
        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    // UpdateStudentCommandValidator
    [Fact]
    public void Update_ValidCommand_PassesValidation()
    {
        var validator = new UpdateStudentCommandValidator();
        var cmd = new UpdateStudentCommand(Guid.NewGuid(), Guid.NewGuid(), "Jane", "Smith", "jane@uni.edu");
        validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Update_InvalidEmail_FailsValidation()
    {
        var validator = new UpdateStudentCommandValidator();
        var cmd = new UpdateStudentCommand(Guid.NewGuid(), Guid.NewGuid(), "Jane", "Smith", "bad-email");
        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    // SuspendStudentCommandValidator
    [Fact]
    public void Suspend_EmptyReason_FailsValidation()
    {
        var validator = new SuspendStudentCommandValidator();
        var cmd = new SuspendStudentCommand(Guid.NewGuid(), Guid.NewGuid(), "");
        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Suspend_ValidReason_PassesValidation()
    {
        var validator = new SuspendStudentCommandValidator();
        var cmd = new SuspendStudentCommand(Guid.NewGuid(), Guid.NewGuid(), "Academic misconduct");
        validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}