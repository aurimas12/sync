using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using TableAir.AdminFlow.Model;

namespace TableAir.AdminFlow.Pages
{
    public class LoginModel : PageModel
    {
        public async Task OnGet(string redirectUri)
        {
            System.Console.WriteLine("sfsdf"+ Request.Query["tenant"].ToString() + "sss");
            await HttpContext.ChallengeAsync("oidc", new AuthenticationProperties { RedirectUri = redirectUri ?? $"/{MicrosoftSync.TeamUrl}" });
        }
    }
}