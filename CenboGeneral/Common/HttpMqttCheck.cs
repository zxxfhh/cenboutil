namespace CenboGeneral
{
    public class HttpMqttCheck
    {
        /// <summary>
        /// 请求唯一id(发送mq是生成GUID)
        /// </summary>
        public string commandID { get; set; } = "";

        /// <summary>
        /// 数据内容
        /// </summary>
        public string datastr { get; set; } = "";

        /// <summary>
        /// 线程控制
        /// </summary>
        public CancellationTokenSource cantoken { get; set; } = new CancellationTokenSource();
    }
}
