
using IdentityServer4;
using IdentityServer4.Models;

namespace IdentityServer
{
    public static class ServerConfig
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(), //必须要添加，否则报无效的scope错误
                new IdentityResources.Profile()
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            List<ApiResource> list = new List<ApiResource>();
            int clientCount = AppSetting.GetConfig("IdentityServer:ClientCount").ToInt2();
            for (int i = 1; i <= clientCount; i++)
            {
                string name = "IdentityServer:Client" + i;
                ApiResource _client = new ApiResource(AppSetting.GetConfig(name + ":Scope"),
                    AppSetting.GetConfig(name + ":Scope") + "ApiName")
                {
                    Scopes = new List<string>
                    {
                        AppSetting.GetConfig(name + ":Scope")
                    }
                };
                list.Add(_client);
            }
            return list;
        }

        public static IEnumerable<ApiScope> GetApiScopes()
        {
            List<ApiScope> list = new List<ApiScope>();
            int clientCount = AppSetting.GetConfig("IdentityServer:ClientCount").ToInt2();
            for (int i = 1; i <= clientCount; i++)
            {
                string name = "IdentityServer:Client" + i;
                list.Add(new ApiScope(AppSetting.GetConfig(name + ":Scope")));
            }
            return list;
        }

        public static IEnumerable<Client> GetClients()
        {
            ICollection<string> ResourceOwnerPasswordAndCode =
                    new[] { GrantType.ResourceOwnerPassword, GrantType.AuthorizationCode };

            List<Client> list = new List<Client>();
            int clientCount = AppSetting.GetConfig("IdentityServer:ClientCount").ToInt2();
            for (int i = 1; i <= clientCount; i++)
            {
                string name = "IdentityServer:Client" + i;
                int TokenLifeTime = AppSetting.GetInt(name + ":TokenLifeTime");
                Client _client = new Client
                {
                    ClientId = AppSetting.GetConfig(name + ":ClientId"),
                    ClientName = AppSetting.GetConfig(name + ":ClientId") + "Name",
                    ClientSecrets = { new Secret(AppSetting.GetConfig(name + ":ClientSecrets").Sha256()) },

                    AccessTokenLifetime = TokenLifeTime * 60 * 60,//设置AccessToken过期时间(秒)
                    //AllowedGrantTypes = GrantTypes.Implicit, //隐式流客户端（MVC）
                    //AllowedGrantTypes = GrantTypes.ResourceOwnerPassword, // 用户+密码
                    AllowedGrantTypes = ResourceOwnerPasswordAndCode,

                    //RefreshTokenExpiration = TokenExpiration.Sliding, //刷新令牌时，将刷新RefreshToken的生命周期。RefreshToken的总生命周期不会超过AbsoluteRefreshTokenLifetime。
                    //AbsoluteRefreshTokenLifetime = 7 * 24 * 60 * 60, //如果令牌状态（没有使用）超过7天，将强制过期
                    //SlidingRefreshTokenLifetime = 24 * 60 * 60,//以秒为单位滑动刷新令牌的生命周期。
                    ////按照现有的设置，如果24小时内没有使用RefreshToken，那么RefreshToken将失效。即便是在24小时内一直有使用RefreshToken，RefreshToken的总生命周期不会超过30天。所有的时间都可以按实际需求调整。
                    RefreshTokenExpiration = TokenExpiration.Absolute, //绝对过期模式
                    AbsoluteRefreshTokenLifetime = TokenLifeTime * 60 * 60 + 2, //强制过期

                    RequirePkce = true,
                    RequireConsent = false, //禁用 权限确认页面
                    RequireClientSecret = false, //不使用ClientSecret(网页端需要)

                    AllowOfflineAccess = true,//如果要获取refresh_tokens ,必须把AllowOfflineAccess设置为true

                    RedirectUris = { AppSetting.GetConfig(name + ":RedirectUris") },
                    PostLogoutRedirectUris = { AppSetting.GetConfig(name + ":PostLogoutRedirectUris") },
                    AllowedCorsOrigins = { AppSetting.GetConfig(name + ":AllowedCorsOrigins") },

                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OfflineAccess,  //如果要获取refresh_tokens ,必须在scopes中加上OfflineAccess
                        IdentityServerConstants.StandardScopes.OpenId,   //如果要获取id_token,必须在scopes中加上OpenId和Profile，id_token需要通过refresh_tokens获取AccessToken的时候才能拿到（还未找到原因）
                        IdentityServerConstants.StandardScopes.Profile,   //如果要获取id_token,必须在scopes中加上OpenId和Profile
                        AppSetting.GetConfig(name + ":Scope")
                    }
                };
                list.Add(_client);
            }
            return list;
        }
    }
}