using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PeKeyMngr.Authorization;
using PeKeyMngr.Data;
using PeKeyMngr.Models;
using System.Security.Claims;

namespace PeKeyMngr.Tests
{
    public class CanAccessTeacherPagesHandlerTests: IClassFixture<AuthorizationTestFixture>
    {
        private readonly AuthorizationTestFixture _fixture;

        public CanAccessTeacherPagesHandlerTests(AuthorizationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task UserWithPermission_ShouldBeAuthorized()
        {
            ApplicationUser user = new ApplicationUser { UserName = "teacherWithRole", Email = "teacherWithRole@example.com", EmailConfirmed = true, Roles = new() };
            user.Roles.Add(new Role{RoleId = Guid.NewGuid(), RoleName = "teacher"});
            await _fixture.UserManager.CreateAsync(user, "1Q2w3e4r.");

            ClaimsPrincipal principal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id)
                    },
                    "TestAuthentication"
                )
            );

            CanAccessTeacherPages requirement = new CanAccessTeacherPages();

            AuthorizationHandlerContext context = new AuthorizationHandlerContext(
                new[] { requirement },
                principal,
                null
            );

            CanAccessTeacherPagesHandler handler = new CanAccessTeacherPagesHandler(_fixture.UserManager);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task UserWithoutPermission_ShouldBeNotAuthorized()
        {
            ApplicationUser user = new ApplicationUser { UserName = "teacherWithoutRole", Email = "teacherWithoutRole@example.com", EmailConfirmed = true, Roles = new() };
            await _fixture.UserManager.CreateAsync(user, "1Q2w3e4r.");

            ClaimsPrincipal principal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id)
                    },
                    "TestAuthentication"
                )
            );

            CanAccessTeacherPages requirement = new CanAccessTeacherPages();

            AuthorizationHandlerContext context = new AuthorizationHandlerContext(
                new[] { requirement },
                principal,
                null
            );

            CanAccessTeacherPagesHandler handler = new CanAccessTeacherPagesHandler(_fixture.UserManager);

            await handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }
    }
}
