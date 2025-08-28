using IdentityServer.Encrypt;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using IdentityServerModel;
using System.Security.Claims;

namespace IdentityServer
{
    /// <summary>
    /// 密码模式，自定义用户验证
    /// </summary>
    public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            // 在这里添加你的用户验证逻辑
            // 比如查询数据库，检查用户名密码是否匹配
            if (!string.IsNullOrEmpty(context.UserName) && !string.IsNullOrEmpty(context.Password))
            {
                string UserPwd = EncryptsHelper.Encrypt(context.Password);
                string useruid = context.UserName.Split('|')[0];
                string sourcetype = context.UserName.Split('|')[1];
                Account user = AccountDAO.Instance.GetOneBy(t => t.UserUid == useruid
                                    && t.Password == UserPwd);
                if (user != null)
                {
                    bool isuserok = false;
                    if (user.IsEnable == 1)
                    {
                        ChangeLoginMessage(useruid, "用户信息被禁用。");
                        isuserok = false;
                    }
                    if (user.IsDelete == 1)
                    {
                        ChangeLoginMessage(useruid, "用户信息不存在。");
                        isuserok = false;
                    }
                    if (!isuserok)
                    {
                        user.LoginCount++;
                        user.OnlineState = 1;
                        user.LastLoginTime = DateTime.Now.ToDateTimeString(); ;
                        Task.Run(() =>
                        {
                            AccountDAO.Instance.UpdateColumns(user, it => new
                            {
                                it.OnlineState,
                                it.LastLoginTime,
                                it.LoginCount,
                            });
                        });
                        ChangeLoginMessage(useruid, "用户登录成功。");
                        string TokenLifeTime = context.Request.AccessTokenLifetime.ToString();
                        // 验证成功
                        context.Result = new GrantValidationResult(
                            subject: user.UserSnowId.ToString(),
                            authenticationMethod: "AAAAApwd",
                            claims: new Claim[] {
                            new Claim("sourcetype", sourcetype),
                            new Claim("name", user.TrueName),
                            new Claim("tokenlifetime", TokenLifeTime)
                            }
                            );
                        return Task.CompletedTask;
                    }
                }
                else
                {
                    ChangeLoginMessage(useruid, "用户名或者密码不正确。");
                }
            }

            // 验证失败
            return Task.FromResult(new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                "用户名或者密码不正确。"));
        }

        private void ChangeLoginMessage(string useruid, string message)
        {
            lock (OperatorCommon.LoginLock)
            {
                if (OperatorCommon.LoginMessage.ContainsKey(useruid))
                {
                    OperatorCommon.LoginMessage[useruid] = message;
                }
                else
                {
                    OperatorCommon.LoginMessage.Add(useruid, message);
                }
            }
        }

    }
}
