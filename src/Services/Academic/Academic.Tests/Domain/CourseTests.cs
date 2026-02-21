using Xunit;
using Academic.Domain.Entities;
using Academic.Domain.Enums;
using Academic.Domain.Exceptions;
using FluentAssertions;
namespace Academic.Tests.Domain;
public sealed class CourseTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid DeptId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsCourse()
    {
        var course = Course.Create(TenantId, DeptId, "Data Structures", "CS301", 3, "Core", 3, 1, 0, 60);
        course.Should().NotBeNull();
        course.Code.Should().Be("CS301");
        course.Status.Should().Be(CourseStatus.Draft);
    }

    [Fact]
    public void Create_InvalidCredits_ThrowsException()
    {
        var act = () => Course.Create(TenantId, DeptId, "Data Structures", "CS301", 0, "Core", 3, 1, 0, 60);
        act.Should().Throw<AcademicDomainException>();
    }

    [Fact]
    public void Publish_DraftCourse_SetsPublished()
    {
        var course = Course.Create(TenantId, DeptId, "Data Structures", "CS301", 3, "Core", 3, 1, 0, 60);
        course.Publish();
        course.Status.Should().Be(CourseStatus.Published);
    }

    [Fact]
    public void Publish_RetiredCourse_ThrowsException()
    {
        var course = Course.Create(TenantId, DeptId, "Data Structures", "CS301", 3, "Core", 3, 1, 0, 60);
        course.Publish();
        course.Retire();
        var act = () => course.Publish();
        act.Should().Throw<AcademicDomainException>();
    }

    [Fact]
    public void Publish_RaisesCoursePublishedEvent()
    {
        var course = Course.Create(TenantId, DeptId, "Data Structures", "CS301", 3, "Core", 3, 1, 0, 60);
        course.Publish();
        course.DomainEvents.Should().Contain(e => e.GetType().Name == "CoursePublishedEvent");
    }
}