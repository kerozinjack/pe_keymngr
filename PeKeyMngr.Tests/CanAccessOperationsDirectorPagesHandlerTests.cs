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
    public class CanAccessOperationsDirectorPagesHandlerTests
    {
        private readonly Mock<IUserStore<ApplicationUser>> _userStore;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CanAccessOperationsDirectorPagesHandler _handler;

        public CanAccessOperationsDirectorPagesHandlerTests()
        {
            _userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManager = new UserManager<ApplicationUser>(
                _userStore.Object,
                Options.Create(new IdentityOptions()),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>());
            
            _handler = new CanAccessOperationsDirectorPagesHandler(_userManager);
        }

        [Fact]
        public async Task UserWithPermission_ShouldBeAuthorized()
        {
            ApplicationUser user = new ApplicationUser { Id = "123", EmailConfirmed = true, Roles = new() };
            user.Roles.Add(new Role{RoleId = Guid.NewGuid(), RoleName = "Operations Director"});
            _userStore.Setup(x => x.FindByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(user);

            AuthorizationHandlerContext context = CreateAuthorizationContext("123");
            await _handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task UserWithoutPermission_ShouldBeNotAuthorized()
        {
            ApplicationUser user = new ApplicationUser { Id = "123", EmailConfirmed = true, Roles = new() };
            _userStore.Setup(x => x.FindByIdAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(user);

            AuthorizationHandlerContext context = CreateAuthorizationContext("123");
            await _handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }

        private AuthorizationHandlerContext CreateAuthorizationContext(string id)
        {
            Claim[] claims = new[] { new Claim(ClaimTypes.NameIdentifier, id) };
            ClaimsIdentity identity = new ClaimsIdentity(claims, "Test");
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

            return new AuthorizationHandlerContext(
                new[] { new CanAccessOperationsDirectorPages() },
                principal,
                null
            );
        }
    }
}