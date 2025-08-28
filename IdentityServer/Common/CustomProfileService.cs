using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServerModel;
using Newtonsoft.Json;
using System.Security.Claims;

namespace IdentityServer
{
    public class CustomProfileService : IProfileService
    {
        /// <summary>
        /// 只要有关用户的身份信息单元被请求（例如在令牌创建期间或通过用户信息终点），就会调用此方法
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public virtual Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var claims = context.Subject.Claims.ToList();

            claims.RemoveAll(t => t.Type == "idp");
            claims.RemoveAll(t => t.Type == "amr");

            // 把认证通过的用户身份
            context.IssuedClaims = claims.ToList();

            return Task.CompletedTask;
        }

        /// <summary>
        /// 验证用户是否有效 例如：token创建或者验证
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public virtual Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;
            return Task.CompletedTask;
        }

    }
}
