using MailKit.Net.Smtp;
using MimeKit;
using NewLife.Log;

namespace CenboGeneral
{
    public class MailKitEmail
    {
        /// <summary>
        /// 发送者
        /// </summary>
        public string mailFrom { get; set; }

        /// <summary>
        /// 收件人
        /// </summary>
        public string[] mailToArray { get; set; }

        /// <summary>
        /// 抄送
        /// </summary>
        public string[] mailCcArray { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string mailSubject { get; set; }

        /// <summary>
        /// 正文
        /// </summary>
        public string mailBody { get; set; }

        /// <summary>
        /// 发件人密码
        /// </summary>
        public string mailPwd { get; set; }

        /// <summary>
        /// SMTP邮件服务器
        /// </summary>
        public string host { get; set; }

        /// <summary>
        /// SMTP邮件服务器端口
        /// </summary>
        public int hostport { get; set; }

        /// <summary>
        /// 正文是否是html格式
        /// </summary>
        public bool isbodyHtml { get; set; }

        /// <summary>
        /// 附件
        /// </summary>
        public string[] attachmentsPath { get; set; }


        /// <summary>
        /// 邮箱发送类
        /// </summary>
        /// <param name="toEmaill">发送方邮箱</param>
        /// <param name="toEmailBlonger">发送方名称</param>
        /// <param name="subject">邮件标题</param>
        /// <param name="text">发送的文字内容</param>
        /// <param name="html">发送的html内容</param>
        /// <param name="path">发送的附件,找不到的就自动过滤</param>
        /// <returns></returns>
        public bool Send()
        {
            try
            {
                MimeMessage message = new MimeMessage();
                //发送方
                message.From.Add(new MailboxAddress("发件人", this.mailFrom));
                //向收件人地址集合添加邮件地址
                if (mailToArray != null)
                {
                    for (int i = 0; i < mailToArray.Length; i++)
                    {
                        message.To.Add(new MailboxAddress("收件人" + i, mailToArray[i].ToString()));
                    }
                }
                //向抄送收件人地址集合添加邮件地址
                if (mailCcArray != null)
                {
                    for (int i = 0; i < mailCcArray.Length; i++)
                    {
                        message.Cc.Add(new MailboxAddress("抄送人" + i, mailCcArray[i].ToString()));
                    }
                }

                //标题
                message.Subject = mailSubject;

                var builder = new BodyBuilder();
                if (isbodyHtml)
                {
                    builder.HtmlBody = mailBody;
                }
                else
                {
                    builder.TextBody = mailBody;
                }

                //在有附件的情况下添加附件
                try
                {
                    if (attachmentsPath != null && attachmentsPath.Length > 0)
                    {
                        foreach (string filepath in attachmentsPath)
                        {
                            builder.Attachments.Add(filepath);
                        }
                    }
                }
                catch (Exception err)
                {
                    throw new Exception("在添加附件时有错误:" + err);
                }

                //赋值邮件内容
                message.Body = builder.ToMessageBody();

                //开始发送
                using (var client = new SmtpClient())
                {
                    bool isssl = false;
                    if (this.hostport != 25)
                    {
                        isssl = true;
                    }
                    client.Connect(this.host, this.hostport, isssl);
                    client.Authenticate(this.mailFrom, this.mailPwd);
                    client.Send(message);
                    client.Disconnect(true);
                }
                return true;
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"发送邮件错误：{ex.ToString()}");
                return false;
            }
        }

    }
}
