using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Data;

namespace PeKeyMngr.Authorization
{
    public class CanAccessOperationsDirectorPagesHandler : AuthorizationHandler<CanAccessOperationsDirectorPages>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CanAccessOperationsDirectorPagesHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessOperationsDirectorPages requirement)
        {
            string? userId = _userManager.GetUserId(context.User);

            ApplicationUser? user = _userManager.Users.Include(u => u.Roles).FirstOrDefault(u => u.Id == userId);

            if (user != null && user.Roles.Select(r => r.RoleName).Any(r => r == "Operations Director"))
            {
                context.Succeed(requirement);
            }
        }
    }
}