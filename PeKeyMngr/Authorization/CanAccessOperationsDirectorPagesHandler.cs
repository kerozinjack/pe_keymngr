using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PeKeyMngr.Data;

namespace PeKeyMngr.Authorization
{
    public class CanAccessOperationsDirectorPagesHandler: AuthorizationHandler<CanAccessOperationsDirectorPages>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CanAccessOperationsDirectorPagesHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessOperationsDirectorPages requirement)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(context.User);

            if (user != null && user.Roles.Select(r => r.RoleName).Any(r => r == "Operations Director"))
            {
                context.Succeed(requirement);
            }
        }
    }
}