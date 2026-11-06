using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Data;

namespace PeKeyMngr.Authorization
{
    public class CanAccessTeacherPagesHandler : AuthorizationHandler<CanAccessTeacherPages>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CanAccessTeacherPagesHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessTeacherPages requirement)
        {
            string? userId = _userManager.GetUserId(context.User);

            ApplicationUser? user = _userManager.Users.Include(u => u.Roles).FirstOrDefault(u => u.Id == userId);

            if (user != null && user.Roles.Select(r => r.RoleName).Any(r => r == "teacher"))
            {
                context.Succeed(requirement);
            }
        }
    }
}