using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServerModel;
using CenBoCommon.Zxx;

namespace IdentityServer
{
    public class DynamicResourcesStore : IResourceStore
    {
        private readonly ILogger<DynamicResourcesStore> _logger;
        private static List<IdentityResource> _identityResources;
        private static List<ApiResource> _apiResources;
        private static List<ApiScope> _apiScopes;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryResourcesStore" /> class.
        /// </summary>
        public DynamicResourcesStore(ILogger<DynamicResourcesStore> logger)
        {
            _logger = logger;
            if (_identityResources == null) _identityResources = ServerConfig.GetIdentityResources().ToList();
            //if (_apiResources == null) _apiResources = ServerConfig.GetApiResources().ToList();
            //if (_apiScopes == null) _apiScopes = ServerConfig.GetApiScopes().ToList();
            if (_apiResources == null && _apiScopes == null) GetResourcesAndScopes();
        }

        private static void GetResourcesAndScopes()
        {
            _apiResources = null;
            _apiScopes = null;
            var clients = SysCommonDAO<Clientresources>.Instance.GetListBy(t => t.IsEnable == 1);
            if (clients.Any())
            {
                _apiResources = new List<ApiResource>();
                _apiScopes = new List<ApiScope>();
                foreach (var item in clients)
                {
                    var _AllowedScopes = item.AllowedScopes.ToStringList('|');
                    foreach (var scope in _AllowedScopes)
                    {
                        ApiResource _ApiResource = new ApiResource(scope, scope + "ApiName")
                        {
                            Scopes = new List<string> { scope }
                        };
                        _apiResources.Add(_ApiResource);

                        ApiScope _ApiScope = new ApiScope(scope);
                        _apiScopes.Add(_ApiScope);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public Task<Resources> GetAllResourcesAsync()
        {
            var result = new Resources(_identityResources, _apiResources, _apiScopes);
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            if (apiResourceNames == null) throw new ArgumentNullException(nameof(apiResourceNames));

            var query = from a in _apiResources
                        where apiResourceNames.Contains(a.Name)
                        select a;
            return Task.FromResult(query);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null) throw new ArgumentNullException(nameof(scopeNames));

            var identity = from i in _identityResources
                           where scopeNames.Contains(i.Name)
                           select i;

            return Task.FromResult(identity);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null) throw new ArgumentNullException(nameof(scopeNames));

            var query = from a in _apiResources
                        where a.Scopes.Any(x => scopeNames.Contains(x))
                        select a;

            return Task.FromResult(query);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null) throw new ArgumentNullException(nameof(scopeNames));

            var query =
                from x in _apiScopes
                where scopeNames.Contains(x.Name)
                select x;

            return Task.FromResult(query);
        }

        public static bool UpdateApiResourceAndScope()
        {
            bool result = false;
            try
            {
                GetResourcesAndScopes();
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
