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
    public class CanAccessJanitorPagesHandlerTests: IClassFixture<AuthorizationTestFixture>
    {
        private readonly AuthorizationTestFixture _fixture;

        public CanAccessJanitorPagesHandlerTests(AuthorizationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task UserWithPermission_ShouldBeAuthorized()
        {
            ApplicationUser user = new ApplicationUser { UserName = "janitorWithRole", Email = "janitorWithRole@example.com", EmailConfirmed = true, Roles = new() };
            user.Roles.Add(new Role{RoleId = Guid.NewGuid(), RoleName = "janitor"});
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

            CanAccessJanitorPages requirement = new CanAccessJanitorPages();

            AuthorizationHandlerContext context = new AuthorizationHandlerContext(
                new[] { requirement },
                principal,
                null
            );

            CanAccessJanitorPagesHandler handler = new CanAccessJanitorPagesHandler(_fixture.UserManager);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task UserWithoutPermission_ShouldBeNotAuthorized()
        {
            ApplicationUser user = new ApplicationUser { UserName = "janitorWithoutRole", Email = "janitorWithoutRole@example.com", EmailConfirmed = true, Roles = new() };
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

            CanAccessJanitorPages requirement = new CanAccessJanitorPages();

            AuthorizationHandlerContext context = new AuthorizationHandlerContext(
                new[] { requirement },
                principal,
                null
            );

            CanAccessJanitorPagesHandler handler = new CanAccessJanitorPagesHandler(_fixture.UserManager);

            await handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }
    }
}
