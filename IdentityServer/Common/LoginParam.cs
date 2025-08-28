using System.ComponentModel;

namespace IdentityServer
{
    public class LoginParam
    {
        /// <summary>
        /// 用户账号
        /// </summary>
        public string UserCode { get; set; }
        /// <summary>
        /// 用户密码
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// 请求模块编码
        /// </summary>
        public string ClientCode { get; set; }
        /// <summary>
        /// 请求来源
        /// </summary>
        public SourceType SourceType { get; set; }
    }

    /// <summary>
    /// 请求来源枚举
    /// </summary>
    public enum SourceType
    {
        [Description("网页")]
        Web = 1,
        [Description("一体机")]
        Android = 2,
        [Description("手机APP")]
        APP = 3,
        [Description("微信")]
        Wx = 4,
        [Description("H5页面")]
        H5 = 5,
    }

}
