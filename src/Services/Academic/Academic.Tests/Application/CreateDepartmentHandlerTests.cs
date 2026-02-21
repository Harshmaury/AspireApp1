using Xunit;
using Academic.Application.Department.Commands;
using Academic.Application.Interfaces;
using Academic.Domain.Entities;
using Moq;
using FluentAssertions;
namespace Academic.Tests.Application;
public sealed class CreateDepartmentHandlerTests
{
    private readonly Mock<IDepartmentRepository> _repoMock = new();

    [Fact]
    public async Task Handle_ValidCommand_ReturnsDepartmentDto()
    {
        _repoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateDepartmentCommandHandler(_repoMock.Object);
        var cmd = new CreateDepartmentCommand(Guid.NewGuid(), "Computer Science", "CSE", 1972, null);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsException()
    {
        _repoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateDepartmentCommandHandler(_repoMock.Object);
        var cmd = new CreateDepartmentCommand(Guid.NewGuid(), "Computer Science", "CSE", 1972, null);
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}