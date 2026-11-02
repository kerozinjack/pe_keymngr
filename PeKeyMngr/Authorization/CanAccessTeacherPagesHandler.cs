using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PeKeyMngr.Data;

namespace PeKeyMngr.Authorization
{
    public class CanAccessTeacherPagesHandler: AuthorizationHandler<CanAccessTeacherPages>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CanAccessTeacherPagesHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessTeacherPages requirement)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(context.User);

            if (user != null && user.Roles.Select(r => r.RoleName).Any(r => r == "teacher"))
            {
                context.Succeed(requirement);
            }
        }
    }
}