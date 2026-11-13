using Users.Domain.Entities;
using Users.Domain.Exceptions;

namespace Users.Tests;

public class UserDomainTests
{
    [Fact]
    public void Create_TrimsLogin()
    {
        var user = User.Create("  alice  ", "hash", UserRole.User);

        Assert.Equal("alice", user.Login);
        Assert.Equal(UserRole.User, user.Role);
    }

    [Fact]
    public void Create_EmptyLogin_Throws()
    {
        Assert.Throws<DomainValidationException>(() =>
            User.Create("  ", "hash", UserRole.Admin));
    }

    [Fact]
    public void Create_EmptyHash_Throws()
    {
        Assert.Throws<DomainValidationException>(() =>
            User.Create("bob", "", UserRole.User));
    }
}
