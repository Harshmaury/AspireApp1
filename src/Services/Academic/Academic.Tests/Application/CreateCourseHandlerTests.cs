using Xunit;
using Academic.Application.Course.Commands;
using Academic.Application.Interfaces;
using Academic.Domain.Entities;
using Moq;
using FluentAssertions;
namespace Academic.Tests.Application;
public sealed class CreateCourseHandlerTests
{
    private readonly Mock<ICourseRepository> _repoMock = new();
    private readonly Mock<IDepartmentRepository> _deptRepoMock = new();

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCourseDto()
    {
        _repoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _deptRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Department.Create(Guid.NewGuid(), "Computer Science", "CSE", 1972));
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateCourseCommandHandler(_repoMock.Object, _deptRepoMock.Object);
        var cmd = new CreateCourseCommand(Guid.NewGuid(), Guid.NewGuid(), "Data Structures", "CS301", 3, "Core", 3, 1, 0, 60, null);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsException()
    {
        _repoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateCourseCommandHandler(_repoMock.Object, _deptRepoMock.Object);
        var cmd = new CreateCourseCommand(Guid.NewGuid(), Guid.NewGuid(), "Data Structures", "CS301", 3, "Core", 3, 1, 0, 60, null);
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}