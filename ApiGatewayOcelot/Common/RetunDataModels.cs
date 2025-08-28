using System.Runtime.Serialization;

namespace ApiGatewayOcelot
{
    /// <summary>
    /// API统一返回数据格式模型
    /// </summary>
    public class MetaData
    {

        private string _result = string.Empty;

        /// <summary>
        /// Josn数据
        /// </summary>           
        [DataMember]
        public string Result
        {
            get { return _result; }
            set { _result = value; }
        }

        /// <summary>
        /// 结果时间
        /// </summary>
        [DataMember]
        public DateTime Timestamp
        {
            get { return DateTime.Now; }
        }

        /// <summary>
        /// 返回状态
        /// </summary>
        [DataMember]
        public bool Status { get; set; }

        private string _message = string.Empty;
        /// <summary>
        /// 提示信息
        /// </summary>
        [DataMember]
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        /// <summary>
        /// 总数（分页时使用）
        /// </summary>
        private int _total = 0;
        public int Total
        {
            get { return _total; }
            set { _total = value; }
        }

    }

}
