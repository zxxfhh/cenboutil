using IdentityModel;
using IdentityServer;
using IdentityServer.Encrypt;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using IdentityServerModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace IdentityServerHost.Quickstart.UI
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class AuthLoginController : Controller
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
        private readonly ILogger<AuthLoginController> _logger;

        public AuthLoginController(
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
            ILogger<AuthLoginController> logger)
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

        public async Task<IActionResult> Index(string returnUrl = "")
        {
            string _ReturnUrl = "~/home";

            if (Request.HttpContext.User.Identity != null && Request.HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect(_ReturnUrl);
            }
            else
            {
                var vm = await BuildLoginViewModelAsync(returnUrl);
                vm.AllowRememberLogin = true;
                vm.ReturnUrl = _ReturnUrl;
                return View(vm);
            }
        }
        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login([FromBody] LoginInputModelAuth model)
        public async Task<IActionResult> Index(LoginInputModel model, string button)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            if (button != "login")
            //if (model.Button != "login")
            {
                if (context != null)
                {
                    // if the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.
                    await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    if (context.IsNativeClient())
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.
                        return this.LoadingPage("Redirect", model.ReturnUrl);
                    }

                    return Redirect(model.ReturnUrl);
                }
                else
                {
                    // since we don't have a valid context, then we just go back to the home page
                    return Redirect("~/");
                }
            }

            string UserPwd = EncryptsHelper.Encrypt(model.Password);
            Account user = AccountDAO.Instance.GetOneBy(t => t.UserUid == model.Username
                                && t.Password == UserPwd);
            string message = "用户名或者密码不正确。";
            bool isfail = false;
            if (user == null) isfail = true;
            if (user != null)
            {
                if (user.IsEnable == 0)
                {
                    message = "用户信息被禁用。";
                    isfail = true;
                }
                if (user.IsDelete == 0)
                {
                    message = "用户信息不存在。";
                    isfail = true;
                }
                if (!isfail)
                {
                    isfail = false;
                    user.LoginCount++;
                    user.OnlineState = 1;
                    user.LastLoginTime = DateTime.Now.ToDateTimeString();
                    var _1 = Task.Run(() =>
                     {
                         AccountDAO.Instance.UpdateColumns(user, it => new
                         {
                             it.OnlineState,
                             it.LastLoginTime,
                             it.LoginCount,
                         });
                     });

                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserUid, user.UserSnowId.ToString(), user.TrueName, clientId: context?.Client.ClientId));

                    AuthenticationProperties props = null;
                    if (AccountOptions.AllowRememberLogin && model.RememberLogin)
                    {
                        props = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                        };
                    };

                    var additionalLocalClaims = new List<Claim>();
                    additionalLocalClaims.Add(new Claim(JwtClaimTypes.AuthenticationMethod, "AAAAApwd"));
                    additionalLocalClaims.Add(new Claim("sourcetype", "Web"));
                    string TokenLifeTime = "3600";
                    if (context != null)
                    {
                        TokenLifeTime = context.Client.AccessTokenLifetime.ToString();
                    }
                    additionalLocalClaims.Add(new Claim("tokenlifetime", TokenLifeTime));

                    var isuser = new IdentityServerUser(user.UserSnowId.ToString())
                    {
                        DisplayName = user.TrueName,
                        AdditionalClaims = additionalLocalClaims
                    };

                    await HttpContext.SignInAsync(isuser, props);

                    if (context != null)
                    {
                        if (context.IsNativeClient())
                        {
                            return this.LoadingPage("Redirect", model.ReturnUrl);
                        }

                        return Redirect(model.ReturnUrl);
                    }

                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(model.ReturnUrl))
                        {
                            return Redirect("~/");
                        }
                        else
                        {
                            return Redirect(model.ReturnUrl);
                        }
                    }
                }
            }

            if (isfail)
            {
                await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, message));
                ModelState.AddModelError(string.Empty, message);
                var err = await BuildLoginViewModelAsync(model);
                return View(err);
            }
            var vm = await BuildLoginViewModelAsync(model);
            return View(vm);
        }


        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            //建立一个模型，以便注销页面知道要显示什么
            var vmm = await BuildLogoutViewModelAsync(logoutId);

            if (vmm.ShowLogoutPrompt == false)
            {
                //如果从IdentityServer正确验证了注销请求，则不需要显示提示，只需直接将用户注销即可。
                return await Logout(vmm);
            }
            else
            {
                var vm = await BuildLoggedOutViewModelAsync(logoutId);

                if (User?.Identity.IsAuthenticated == true)
                {
                    // delete local authentication cookie
                    await HttpContext.SignOutAsync();

                    // raise the logout event
                    await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));

                    long _UserSnowId = User.GetSubjectId().ToLong();
                    Account user = AccountDAO.Instance.GetOneBy(t => t.UserSnowId == _UserSnowId);
                    if (user != null)
                    {
                        user.OnlineState = 0;
                        user.LastOutTime = DateTime.Now.ToDateTimeString(); ;
                        await Task.Run(() =>
                        {
                            AccountDAO.Instance.UpdateColumns(user, it => new
                            {
                                it.OnlineState,
                                it.LastOutTime,
                            });
                        });
                    }
                }

                //// check if we need to trigger sign-out at an upstream identity provider
                //if (vm.TriggerExternalSignout)
                //{
                //    // build a return URL so the upstream provider will redirect back
                //    // to us after the user has logged out. this allows us to then
                //    // complete our single sign-out processing.
                //    string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                //    // this triggers a redirect to the external provider for sign-out
                //    return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
                //}
            }
            return Redirect("~/AuthLogin/Index?ReturnUrl=/grants");
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            // build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await HttpContext.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));

                long _UserSnowId = User.GetSubjectId().ToLong();
                Account user = AccountDAO.Instance.GetOneBy(t => t.UserSnowId == _UserSnowId);
                if (user != null)
                {
                    user.OnlineState = 0;
                    user.LastOutTime = DateTime.Now.ToDateTimeString(); ;
                    await Task.Run(() =>
                    {
                        AccountDAO.Instance.UpdateColumns(user, it => new
                        {
                            it.OnlineState,
                            it.LastOutTime,
                        });
                    });
                }
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }
            return Redirect(vm.PostLogoutRedirectUri);
        }

        /*****************************************/
        /* helper APIs for the AccountController */
        /*****************************************/
        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel
                {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    Username = context?.LoginHint,
                };

                if (!local)
                {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return vm;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null)
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.Client.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.Username = model.Username;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                //如果用户未通过身份验证，则只显示注销页面
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            //_logger.LogWarning($"用户正在注销(logoutId){logoutId}");
            var msg = await _logoutMessageStore.ReadAsync(logoutId);
            _logger.LogWarning($"用户正在注销(msg){JsonConvert.SerializeObject(msg)}");
            var iframeUrl = await GetIdentityServerSignoutFrameCallbackUrlAsync(_context.HttpContext, msg?.Data);
            //_logger.LogWarning($"用户正在注销(iframeUrl){iframeUrl}");
            var context = new LogoutRequest(iframeUrl, msg?.Data);
            _logger.LogWarning($"用户正在注销(context){JsonConvert.SerializeObject(context)}");
            var context2 = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                //自动注销是安全的
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            //显示注销提示。这可以防止用户受到攻击,被另一个恶意网页自动注销。
            return vm;
        }
        private async Task<string> GetIdentityServerSignoutFrameCallbackUrlAsync(HttpContext context, LogoutMessage logoutMessage = null)
        {
            var userSession = context.RequestServices.GetRequiredService<IUserSession>();
            var user = await userSession.GetUserAsync();
            var currentSubId = user?.GetSubjectId();

            LogoutNotificationContext endSessionMsg = null;

            // if we have a logout message, then that take precedence over the current user
            if (logoutMessage?.ClientIds?.Any() == true)
            {
                var clientIds = logoutMessage?.ClientIds;

                // check if current user is same, since we might have new clients (albeit unlikely)
                if (currentSubId == logoutMessage?.SubjectId)
                {
                    clientIds = clientIds.Union(await userSession.GetClientListAsync());
                    clientIds = clientIds.Distinct();
                }

                endSessionMsg = new LogoutNotificationContext
                {
                    SubjectId = logoutMessage.SubjectId,
                    SessionId = logoutMessage.SessionId,
                    ClientIds = clientIds
                };
            }
            else if (currentSubId != null)
            {
                // see if current user has any clients they need to signout of 
                var clientIds = await userSession.GetClientListAsync();
                if (clientIds.Any())
                {
                    endSessionMsg = new LogoutNotificationContext
                    {
                        SubjectId = currentSubId,
                        SessionId = await userSession.GetSessionIdAsync(),
                        ClientIds = clientIds
                    };
                }
            }

            if (endSessionMsg != null)
            {
                var clock = context.RequestServices.GetRequiredService<ISystemClock>();
                var msg = new Message<LogoutNotificationContext>(endSessionMsg, clock.UtcNow.UtcDateTime);

                var endSessionMessageStore = context.RequestServices.GetRequiredService<IMessageStore<LogoutNotificationContext>>();
                var id = await endSessionMessageStore.WriteAsync(msg);

                var signoutIframeUrl = EnsureTrailingSlash(context.GetIdentityServerBaseUrl()) + "connect/endsession/callback";
                signoutIframeUrl = AddQueryString(signoutIframeUrl, "endSessionId", id);

                return signoutIframeUrl;
            }

            // no sessions, so nothing to cleanup
            return null;
        }
        private string EnsureTrailingSlash(string url)
        {
            if (url != null && !url.EndsWith("/"))
            {
                return url + "/";
            }

            return url;
        }
        private string AddQueryString(string url, string name, string value)
        {
            return AddQueryString(url, name + "=" + System.Text.Encodings.Web.UrlEncoder.Default.Encode(value));
        }

        private string AddQueryString(string url, string query)
        {
            if (!url.Contains("?"))
            {
                url += "?";
            }
            else if (!url.EndsWith("&"))
            {
                url += "&";
            }

            return url + query;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }

        #region SSO授权页面
        /// <summary>
        /// Shows the consent screen
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Consent(string returnUrl)
        {
            string _ReturnUrl = "~/grants";
            var vm = await BuildViewModelAsync(returnUrl);
            vm.ReturnUrl = _ReturnUrl;
            return View("Consent", vm);
        }

        /// <summary>
        /// Handles the consent screen postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Consent(ConsentInputModel model)
        {
            var result = await ProcessConsent(model);

            if (result.IsRedirect)
            {
                var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                if (context?.IsNativeClient() == true)
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.
                    return this.LoadingPage("Redirect", result.RedirectUri);
                }

                return Redirect(result.RedirectUri);
            }

            if (result.HasValidationError)
            {
                ModelState.AddModelError(string.Empty, result.ValidationError);
            }

            if (result.ShowView)
            {
                return View("Index", result.ViewModel);
            }

            return View("Error");
        }

        /*****************************************/
        /* helper APIs for the ConsentController */
        /*****************************************/
        private async Task<ProcessConsentResult> ProcessConsent(ConsentInputModel model)
        {
            var result = new ProcessConsentResult();

            // validate return url is still valid
            var request = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            if (request == null) return result;

            ConsentResponse grantedConsent = null;

            // user clicked 'no' - send back the standard 'access_denied' response
            if (model?.Button == "no")
            {
                grantedConsent = new ConsentResponse { Error = AuthorizationError.AccessDenied };

                // emit event
                await _events.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues));
            }
            // user clicked 'yes' - validate the data
            else if (model?.Button == "yes")
            {
                // if the user consented to some scope, build the response model
                if (model.ScopesConsented != null && model.ScopesConsented.Any())
                {
                    var scopes = model.ScopesConsented;
                    if (ConsentOptions.EnableOfflineAccess == false)
                    {
                        scopes = scopes.Where(x => x != IdentityServer4.IdentityServerConstants.StandardScopes.OfflineAccess);
                    }

                    grantedConsent = new ConsentResponse
                    {
                        RememberConsent = model.RememberConsent,
                        ScopesValuesConsented = scopes.ToArray(),
                        Description = model.Description
                    };

                    // emit event
                    await _events.RaiseAsync(new ConsentGrantedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues, grantedConsent.ScopesValuesConsented, grantedConsent.RememberConsent));
                }
                else
                {
                    result.ValidationError = ConsentOptions.MustChooseOneErrorMessage;
                }
            }
            else
            {
                result.ValidationError = ConsentOptions.InvalidSelectionErrorMessage;
            }

            if (grantedConsent != null)
            {
                // communicate outcome of consent back to identityserver
                await _interaction.GrantConsentAsync(request, grantedConsent);

                // indicate that's it ok to redirect back to authorization endpoint
                result.RedirectUri = model.ReturnUrl;
                result.Client = request.Client;
            }
            else
            {
                // we need to redisplay the consent UI
                result.ViewModel = await BuildViewModelAsync(model.ReturnUrl, model);
            }

            return result;
        }

        private async Task<ConsentViewModel> BuildViewModelAsync(string returnUrl, ConsentInputModel model = null)
        {
            var request = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (request != null)
            {
                return CreateConsentViewModel(model, returnUrl, request);
            }
            else
            {
                _logger.LogError("No consent request matching request: {0}", returnUrl);
            }

            return null;
        }

        private ConsentViewModel CreateConsentViewModel(
            ConsentInputModel model, string returnUrl,
            AuthorizationRequest request)
        {
            var vm = new ConsentViewModel
            {
                RememberConsent = model?.RememberConsent ?? true,
                ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>(),
                Description = model?.Description,

                ReturnUrl = returnUrl,

                ClientName = request.Client.ClientName ?? request.Client.ClientId,
                ClientUrl = request.Client.ClientUri,
                ClientLogoUrl = request.Client.LogoUri,
                AllowRememberConsent = request.Client.AllowRememberConsent
            };

            vm.IdentityScopes = request.ValidatedResources.Resources.IdentityResources.Select(x => CreateScopeViewModel(x, vm.ScopesConsented.Contains(x.Name) || model == null)).ToArray();

            var apiScopes = new List<ScopeViewModel>();
            foreach (var parsedScope in request.ValidatedResources.ParsedScopes)
            {
                var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
                if (apiScope != null)
                {
                    var scopeVm = CreateScopeViewModel(parsedScope, apiScope, vm.ScopesConsented.Contains(parsedScope.RawValue) || model == null);
                    apiScopes.Add(scopeVm);
                }
            }
            if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess)
            {
                apiScopes.Add(GetOfflineAccessScope(vm.ScopesConsented.Contains(IdentityServer4.IdentityServerConstants.StandardScopes.OfflineAccess) || model == null));
            }
            vm.ApiScopes = apiScopes;

            return vm;
        }

        private ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check)
        {
            return new ScopeViewModel
            {
                Value = identity.Name,
                DisplayName = identity.DisplayName ?? identity.Name,
                Description = identity.Description,
                Emphasize = identity.Emphasize,
                Required = identity.Required,
                Checked = check || identity.Required
            };
        }

        public ScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check)
        {
            var displayName = apiScope.DisplayName ?? apiScope.Name;
            if (!String.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter))
            {
                displayName += ":" + parsedScopeValue.ParsedParameter;
            }

            return new ScopeViewModel
            {
                Value = parsedScopeValue.RawValue,
                DisplayName = displayName,
                Description = apiScope.Description,
                Emphasize = apiScope.Emphasize,
                Required = apiScope.Required,
                Checked = check || apiScope.Required
            };
        }

        private ScopeViewModel GetOfflineAccessScope(bool check)
        {
            return new ScopeViewModel
            {
                Value = IdentityServer4.IdentityServerConstants.StandardScopes.OfflineAccess,
                DisplayName = ConsentOptions.OfflineAccessDisplayName,
                Description = ConsentOptions.OfflineAccessDescription,
                Emphasize = true,
                Checked = check
            };
        }
        #endregion

    }
}
