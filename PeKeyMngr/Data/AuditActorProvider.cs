using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace PeKeyMngr.Data
{
    ///auditactorprovider: ki vegezte a muveletet 
    public sealed class AuditActorProvider(
        AuthenticationStateProvider authenticationStateProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        public async Task<string> GetActorAsync()
        {
            try
            {
                AuthenticationState state =
                    await authenticationStateProvider.GetAuthenticationStateAsync();

                if (state.User.Identity?.IsAuthenticated == true)
                {
                    return FormatActor(state.User);
                }
            }
            catch (InvalidOperationException)
            {
                
            }

            return GetActorFromRequest();
        }

        public string GetActorFromRequest()
        {
            return FormatActor(httpContextAccessor.HttpContext?.User);
        }

        public static string FormatActor(ClaimsPrincipal? principal)
        {
            // Ha nincs bejelentkezett felhasználó (pl. Seeder), "rendszer" kerül be.
            return principal?.Identity?.IsAuthenticated == true
                ? principal.Identity.Name ?? "ismeretlen"
                : "rendszer";
        }
    }
}
