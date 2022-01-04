using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Threading.Tasks;
using TableAir.AdminFlow.Model;

namespace TableAir.AdminFlow.Pages
{
    public class AdminConsentCallbackModel : PageModel
    {
        public NavigationManager NavigationManager { get; private set; }

        public AdminConsentCallbackModel(NavigationManager _navigationManager)
        {
            NavigationManager = _navigationManager;
        }

        public async Task OnGet()
        {
            var repo = new UsersRepository();
            var team = repo.Teams.Where(t => t.Id == MicrosoftSync.TeamId).FirstOrDefault();
            repo.ExternalLinks.Add(new ExternalLink
            {
                Team = team,
                Provider = 4,
                ExternalId = Request.Query["tenant"].ToString(),
                Properties = "{\"sync_client\": \"a2920437-2703-4699-92a4-f6e5a6429df9\", \"initial_sync_params\": {\"import_users\": true}}", //dev
                // Properties = "{\"sync_client\": \"c2c3392c-9323-4f02-b831-dd22d285901d\", \"initial_sync_params\": {\"import_users\": true}}", //prod
                UseCron = false,
            });
            repo.SaveChanges();

            Response.Redirect($"/{MicrosoftSync.TeamUrl}/?refresh=true", true);
        }
    }
}