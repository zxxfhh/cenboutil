using IdentityServer;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IdentityServerHost.Quickstart.UI
{
    [SecurityHeaders]
    [Authorize]
    public class ClientController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IResourceStore _resources;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly IOptions<IdentityServerOptions> _options;
        private readonly ILogger<ClientController> _logger;

        public ClientController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IResourceStore resources,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            IOptions<IdentityServerOptions> options,
            ILogger<ClientController> logger)
        {
            _interaction = interaction;
            _clientStore = clientStore;
            _resources = resources;
            _schemeProvider = schemeProvider;
            _events = events;
            _options = options;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var aa = _clientStore;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshClient(string button)
        {
            DynamicResourcesStore.UpdateApiResourceAndScope();
            DynamicClientStore.UpdateClient();

            return RedirectToAction("Index");
        }

    }
}
