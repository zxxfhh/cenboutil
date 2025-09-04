using NewLife.Configuration;
using System.ComponentModel;

namespace CenboGeneral
{
    /// <summary>中转服务配置</summary>
    [Description("中转服务配置")]
    [Config("Config/MqttSetting.config")]
    public class MainSetting : Config<MainSetting>
    {

        /// <summary>本身http服务端口</summary>
        [Description("本身http服务端口")]
        public int HttpPort { get; set; } = 13595;

        /// <summary>Mqtt地址</summary>
        [Description("Mqtt地址")]
        public string MQHost { get; set; } = "192.168.0.76";

        /// <summary>Mqtt用户</summary>
        [Description("Mqtt用户")]
        public string MQUser { get; set; } = "cenbo";

        /// <summary>Mqtt密码</summary>
        [Description("Mqtt密码")]
        public string MQPass { get; set; } = "veITwUIjDR";

        /// <summary>Mqtt接收key</summary>
        [Description("Mqtt接收key")]
        public string MqRoutingKey { get; set; } = "httpservice/exchange/receive";

        [Description("短信调用Url")]
        public String SmsUrl { get; set; } = "http://dx.zjsjy.gov/SendSmsService/SmpWebService.asmx";


        /// <summary>短信通知默认号码(,隔开)</summary>
        [Description("短信通知默认号码(,隔开)")]
        public String NoteDefaultTel { get; set; } = "680574";


        /// <summary>发件人邮箱地址</summary>
        [Description("发件人邮箱地址")]
        public String SendEmail { get; set; } = "zhangxx@cenbo.com";

        /// <summary>发件人邮箱授权码</summary>
        [Description("SendEmailCode")]
        public string SendEmailCode { get; set; } = "3itQKBmrPeZaPAnx";

        /// <summary>SMTP邮件服务器(如果是QQ邮箱则：smtp:qq.com,依次类推)</summary>
        [Description("SMTP邮件服务器(如果是QQ邮箱则：smtp:qq.com,依次类推)")]
        public string EmailHost { get; set; } = "smtp.qiye.aliyun.com";

        /// <summary>SMTP邮件服务器端口</summary>
        [Description("SMTP邮件服务器端口")]
        public int EmailHostPort { get; set; } = 465;

        /// <summary>收件人邮箱地址集合(逗号隔开)</summary>
        [Description("收件人邮箱地址集合(逗号隔开)")]
        public String ReceiveEmails { get; set; } = "609912601@qq.com";


        /// <summary>是否每天6点重启服务器</summary>
        [Description("是否每天6点重启服务器")]
        public Boolean IsFwqRestart { get; set; } = true;

        /// <summary>是否启用常规服务看守</summary>
        [Description("是否启用常规服务看守")]
        public Boolean IsGeneralDog { get; set; } = true;

        /// <summary>kkkfileview路径(win)</summary>
        [Description("kkkfileview路径(win)")]
        public String WinKkfileviewPath { get; set; } = "I:\\Winds服务器环境\\kkFileView-4.1.0\\bin\\startup.bat";

    }
}

