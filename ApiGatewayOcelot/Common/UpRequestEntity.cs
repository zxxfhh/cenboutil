using System.ComponentModel;

namespace ApiGatewayOcelot
{
    /// <summary>
    /// 上游请求模型
    /// </summary>
    public class UpRequestEntity
    {
        /// <summary>
        /// 传输长度
        ///</summary>
        [DisplayName("传输长度")]
        public long ContentLength { get; set; }
        /// <summary>
        /// 传输方式
        ///</summary>
        [DisplayName("传输方式")]
        public string ContentType { get; set; }
        /// <summary>
        /// 请求方法
        ///</summary>
        [DisplayName("请求方法")]
        public string RequestMethod { get; set; }
        /// <summary>
        /// 请求URL
        ///</summary>
        [DisplayName("请求URL")]
        public string RequestURL { get; set; }
        /// <summary>
        /// 请求人IP
        ///</summary>
        [DisplayName("请求人IP")]
        public string RemoteIp { get; set; }
        /// <summary>
        /// Ids4认证
        ///</summary>
        [DisplayName("Ids4认证")]
        public string Authorization { get; set; }
        /// <summary>
        /// 直连Token
        ///</summary>
        [DisplayName("直连Token")]
        public string SelfToken { get; set; }
        /// <summary>
        /// 请求时间
        ///</summary>
        [DisplayName("请求时间")]
        public DateTime RequestTime { get; set; } = DateTime.Now;
    }
}
