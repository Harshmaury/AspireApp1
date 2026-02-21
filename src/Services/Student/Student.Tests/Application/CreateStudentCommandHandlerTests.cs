using Student.Application.Features.Students.Commands;
using Student.Application.Interfaces;
using Student.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Student.Tests.Application;

public sealed class CreateStudentCommandHandlerTests
{
    private readonly Mock<IStudentRepository> _repo = new();

    private CreateStudentCommandHandler BuildHandler() => new(_repo.Object);

    private static CreateStudentCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        FirstName: "John",
        LastName: "Doe",
        Email: "john@uni.edu"
    );

    [Fact]
    public async Task Handle_ValidCommand_ReturnsResult()
    {
        _repo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<StudentAggregate>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        result.StudentId.Should().NotBeEmpty();
        result.StudentNumber.Should().StartWith("STU-");
        result.Status.Should().Be("Applicant");
    }

    [Fact]
    public async Task Handle_DuplicateUser_ThrowsInvalidOperationException()
    {
        _repo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        var act = async () => await BuildHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already exists*");
    }
}
