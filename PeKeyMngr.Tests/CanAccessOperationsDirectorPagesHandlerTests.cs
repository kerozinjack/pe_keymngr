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
    public class CanAccessOperationsDirectorPagesHandlerTests: IClassFixture<AuthorizationTestFixture>
    {
        private readonly AuthorizationTestFixture _fixture;

        public CanAccessOperationsDirectorPagesHandlerTests(AuthorizationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task UserWithPermission_ShouldBeAuthorized()
        {
            ApplicationUser user = new ApplicationUser { UserName = "directorWithRole", Email = "directorWithRole@example.com", EmailConfirmed = true, Roles = new() };
            user.Roles.Add(new Role{RoleId = Guid.NewGuid(), RoleName = "Operations Director"});
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

            CanAccessOperationsDirectorPages requirement = new CanAccessOperationsDirectorPages();

            AuthorizationHandlerContext context = new AuthorizationHandlerContext(
                new[] { requirement },
                principal,
                null
            );

            CanAccessOperationsDirectorPagesHandler handler = new CanAccessOperationsDirectorPagesHandler(_fixture.UserManager);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task UserWithoutPermission_ShouldBeNotAuthorized()
        {
            ApplicationUser user = new ApplicationUser { UserName = "directorWithoutRole", Email = "directorWithoutRole@example.com", EmailConfirmed = true, Roles = new() };
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

            CanAccessOperationsDirectorPages requirement = new CanAccessOperationsDirectorPages();

            AuthorizationHandlerContext context = new AuthorizationHandlerContext(
                new[] { requirement },
                principal,
                null
            );

            CanAccessOperationsDirectorPagesHandler handler = new CanAccessOperationsDirectorPagesHandler(_fixture.UserManager);

            await handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }
    }
}
