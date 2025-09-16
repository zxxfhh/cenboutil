using MQTTnet;

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

    public class MqttConfig
    {
        /// <summary>
        /// Mqtt地址
        /// </summary>
        public string MQHost { get; set; }
        /// <summary>
        /// Mqtt用户
        /// </summary>
        public string MQUser { get; set; }
        /// <summary>
        /// Mqtt密码
        /// </summary>
        public string MQPass { get; set; }
        /// <summary>
        /// Mqtt接收key
        /// </summary>
        public string MqRoutingKey { get; set; }
    }

    /// <summary>
    /// MQTT客户端包装器
    /// </summary>
    public class MqttClientWrapper
    {
        public MqttConfig Config { get; set; }
        public IMqttClient Client { get; set; }
        public MqttClientOptions Options { get; set; }
        public bool IsConnected => Client?.IsConnected ?? false;
        public int ConnectFailCount { get; set; } = 0;
        public DateTime LastConnectTime { get; set; } = DateTime.MinValue;
        public string ConnectionId { get; set; }
    }
}
