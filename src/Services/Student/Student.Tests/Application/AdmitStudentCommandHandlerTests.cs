using Student.Application.Features.Students.Commands;
using Student.Application.Interfaces;
using Student.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Student.Tests.Application;

public sealed class AdmitStudentCommandHandlerTests
{
    private readonly Mock<IStudentRepository> _repo = new();

    private AdmitStudentCommandHandler BuildHandler() => new(_repo.Object);

    [Fact]
    public async Task Handle_ValidStudent_ReturnsTrue()
    {
        var student = StudentAggregate.Create(Guid.NewGuid(), Guid.NewGuid(), "John", "Doe", "j@uni.edu");
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(student);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<StudentAggregate>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(new AdmitStudentCommand(student.Id, student.TenantId), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StudentNotFound_ThrowsInvalidOperationException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((StudentAggregate?)null);

        var act = async () => await BuildHandler().Handle(
            new AdmitStudentCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*not found*");
    }
}
