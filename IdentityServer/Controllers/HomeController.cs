using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServerHost.Quickstart.UI;
using IdentityServerModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer.Controllers
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly IHttpContextAccessor _context;
        private readonly IMessageStore<LogoutMessage> _logoutMessageStore;
        private readonly IMessageStore<ErrorMessage> _errorMessageStore;
        private readonly IConsentMessageStore _consentMessageStore;
        private readonly IPersistedGrantService _grants;
        private readonly IUserSession _userSession;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            IHttpContextAccessor context,
            IMessageStore<LogoutMessage> logoutMessageStore,
            IMessageStore<ErrorMessage> errorMessageStore,
            IConsentMessageStore consentMessageStore,
            IPersistedGrantService grants,
            IUserSession userSession,
            ILogger<HomeController> logger)
        {
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _context = context;
            _logoutMessageStore = logoutMessageStore;
            _errorMessageStore = errorMessageStore;
            _consentMessageStore = consentMessageStore;
            _grants = grants;
            _userSession = userSession;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public string GetUserName()
        {
            return _context.HttpContext?.User?.GetDisplayName() ?? "";
        }
        /// <summary>
        /// 获取登录用户可访问的所有模块，及模块的操作菜单
        /// </summary>
        public string GetModulesTree()
        {
            string result = "";
            var mlist = ModuleDAO.Instance.GetList();
            if (mlist.Any())
            {
                var treelist = mlist.GenerateTree(u => u.Id, u => u.ParentId);
                if (treelist.Any())
                {
                    result = treelist.ToJson();
                }
            }
            return result;
        }

    }
}
