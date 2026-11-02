using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PeKeyMngr.Data;

namespace PeKeyMngr.Authorization
{
    public class CanAccessAdminPagesHandler: AuthorizationHandler<CanAccessAdminPages>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CanAccessAdminPagesHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessAdminPages requirement)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(context.User);

            if (user != null && user.Roles.Select(r => r.RoleName).Any(r => r == "admin"))
            {
                context.Succeed(requirement);
            }
        }
    }
}