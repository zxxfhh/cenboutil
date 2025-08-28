using CenBoCommon.Zxx;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServerModel;

namespace IdentityServer
{
    public class DynamicClientStore : IClientStore
    {
        private readonly ILogger<DynamicClientStore> _logger;
        private static List<Client> _clients;

        public DynamicClientStore(ILogger<DynamicClientStore> logger)
        {
            _logger = logger;
            //if (_clients == null) _clients = ServerConfig.GetClients().ToList();
            if (_clients == null) GetClients();
        }

        private static void GetClients()
        {
            _clients = null;
            var clients = SysCommonDAO<Clientresources>.Instance.GetListBy(t => t.IsEnable == 1);
            if (clients.Any())
            {
                _clients = new List<Client>();
                ICollection<string> ResourceOwnerPasswordAndCode = new[] { GrantType.ResourceOwnerPassword, GrantType.AuthorizationCode };
                foreach (var item in clients)
                {
                    var _RedirectUris = item.RedirectUris.ToStringList('|');
                    var _PostLogoutRedirectUris = item.PostLogoutRedirectUris.ToStringList('|');
                    var _AllowedCorsOrigins = item.AllowedCorsOrigins.ToStringList('|');
                    var _AllowedScopes = item.AllowedScopes.ToStringList('|');
                    _AllowedScopes.Add("offline_access");
                    _AllowedScopes.Add("openid");
                    _AllowedScopes.Add("profile");
                    Client _client = new Client
                    {
                        ClientId = item.ClientId,
                        ClientName = item.ClientName,
                        ClientSecrets = { new Secret(item.ClientSecrets.Sha256()) },
                        AccessTokenLifetime = item.TokenLifeTime * 60 * 60,
                        AllowedGrantTypes = ResourceOwnerPasswordAndCode,
                        RefreshTokenExpiration = TokenExpiration.Absolute, //绝对过期模式
                        AbsoluteRefreshTokenLifetime = item.TokenLifeTime * 60 * 60 + 2, //强制过期

                        RequirePkce = true,
                        RequireConsent = true, //禁用 权限确认页面
                        RequireClientSecret = false, //不使用ClientSecret(网页端需要)

                        AllowOfflineAccess = true,//如果要获取refresh_tokens ,必须把AllowOfflineAccess设置为true

                        RedirectUris = _RedirectUris,
                        PostLogoutRedirectUris = _PostLogoutRedirectUris,
                        AllowedCorsOrigins = _AllowedCorsOrigins,
                        //作用域
                        AllowedScopes = _AllowedScopes
                    };
                    _clients.Add(_client);
                }
            }
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            var query =
                   from client in _clients
                   where client.ClientId == clientId
                   select client;

            return Task.FromResult(query.SingleOrDefault());
        }

        // 更新客户端配置
        public static bool UpdateClient()
        {
            bool result = false;
            try
            {
                GetClients();
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }
    }
}
