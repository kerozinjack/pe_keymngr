using Microsoft.AspNetCore.Identity;
using PeKeyMngr.Models;

namespace PeKeyMngr.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public List<Role> Roles { get; set; } = [];
        public int? AuthorizationCode { get; set; }
    }

}
