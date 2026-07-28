using FluentAssertions;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure.Services;

namespace LibraryManagement.Tests.Services;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void SetCurrentUser_ShouldCreateAuthenticatedSession()
    {
        var service = new CurrentUserService();
        var user = new CurrentUser(1, "NV001", "Quản trị viên", "admin", "Administrator");

        service.SetCurrentUser(user);

        service.IsAuthenticated.Should().BeTrue();
        service.CurrentUser.Should().Be(user);
        service.IsInRole("administrator").Should().BeTrue();
    }

    [Fact]
    public void Clear_ShouldRemoveSessionAndRaiseEventOnce()
    {
        var service = new CurrentUserService();
        var eventCount = 0;
        service.CurrentUserChanged += (_, _) => eventCount++;
        service.SetCurrentUser(
            new CurrentUser(1, "NV001", "Quản trị viên", "admin", "Administrator"));

        service.Clear();
        service.Clear();

        service.IsAuthenticated.Should().BeFalse();
        service.CurrentUser.Should().BeNull();
        eventCount.Should().Be(2);
    }
}
