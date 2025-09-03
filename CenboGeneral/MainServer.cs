using CenBoCommon.Zxx;
using CenboNew.ServiceLog;
using MQTTnet;
using MQTTnet.Protocol;
using NewLife.Http;
using NewLife.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace CenboGeneral
{
    public class MainServer
    {
        public HttpServer _httpServer = null;
        private List<HttpMqttCheck> cmdlist = new List<HttpMqttCheck>();
        /// <summary>
        /// 常规服务看守定时器
        /// </summary>
        private TimerX timergeneraldog = null;
        /// <summary>
        /// 业务服务看守定时器
        /// </summary>
        private TimerX timerbusinessdog = null;
        /// <summary>
        /// 每天6点重启服务器定时器
        /// </summary>
        private TimerX timerfwqrestart = null;
        /// <summary>
        /// Mqtt重连失败次数
        /// </summary>
        private int mqttConnectFailCount = 0;

        #region 开启和关闭

        public void Stop()
        {
            timergeneraldog?.Dispose();
            timerfwqrestart?.Dispose();
            timerbusinessdog?.Dispose();
            _httpServer?.Stop("服务停止");
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"服务端口[{MainSetting.Current.HttpPort}]关闭成功", "结束服务");
        }

        public void Start()
        {
            _httpServer = new HttpServer
            {
                Port = MainSetting.Current.HttpPort,
            };
            HttpComServer _HttpCom = new HttpComServer();
            _HttpCom.HttpComServerEvent += HttpComServerEvent;
            _httpServer.Map("/*", _HttpCom);
            _httpServer.Start();
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"Http端口监听成功{_httpServer.Port}", "开启服务");

            //2分钟检测一次
            timergeneraldog = new TimerX(GeneralDog, null, 30 * 1000, 1000 * 60 * 2);
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "常规服务看守定时器(2分钟1次)开启成功", "开启服务");

            var timefwq = DateTime.Now.Date.AddHours(6);
            timerfwqrestart = new TimerX(FwqRestart, null, timefwq, 1000 * 60 * 60 * 24);
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "服务器重启定时器(每天6点)开启成功", "开启服务");

            var obj = new object[] { };
            ThreadWithState<object[]> tws = new ThreadWithState<object[]>(obj, MqttConnect);
            Thread t = new Thread(tws.ThreadProc) { IsBackground = true };
            t.Start();
        }

        #endregion

        #region Http服务处理
        public void HttpComServerEvent(IHttpContext context)
        {
            bool iserrorres = true;
            try
            {
                if (context.Request.Method.ToUpper() == "POST")
                {
                    var pk = context.Request.Body;
                    byte[] dstArray = new byte[pk.Count];
                    Buffer.BlockCopy(pk.Data, pk.Offset, dstArray, 0, dstArray.Length);
                    string data = Encoding.UTF8.GetString(dstArray);
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"接收数据：{data}", "Http");

                    if (context.Path == "/")
                    {
                        if (!data.IsZxxNullOrEmpty())
                        {
                            var result = HttpToMqtt(data);
                            context.Response.StatusCode = HttpStatusCode.OK;
                            context.Response.SetResult(result.dataobj);
                        }
                    }
                    //执行系统命令
                    else if (context.Path == "/cenbo/cmd")
                    {
                        if (!data.IsZxxNullOrEmpty())
                        {
                            var result = CmdExecute(data);
                            context.Response.StatusCode = HttpStatusCode.OK;
                            context.Response.SetResult(result.ToJson());
                            iserrorres = false;
                        }
                    }
                    //短信通知(监狱)
                    else if (context.Path == "/cenbo/sms/jy")
                    {
                        if (!data.IsZxxNullOrEmpty())
                        {
                            var result = SmsNoteJy(data);
                            context.Response.StatusCode = HttpStatusCode.OK;
                            context.Response.SetResult(result.ToJson());
                            iserrorres = false;
                        }
                    }
                    //邮件通知
                    else if (context.Path == "/cenbo/email")
                    {
                        if (!data.IsZxxNullOrEmpty())
                        {
                            var result = EmailNote(data);
                            context.Response.StatusCode = HttpStatusCode.OK;
                            context.Response.SetResult(result.ToJson());
                            iserrorres = false;
                        }
                    }
                }
                else if (context.Request.Method.ToUpper() == "GET")
                {
                    var uid = context.Parameters["uid"].ToString();
                    var type = context.Parameters["type"].ToString();
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "Http", LOG_TYPE.ErrorLog);
            }
            if (iserrorres)
            {
                HttpResult result = new HttpResult
                {
                    result = 2,
                    message = "处理错误",
                };
                context.Response.StatusCode = HttpStatusCode.NotFound;
                context.Response.SetResult(result.ToJson());
            }
        }
        #endregion

        #region 消息转发

        /// <summary>
        /// HttpToMqtt消息转发
        /// </summary>
        /// <param name="data">消息</param>
        private HttpResult HttpToMqtt(string data)
        {
            HttpResult result = new HttpResult
            {
                result = 2,
                message = "处理错误",
            };
            try
            {
                var _nbModel = data.ToObject<NBModel>();
                if (_nbModel.commandID.IsZxxNullOrEmpty()
                    || _nbModel.routingkey.IsZxxNullOrEmpty()
                    || _nbModel.commandstr.IsZxxNullOrEmpty()) return result;

                var expireTime = _nbModel.expireTime.ToDateTime();
                string key = _nbModel.commandID;
                string routingkey = _nbModel.routingkey.ToString();

                HttpMqttCheck check = new HttpMqttCheck
                {
                    commandID = key,
                    datastr = "",
                };
                lock (cmdlist)
                {
                    if (!cmdlist.Any(x => x.commandID == key))
                    {
                        cmdlist.Add(check);
                    }
                }
                var isresult = MqttPublish(routingkey, data);
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"{routingkey}转发{(isresult ? "成功" : "失败")}", key);
                //等待mqtt结果
                while (true)
                {
                    if (check.cantoken.IsCancellationRequested) break;
                    if (expireTime < DateTime.Now)
                    {
                        result.result = 1;
                        result.message = "处理超时";
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"{routingkey}返回超时", key);
                        lock (cmdlist)
                        {
                            cmdlist.RemoveAll(x => x.commandID == key);
                        }
                        return result;
                    }
                    //等待20ms
                    Task.Delay(20).Wait();
                }
                //string datastr = check.datastr;
                result.result = 1;
                result.message = "处理成功";
                result.dataobj = check.datastr;
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"{routingkey}返回成功:{check.datastr}", key);
                lock (cmdlist)
                {
                    cmdlist.RemoveAll(x => x.commandID == key);
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "Http", LOG_TYPE.ErrorLog);
            }

            return result;
        }

        #endregion

        #region CMD指令执行

        /// <summary>
        /// 执行系统命令
        /// </summary>
        /// <param name="command">命令</param>
        private HttpResult CmdExecute(string command)
        {
            HttpResult result = new HttpResult
            {
                result = 2,
                message = "处理错误",
            };
            try
            {
                if (!CheckRestartPermission())
                {
                    result.message = "服务没有管理员权限。";
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"服务没有管理员权限。", "指令执行");
                    return result;
                }

                // 检测操作系统并执行相应的命令
                bool isSuccess;
                string resultMessage;

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    // Linux系统，以root执行
                    (isSuccess, resultMessage) = RunLinuxCmd(command);
                }
                else
                {
                    // Windows系统，以管理员模式执行
                    (isSuccess, resultMessage) = RunWindowCmd(command);
                }

                if (isSuccess)
                {
                    result.result = 1;
                    result.message = "执行成功";
                    result.dataobj = resultMessage;
                }
                else
                {
                    result.result = 2;
                    result.message = "执行失败";
                    result.dataobj = resultMessage;
                }

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"命令执行{(isSuccess ? "成功" : "失败")}：{command} -> {resultMessage}", "指令执行");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "指令执行", LOG_TYPE.ErrorLog);
                result.message = $"执行异常：{ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Linux系统执行命令（以root权限）
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <returns></returns>
        private (bool, string) RunLinuxCmd(string cmd)
        {
            bool isSuccess = false;
            string resultstr = "";
            Process process = null;

            try
            {
                // 使用sudo权限执行命令
                string sudoCmd = $"sudo {cmd}";

                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{sudoCmd}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Environment =
                    {
                        ["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
                    }
                };

                process = new Process { StartInfo = psi };
                process.Start();

                // 异步读取输出，避免死锁
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // 等待进程完成，最多3秒
                bool finished = process.WaitForExit(3000);

                if (!finished)
                {
                    // 超时，强制终止进程
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit(1000); // 等待进程完全退出
                        }
                    }
                    catch (Exception killEx)
                    {
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"强制终止进程失败: {killEx.Message}", "CMD", LOG_TYPE.ErrorLog);
                    }

                    resultstr = "错误: 命令执行超时（3秒）";
                    return (false, resultstr);
                }

                // 获取输出结果
                string output = outputTask.IsCompleted ? outputTask.Result : "";
                string error = errorTask.IsCompleted ? errorTask.Result : "";

                // 检查进程退出码
                int exitCode = process.ExitCode;

                // 合并输出信息
                var outputParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    outputParts.Add($"输出: {output.Trim()}");
                }
                if (!string.IsNullOrWhiteSpace(error))
                {
                    outputParts.Add($"错误: {error.Trim()}");
                }

                resultstr = outputParts.Count > 0 ? string.Join(" | ", outputParts) : "命令执行完成，无输出";

                // 根据退出码判断是否成功（0表示成功）
                isSuccess = (exitCode == 0);

                if (!isSuccess && outputParts.Count == 0)
                {
                    resultstr = $"命令执行失败，退出码: {exitCode}";
                }
            }
            catch (Exception ex)
            {
                resultstr = $"错误: {ex.Message}";
                isSuccess = false;
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"Linux命令执行异常: {ex}", "CMD", LOG_TYPE.ErrorLog);
            }
            finally
            {
                // 确保进程资源被释放
                try
                {
                    process?.Dispose();
                }
                catch (Exception disposeEx)
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"释放进程资源失败: {disposeEx.Message}", "CMD", LOG_TYPE.ErrorLog);
                }
            }

            return (isSuccess, resultstr);
        }

        /// <summary>
        /// Windows系统执行命令（以管理员权限）
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <returns></returns>
        private (bool, string) RunWindowCmd(string cmd)
        {
            bool isSuccess = false;
            string resultstr = "";
            Process process = null;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmd.StartsWith("/") ? cmd : $"/c {cmd}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    // 注意：当UseShellExecute = false时，Verb = "runas"不会生效
                    // 如果需要管理员权限，需要程序本身以管理员身份运行
                };

                process = new Process { StartInfo = psi };
                process.Start();

                // 异步读取输出，避免死锁
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // 等待进程完成，最多3秒
                bool finished = process.WaitForExit(3000);

                if (!finished)
                {
                    // 超时，强制终止进程
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit(1000); // 等待进程完全退出
                        }
                    }
                    catch (Exception killEx)
                    {
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"强制终止进程失败: {killEx.Message}", "CMD", LOG_TYPE.ErrorLog);
                    }

                    resultstr = "错误: 命令执行超时（3秒）";
                    return (false, resultstr);
                }

                // 获取输出结果
                string output = outputTask.IsCompleted ? outputTask.Result : "";
                string error = errorTask.IsCompleted ? errorTask.Result : "";

                // 检查进程退出码
                int exitCode = process.ExitCode;

                // 合并输出信息
                var outputParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    outputParts.Add($"输出: {output.Trim()}");
                }
                if (!string.IsNullOrWhiteSpace(error))
                {
                    outputParts.Add($"错误: {error.Trim()}");
                }

                resultstr = outputParts.Count > 0 ? string.Join(" | ", outputParts) : "命令执行完成，无输出";

                // 根据退出码判断是否成功（0表示成功）
                isSuccess = (exitCode == 0);

                if (!isSuccess && outputParts.Count == 0)
                {
                    resultstr = $"命令执行失败，退出码: {exitCode}";
                }
            }
            catch (Exception ex)
            {
                resultstr = $"错误: {ex.Message}";
                isSuccess = false;
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"Windows命令执行异常: {ex}", "CMD", LOG_TYPE.ErrorLog);
            }
            finally
            {
                // 确保进程资源被释放
                try
                {
                    process?.Dispose();
                }
                catch (Exception disposeEx)
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"释放进程资源失败: {disposeEx.Message}", "CMD", LOG_TYPE.ErrorLog);
                }
            }

            return (isSuccess, resultstr);
        }

        #endregion

        #region 短信通知

        /// <summary>
        /// 短信通知(监狱)
        /// </summary>
        /// <param name="data">消息</param>
        private HttpResult SmsNoteJy(string data)
        {
            HttpResult result = new HttpResult
            {
                result = 2,
                message = "处理错误",
            };
            try
            {
                var model = JsonConvert.DeserializeObject<NoteInfo>(data);
                if (model != null)
                {
                    string res = "失败";
                    string resxml = "";
                    // 配置绑定和地址
                    Binding binding = new BasicHttpBinding();
                    EndpointAddress address = new EndpointAddress(MainSetting.Current.SmsUrl);
                    using (SmpWebServiceSoapClient client = new SmpWebServiceSoapClient(binding, address))
                    {
                        resxml = client.SendSms(model.NoteContent, model.AddresseeTel, model.SendTime, "admin", "123456", model.AppCode);
                        JObject jo = JObject.Parse(resxml);
                        if (jo["status"] != null)
                        {
                            if (jo["status"].ToInt() == 200)
                            {
                                result.result = 3;
                                result.message = "短信发送成功";
                                res = "成功";
                            }
                            else if (jo["status"].ToInt() == 400)
                            {
                                result.result = 2;
                                result.message = jo["msgs"].ToString();
                            }
                        }
                    }
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"短信发送{res}：{resxml}", "短信发送");
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "短信发送", LOG_TYPE.ErrorLog);
            }

            return result;
        }

        #endregion

        #region 邮件通知

        /// <summary>
        /// 邮件通知
        /// </summary>
        /// <param name="data">消息</param>
        private HttpResult EmailNote(string data)
        {
            HttpResult result = new HttpResult
            {
                result = 2,
                message = "处理错误",
            };
            try
            {
                List<MailText> mailTextList = new List<MailText>();
                try
                {
                    var _mailTextList = JsonConvert.DeserializeObject<List<MailText>>(data);
                    if (_mailTextList.IsZxxAny()) mailTextList.AddRange(_mailTextList);
                }
                catch (Exception e)
                {
                    throw e;
                }
                if (mailTextList.IsZxxAny())
                {
                    foreach (var _mailText in mailTextList)
                    {
                        string rel = "失败";
                        MailKitEmail email = new MailKitEmail();
                        email.mailFrom = MainSetting.Current.SendEmail;
                        email.mailPwd = MainSetting.Current.SendEmailCode;
                        List<string> contents = new List<string>();
                        foreach (var text in _mailText.MessageList)
                        {
                            contents.Add($"<p style='word-break: normal; text-indent: 2em; color:red;'>{text}</p>");
                        }
                        email.mailSubject = _mailText.MailSubject;
                        email.mailBody = String.Format("<h3>您好：</h3>{0}"
                            + "<div style='width:200px;height:50px;float:right;margin-right:100px;'>"
                            + "<div style='width:100%;height:25px;line-height:25px;text-align:center;'>{1}</div>"
                            + "</div>", contents.ListZdToString(" "), DateTime.Now.ToString("yyyy年MM月dd日 HH时mm分"));
                        email.isbodyHtml = true;    //是否是HTML
                        email.host = MainSetting.Current.EmailHost;
                        email.hostport = MainSetting.Current.EmailHostPort;
                        //接收者邮件集合
                        if (_mailText.ReceiveEmails.IsZxxAny())
                            email.mailToArray = _mailText.ReceiveEmails.ToArray();
                        else
                            email.mailToArray = MainSetting.Current.ReceiveEmails.Split(',');

                        //string emailpath = FileTaskPath;
                        //email.attachmentsPath = new string[] { dir + emailpath.Replace("/", "\\") };//附件

                        if (email.Send()) rel = "成功";
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"邮件主题【{_mailText.MailSubject}】发送{rel}", "邮件发送");
                    }

                    result.result = 3;
                    result.message = $"邮件发送完成";
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "邮件发送", LOG_TYPE.ErrorLog);
            }

            return result;
        }

        #endregion

        #region Mqtt转发

        private IMqttClient? mqttClient;
        private MqttClientOptions? clientOptions;
        public void MqttConnect(Object obj)
        {
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(MainSetting.Current.MQHost, 1883) // 要访问的mqtt服务端的 ip 和 端口号
                .WithCredentials(MainSetting.Current.MQUser, MainSetting.Current.MQPass) // 要访问的mqtt服务端的用户名和密码
                .WithClientId($"CenboGeneral_{SnowModel.Instance.NewId()}") // 设置客户端id
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .WithTlsOptions(new MqttClientTlsOptions
                {
                    UseTls = false  // 是否使用 tls加密
                });

            clientOptions = optionsBuilder.Build();

            mqttClient = new MqttClientFactory().CreateMqttClient();
            mqttClient.ConnectedAsync += MqttConnectedAsync; // 客户端连接成功事件
            mqttClient.DisconnectedAsync += MqttDisConnectedAsync; // 客户端连接关闭事件
            mqttClient.ApplicationMessageReceivedAsync += MqttReceivedAsync; // 收到消息事件

            mqttClient.ConnectAsync(clientOptions);
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "Mqtt连接", "MQTT");
        }

        /// <summary>
        /// 客户端连接关闭事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task MqttDisConnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            mqttConnectFailCount++;
            Task.Delay(30 * 1000).Wait();
            mqttClient.ConnectAsync(clientOptions);
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "与服务端重新连接", "MQTT");
            if (mqttConnectFailCount > 10)
            {
                //重启mqtt服务


                mqttConnectFailCount = 0;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 客户端连接成功事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task MqttConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            // 订阅消息主题
            // MqttQualityOfServiceLevel: （QoS）:  0 最多一次，接收者不确认收到消息，并且消息不被发送者存储和重新发送提供与底层 TCP 协议相同的保证。
            // 1: 保证一条消息至少有一次会传递给接收方。发送方存储消息，直到它从接收方收到确认收到消息的数据包。一条消息可以多次发送或传递。
            // 2: 保证每条消息仅由预期的收件人接收一次。级别2是最安全和最慢的服务质量级别，保证由发送方和接收方之间的至少两个请求/响应（四次握手）。
            mqttClient.SubscribeAsync(MainSetting.Current.MqRoutingKey, MqttQualityOfServiceLevel.AtLeastOnce);
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "服务端的连接成功并订阅", "MQTT");

            return Task.CompletedTask;
        }

        /// <summary>
        /// 收到消息事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task MqttReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            var buffer = arg.ApplicationMessage.Payload.ToArray();
            if (buffer != null && buffer.Length > 0)
            {
                string strdata = Encoding.UTF8.GetString(buffer);
                if (strdata == "null" || strdata.IsZxxNullOrEmpty()) return Task.CompletedTask;
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"【qos等级={arg.ApplicationMessage.QualityOfServiceLevel}】 Json数据：{strdata}", "MQTT");
                try
                {
                    JObject jo = JObject.Parse(strdata);
                    if (jo != null && jo.Property("commandID") != null)
                    {
                        string key = jo["commandID"].ToString();
                        lock (cmdlist)
                        {
                            var check = cmdlist.FirstOrDefault(x => x.commandID == key);
                            if (check != null)
                            {
                                check.datastr = strdata;
                                check.cantoken.Cancel();
                                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"返回成功:{strdata}", key);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "错误", LOG_TYPE.ErrorLog);
                }
            }
            return Task.CompletedTask;
        }

        public bool MqttPublish(string publishtopic, string data)
        {
            bool isresult = false;
            try
            {
                if (mqttClient.IsConnected)
                {
                    var message = new MqttApplicationMessage
                    {
                        Topic = publishtopic,
                        PayloadSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)),
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                        Retain = false  // 服务端是否保留消息。true为保留，如果有新的订阅者连接，就会立马收到该消息。
                    };
                    var ret = mqttClient.PublishAsync(message).Result;
                    if (ret.IsSuccess)
                    {
                        isresult = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "错误", LOG_TYPE.ErrorLog);
            }
            return isresult;
        }

        #endregion

        #region 常规服务看守

        /// <summary>
        /// 常规服务看守
        /// </summary>
        /// <param name="obj"></param>
        private void GeneralDog(object? obj)
        {
            try
            {
                if (!CheckRestartPermission())
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"服务没有管理员权限。", "常规服务看守");
                    return;
                }

                //考虑windows和linux(docker)
                List<string> serviceList= new List<string>();
                serviceList.Add("mysqld");
                serviceList.Add("docker");

                //考虑windows和linux(docker)
                List<string> dockerOrWinList = new List<string>();
                dockerOrWinList.Add("mysql");
                dockerOrWinList.Add("rabbitmq");
                dockerOrWinList.Add("consul");
                dockerOrWinList.Add("nginx");
                dockerOrWinList.Add("redis");
                dockerOrWinList.Add("kkfileview");

                //只考虑linux(docker)
                List<string> dockerOnlyList = new List<string>();
                dockerOnlyList.Add("tidb");
                dockerOnlyList.Add("tendisplus");
                dockerOnlyList.Add("easysearch01");
                dockerOnlyList.Add("easysearch02");
                dockerOnlyList.Add("esconsole");
                


                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"短信发送", "常规服务看守");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "常规服务看守", LOG_TYPE.ErrorLog);
            }
        }

        #endregion

        #region 服务器重启

        /// <summary>
        /// 服务器重启
        /// </summary>
        /// <param name="obj"></param>
        private void FwqRestart(object? obj)
        {
            try
            {
                if (!MainSetting.Current.IsFwqRestart) return;
                if (!CheckRestartPermission())
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"服务没有管理员权限。", "服务器重启");
                    return;
                }
                // 检测操作系统并执行相应的命令
                bool isSuccess;
                string resultMessage;

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    // Linux系统，以root执行
                    (isSuccess, resultMessage) = RunLinuxCmd("reboot");
                }
                else
                {
                    // Windows系统，以管理员模式执行
                    (isSuccess, resultMessage) = RunWindowCmd("shutdown /r /t 10");
                }

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"服务器重启{(isSuccess ? "成功" : "失败")}：{resultMessage}", "服务器重启");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "服务器重启", LOG_TYPE.ErrorLog);
            }
        }

        // 可以考虑增加权限检查
        private bool CheckRestartPermission()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // 检查是否有sudo权限
                var (success, _) = RunLinuxCmd("sudo -n true");
                return success;
            }
            else
            {
                // 检查是否以管理员身份运行
                return new WindowsPrincipal(WindowsIdentity.GetCurrent())
                    .IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        #endregion
    }
}
