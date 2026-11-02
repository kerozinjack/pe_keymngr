using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PeKeyMngr.Data;

namespace PeKeyMngr.Authorization
{
    public class CanAccessJanitorPagesHandler: AuthorizationHandler<CanAccessJanitorPages>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CanAccessJanitorPagesHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessJanitorPages requirement)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(context.User);

            if (user != null && user.Roles.Select(r => r.RoleName).Any(r => r == "janitor"))
            {
                context.Succeed(requirement);
            }
        }
    }
}