using System.ComponentModel;

namespace MqttHttpService;

/// <summary>
/// 邮件推送内容
/// </summary>
public class MailText
{
    /// <summary>
    /// 邮件标题
    /// </summary>
    [DisplayName("邮件标题")]
    public string MailSubject { get; set; }

    /// <summary>
    /// 收件人邮箱地址集合
    ///</summary>
    [DisplayName("收件人邮箱地址集合")]
    public List<string> ReceiveEmails { get; set; } = new List<string>();

    /// <summary>
    /// 推送内容
    ///</summary>
    [DisplayName("推送内容")]
    public List<string> MessageList { get; set; } = new List<string>();

}