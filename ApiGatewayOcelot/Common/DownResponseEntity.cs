using System.ComponentModel;

namespace ApiGatewayOcelot
{
    /// <summary>
    /// 下游响应模型
    /// </summary>
    public class DownResponseEntity
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
        /// 请求URL
        ///</summary>
        [DisplayName("请求URL")]
        public string RequestURL { get; set; }
        /// <summary>
        /// 响应Code
        ///</summary>
        [DisplayName("响应Code")]
        public int StatusCode { get; set; }
        /// <summary>
        /// 响应时间
        ///</summary>
        [DisplayName("响应时间")]
        public DateTime ResponseTime { get; set; } = DateTime.Now;
    }
}
