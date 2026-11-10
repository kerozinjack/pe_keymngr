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
    public class CanAccessAdminPagesHandlerTests: IClassFixture<AuthorizationTestFixture>
    {
        private readonly AuthorizationTestFixture _fixture;

        public CanAccessAdminPagesHandlerTests(AuthorizationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task UserWithPermission_ShouldBeAuthorized()
        {
            ApplicationUser user = new ApplicationUser { UserName = "adminWithRole", Email = "adminWithRole@example.com", EmailConfirmed = true, Roles = new() };
            user.Roles.Add(new Role{RoleId = Guid.NewGuid(), RoleName = "admin"});
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

            CanAccessAdminPages requirement = new CanAccessAdminPages();

            AuthorizationHandlerContext context = new AuthorizationHandlerContext(
                new[] { requirement },
                principal,
                null
            );

            CanAccessAdminPagesHandler handler = new CanAccessAdminPagesHandler(_fixture.UserManager);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task UserWithoutPermission_ShouldBeNotAuthorized()
        {
            ApplicationUser user = new ApplicationUser { UserName = "AdminWithoutRole", Email = "adminWithoutRole@example.com", EmailConfirmed = true, Roles = new() };
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

            CanAccessAdminPages requirement = new CanAccessAdminPages();

            AuthorizationHandlerContext context = new AuthorizationHandlerContext(
                new[] { requirement },
                principal,
                null
            );

            CanAccessAdminPagesHandler handler = new CanAccessAdminPagesHandler(_fixture.UserManager);

            await handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }
    }
}
