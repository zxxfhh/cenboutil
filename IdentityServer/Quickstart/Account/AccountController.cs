using IdentityModel;
using IdentityServer;
using IdentityServer.Encrypt;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
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
    public class AccountController : Controller
    {
        private readonly TestUserStore _users;
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
        private readonly ILogger<AccountController> _logger;

        public AccountController(
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
            ILogger<AccountController> logger)
        {
            // 默认用户
            _users = new TestUserStore(TestUsers.Users);

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

        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            // build a model so we know what to show on the login page
            var vm = await BuildLoginViewModelAsync(returnUrl);

            if (vm.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return RedirectToAction("Challenge", "External", new { scheme = vm.ExternalLoginScheme, returnUrl });
            }

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model, string button)
        {
            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            // the user clicked the "cancel" button
            if (button != "login")
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

            if (ModelState.IsValid)
            {
                // validate username/password against in-memory store
                if (_users.ValidateCredentials(model.Username, model.Password))
                {
                    var user = _users.FindByUsername(model.Username);
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.Username, user.SubjectId, user.Username, clientId: context?.Client.ClientId));

                    // only set explicit expiration here if user chooses "remember me". 
                    // otherwise we rely upon expiration configured in cookie middleware.
                    AuthenticationProperties props = null;
                    if (AccountOptions.AllowRememberLogin && model.RememberLogin)
                    {
                        props = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                        };
                    };

                    // issue authentication cookie with subject ID and username
                    var isuser = new IdentityServerUser(user.SubjectId)
                    {
                        DisplayName = user.Username
                    };

                    await HttpContext.SignInAsync(isuser, props);

                    if (context != null)
                    {
                        if (context.IsNativeClient())
                        {
                            // The client is native, so this change in how to
                            // return the response is for better UX for the end user.
                            return this.LoadingPage("Redirect", model.ReturnUrl);
                        }

                        // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                        return Redirect(model.ReturnUrl);
                    }

                    // request for a local page
                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else if (string.IsNullOrEmpty(model.ReturnUrl))
                    {
                        return Redirect("~/");
                    }
                    else
                    {
                        // user might have clicked on a malicious link - should be logged
                        throw new Exception("invalid return URL");
                    }
                }
                else
                {
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
                            user.LastLoginTime = DateTime.Now.ToDateTimeString(); ;
                            await Task.Run(() =>
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
                            //additionalLocalClaims.Add(new Claim(JwtClaimTypes.Name, user.TrueName));
                            additionalLocalClaims.Add(new Claim("sourcetype", "Web"));
                            string TokenLifeTime = "3600";
                            if (context != null)
                            {
                                TokenLifeTime = context.Client.AccessTokenLifetime.ToString();
                            }
                            additionalLocalClaims.Add(new Claim("tokenlifetime", TokenLifeTime));

                            // issue authentication cookie with subject ID and username
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
                                    // The client is native, so this change in how to
                                    // return the response is for better UX for the end user.
                                    return this.LoadingPage("Redirect", model.ReturnUrl);
                                }

                                // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                                return Redirect(model.ReturnUrl);
                            }

                            // request for a local page
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
                }
            }

            // something went wrong, show form with error
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
            }
            return Redirect("~/Account/Login?ReturnUrl=/grants");
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
            //return View("LoggedOut", vm);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
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
    }
}
