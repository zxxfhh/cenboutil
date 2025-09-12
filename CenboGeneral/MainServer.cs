using CenBoCommon.Zxx;
using CenboNew.ServiceLog;
using MQTTnet;
using MQTTnet.Protocol;
using NewLife.Agent;
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

            //10分钟检测一次
            timergeneraldog = new TimerX(GeneralDog, null, 30 * 1000, 1000 * 60 * 10);
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "常规服务看守定时器(10分钟1次)开启成功", "开启服务");

            var timefwq = DateTime.Now.Date.AddHours(7);
            timerfwqrestart = new TimerX(FwqRestart, null, timefwq, 1000 * 60 * 60 * 24);
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "服务器重启定时器(每天7点)开启成功", "开启服务");

            //MQTT服务
            ThreadWithState<object[]> tws = new ThreadWithState<object[]>(new object[] { }, MqttConnect);
            Thread t = new Thread(tws.ThreadProc) { IsBackground = true };
            t.Start();

            //服务器重启监听
            ThreadWithState<object[]> tws2 = new ThreadWithState<object[]>(new object[] { }, FwqRestartCheck);
            Thread t2 = new Thread(tws2.ThreadProc) { IsBackground = true };
            t2.Start();
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
                bool isSuccess = false;
                string resultMessage = "";

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
                    result.result = 3;
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

                // 等待进程完成，最多N秒
                int waittime = 30 * 1000;
                bool finished = process.WaitForExit(waittime);

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

                    resultstr = $"命令执行超时（{waittime / 1000}秒）";
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
                else if (!string.IsNullOrWhiteSpace(error))
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

                // 等待进程完成，最多N秒
                int waittime = 30 * 1000;
                bool finished = process.WaitForExit(waittime);
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

                    resultstr = $"命令执行超时（{waittime / 1000}秒）";
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
                else if (!string.IsNullOrWhiteSpace(error))
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
                    if (model.AddresseeTel.IsZxxNullOrEmpty()) model.AddresseeTel = MainSetting.Current.NoteDefaultTel;
                    string res = "失败";
                    string resxml = "";
                    // 配置绑定和地址
                    Binding binding = new BasicHttpBinding();
                    EndpointAddress address = new EndpointAddress(MainSetting.Current.SmsUrl);
                    using (SmpWebServiceSoapClient client = new SmpWebServiceSoapClient(binding, address))
                    {
                        model.AddresseeTel.ToStringList().ForEach(tel =>
                        {
                            resxml = client.SendSms(model.NoteContent, tel, model.SendTime, "admin", "123456", model.AppCode);
                            Task.Delay(1000).Wait();
                        });
                        //resxml = client.SendSms(model.NoteContent, model.AddresseeTel, model.SendTime, "admin", "123456", model.AppCode);
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
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)  // 强制使用 3.1.1
                .WithCleanSession(false)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(15))
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
            Task.Delay(60 * 1000).Wait();
            mqttClient.ConnectAsync(clientOptions);
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "Mqtt连接断开", "MQTT");
            if (mqttConnectFailCount > 20)
            {
                // 检查服务状态
                var (checkSuccess, checkResult) = RunLinuxCmd($"systemctl is-active rabbitmq");
                //短信通知
                var _NoteInfo = new
                {
                    NoteContent = $"MQTT(20分钟)离线状态：{checkResult}",
                    SendTime = DateTime.Now.AddSeconds(50),
                    AppCode = "南湖劳安"
                };
                SmsNoteJy(_NoteInfo.ToJson());
                ////重启mqtt服务
                //string wincmd = "net stop RabbitMQ & net start RabbitMQ";
                //string linxcmd = "docker restart rabbitmq";

                //// 检测操作系统并执行相应的命令
                //bool isSuccess = false;
                //string resultMessage = "";

                //if (Environment.OSVersion.Platform == PlatformID.Unix)
                //{
                //    // Linux系统，以root执行
                //    (isSuccess, resultMessage) = RunLinuxCmd(linxcmd);
                //}
                //else
                //{
                //    // Windows系统，以管理员模式执行
                //    (isSuccess, resultMessage) = RunWindowCmd(wincmd);
                //}
                //if (isSuccess) mqttConnectFailCount = 0;
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
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "Mqtt重连成功并订阅", "MQTT");

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
                if (!MainSetting.Current.IsGeneralDog) return;
                if (!CheckRestartPermission())
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "服务没有管理员权限", "常规服务看守");
                    return;
                }

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "开始执行常规服务看守检查", "常规服务看守");

                // 检查系统服务和Docker容器
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    CheckLinuxServices();
                }
                else
                {
                    CheckWindowsServices();
                }

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "常规服务看守检查完成", "常规服务看守");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "常规服务看守", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 检查Linux系统服务
        /// </summary>
        private void CheckLinuxServices()
        {
            // 系统服务列表
            List<string> systemServices = new List<string>
            {
                "mysqld",
                "docker"
            };

            // Docker容器列表
            List<string> dockerContainers = new List<string>();
            if (!MainSetting.Current.IsXinChuang)
            {
                dockerContainers.Add("mysql");
                //dockerContainers.Add("rabbitmq");
                dockerContainers.Add("consul");
                dockerContainers.Add("nginx");
                dockerContainers.Add("redis");
                dockerContainers.Add("kkfileview");
            }
            else
            {
                dockerContainers.Add("nginx");
                dockerContainers.Add("consul");
                //dockerContainers.Add("rabbitmq");
                dockerContainers.Add("tidb");
                dockerContainers.Add("tendisplus");
                dockerContainers.Add("easysearch01");
                dockerContainers.Add("easysearch02");
                dockerContainers.Add("esconsole");
            }

            // 检查系统服务
            foreach (string service in systemServices)
            {
                const int maxRetries = 3;
                int currentAttempt = 0;
                while (currentAttempt < maxRetries)
                {
                    currentAttempt++;
                    try
                    {
                        var isSuccess = LinuxServiceDog(service);
                        if (isSuccess) break;
                    }
                    catch { }
                    Task.Delay(1000 * 60).Wait();
                }
            }

            // 检查Docker容器
            foreach (string container in dockerContainers)
            {
                const int maxRetries = 3;
                int currentAttempt = 0;
                while (currentAttempt < maxRetries)
                {
                    currentAttempt++;
                    try
                    {
                        var isSuccess = DockerContainerDog(container);
                        if (isSuccess) break;
                    }
                    catch { }
                    Task.Delay(1000 * 60).Wait();
                }
            }
        }

        /// <summary>
        /// 检查Windows系统服务
        /// </summary>
        private void CheckWindowsServices()
        {
            // Windows服务列表
            List<string> windowsServices = new List<string>
            {
                "MySQL57",
                "MySQL80",
                "RabbitMQ",
                "ConsulService",
                "Nginx",
                "Redis"
            };

            // 检查Windows服务
            foreach (string service in windowsServices)
            {
                const int maxRetries = 3;
                int currentAttempt = 0;
                while (currentAttempt < maxRetries)
                {
                    currentAttempt++;
                    try
                    {
                        var isSuccess = WindowsServiceDog(service);
                        if (isSuccess) break;
                    }
                    catch { }
                    Task.Delay(1000 * 60).Wait();
                }
            }
            Task.Delay(1000 * 60).Wait();
            //启动kkfileview程序(不要显示界面)，监听8012端口被占用说明程序启动成功。
            // 检查并重启kkfileview程序
            KkFileViewDog(MainSetting.Current.WinKkfileviewPath);
        }

        #region Windows系统服务

        /// <summary>
        /// 检查指定端口是否被占用
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>true表示端口被占用，false表示端口空闲</returns>
        private bool CheckPortInUse(int port)
        {
            try
            {
                // 使用netstat命令检查端口占用情况
                var (success, result) = RunWindowCmd($"netstat -an | findstr :{port}");
                if (success && !string.IsNullOrWhiteSpace(result))
                {
                    // 检查结果中是否包含LISTENING状态
                    return result.Contains("LISTENING") || result.Contains("ESTABLISHED");
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"检查端口 {port} 占用情况时发生异常：{ex.Message}", "常规服务看守", LOG_TYPE.ErrorLog);
                return false;
            }
        }

        /// <summary>
        /// 检查并重启Windows系统服务
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        private bool WindowsServiceDog(string serviceName)
        {
            bool restartSuccess = false;
            string resultMessage = "";
            try
            {
                // 检查服务状态
                var (checkSuccess, checkResult) = RunWindowCmd($"sc query \"{serviceName}\"");
                if (checkSuccess && checkResult.Contains("RUNNING"))
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"Windows服务 [{serviceName}] 运行正常", "常规服务看守");
                    return true;
                }
                // 尝试重启服务
                (restartSuccess, resultMessage) = RunWindowCmd($"net stop \"{serviceName}\" & net start \"{serviceName}\"");
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                         $"Windows服务 [{serviceName}] 重启{(restartSuccess ? "成功" : "失败")}：{resultMessage}", "常规服务看守");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"检查Windows服务 [{serviceName}] 时发生异常：{ex.Message}", "常规服务看守", LOG_TYPE.ErrorLog);
            }
            return restartSuccess;
        }

        #endregion

        #region  kkfileview

        /// <summary>
        /// 检查并重启kkfileview程序
        /// </summary>
        /// <param name="kkfileviewPath">kkfileview启动脚本路径</param>
        private void KkFileViewDog(string kkfileviewPath)
        {
            try
            {
                int targetPort = 8012;

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    "开始检查kkfileview程序状态", "常规服务看守");

                // 检查8012端口是否被占用
                bool isPortInUse = CheckPortInUse(targetPort);

                if (isPortInUse)
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"kkfileview程序运行正常，端口 {targetPort} 已被占用", "常规服务看守");
                    return;
                }

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"kkfileview程序未运行，端口 {targetPort} 未被占用，尝试启动程序", "常规服务看守");

                // 使用重试机制启动kkfileview
                const int maxRetries = 3;
                int currentAttempt = 0;
                bool startSuccess = false;

                while (currentAttempt < maxRetries && !startSuccess)
                {
                    currentAttempt++;
                    try
                    {
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"[kkfileview] 开始启动程序，第 {currentAttempt} 次尝试", "常规服务看守");

                        StartKkFileView(kkfileviewPath);

                        // 等待程序启动完成，最多等待30秒
                        int waitCount = 0;
                        int maxWaitCount = 30; // 30秒

                        while (waitCount < maxWaitCount)
                        {
                            Thread.Sleep(1000); // 等待1秒
                            waitCount++;

                            if (CheckPortInUse(targetPort))
                            {
                                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                                    $"[kkfileview] 程序启动成功，端口 {targetPort} 已被占用，耗时 {waitCount} 秒，第 {currentAttempt} 次尝试", "常规服务看守");
                                startSuccess = true;
                                break;
                            }
                        }

                        if (!startSuccess)
                        {
                            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                                $"[kkfileview] 程序启动超时，等待 {maxWaitCount} 秒后端口 {targetPort} 仍未被占用，第 {currentAttempt} 次尝试",
                                "常规服务看守", LOG_TYPE.ErrorLog);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"[kkfileview] 启动程序失败，第 {currentAttempt} 次尝试，错误信息: {ex.Message}",
                            "常规服务看守", LOG_TYPE.ErrorLog);
                    }

                    if (!startSuccess && currentAttempt < maxRetries)
                    {
                        // 等待一段时间后重试，递增等待时间：1秒、2秒、3秒
                        Thread.Sleep(1000 * currentAttempt);
                    }
                }

                if (!startSuccess)
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"[kkfileview] 程序已达到最大重试次数 {maxRetries}，最终启动失败",
                        "常规服务看守", LOG_TYPE.ErrorLog);
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"检查kkfileview程序时发生异常：{ex.Message}", "常规服务看守", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 启动kkfileview程序
        /// </summary>
        /// <param name="batFilePath">bat文件路径</param>
        private void StartKkFileView(string batFilePath)
        {
            if (!File.Exists(batFilePath))
            {
                throw new FileNotFoundException($"kkfileview启动文件不存在：{batFilePath}");
            }

            var psi = new ProcessStartInfo
            {
                FileName = batFilePath,
                UseShellExecute = false,
                CreateNoWindow = true, // 不显示界面
                WindowStyle = ProcessWindowStyle.Hidden, // 隐藏窗口
                WorkingDirectory = Path.GetDirectoryName(batFilePath) // 设置工作目录
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("无法启动kkfileview进程");
                }

                // 不等待进程结束，因为这是一个服务程序，会一直运行
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"kkfileview进程已启动，进程ID：{process.Id}", "常规服务看守");
            }
        }

        #endregion

        #region Linux系统服务

        /// <summary>
        /// 检查并重启Linux系统服务
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        private bool LinuxServiceDog(string serviceName)
        {
            bool restartSuccess = false;
            string resultMessage = "";
            try
            {
                // 检查服务状态
                var (checkSuccess, checkResult) = RunLinuxCmd($"systemctl is-active {serviceName}");
                if (checkSuccess && checkResult.Trim().Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"系统服务 [{serviceName}] 运行正常", "常规服务看守");
                    return true;
                }
                (restartSuccess, resultMessage) = RunLinuxCmd($"systemctl start {serviceName}");
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"系统服务 [{serviceName}] 重启{(restartSuccess ? "成功" : "失败")}：{resultMessage}", "常规服务看守");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"检查系统服务 [{serviceName}] 时发生异常：{ex.Message}", "常规服务看守", LOG_TYPE.ErrorLog);
            }
            return restartSuccess;
        }

        /// <summary>
        /// 检查并重启Docker容器
        /// </summary>
        /// <param name="containerName">容器名称</param>
        private bool DockerContainerDog(string containerName)
        {
            bool restartSuccess = false;
            string resultMessage = "";
            try
            {
                // 检查容器状态
                string checkCmd = $"docker ps --filter \"name={containerName}\" --format \"table {{{{.Names}}}}\\t{{{{.Status}}}}\"";
                var (checkSuccess, checkResult) = RunLinuxCmd(checkCmd);
                if (checkSuccess && checkResult.Contains("Up"))
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"Docker容器 [{containerName}] 运行正常", "常规服务看守");
                    return true;
                }

                // 尝试重启容器
                string restartCmd = $"docker start {containerName}";
                (restartSuccess, resultMessage) = RunLinuxCmd(restartCmd);
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                       $"Docker容器[{containerName}] 重启{(restartSuccess ? "成功" : "失败")}：{resultMessage}", "常规服务看守");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"检查Docker容器 [{containerName}] 时发生异常：{ex.Message}", "常规服务看守", LOG_TYPE.ErrorLog);
            }
            return restartSuccess;
        }

        #endregion

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

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "开始执行服务器重启", "服务器重启");

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

        #region 服务器重启监听

        /// <summary>
        /// 服务器重启
        /// </summary>
        /// <param name="obj"></param>
        private void FwqRestartCheck(object obj)
        {
            try
            {
                //2分钟之后执行一次，确保别的服务启动完成。
                Task.Delay(2 * 60 * 1000).Wait();
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "开始执行服务器重启", "服务器重启");



                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"服务器重启{(isSuccess ? "成功" : "失败")}：{resultMessage}", "服务器重启");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "服务器重启", LOG_TYPE.ErrorLog);
            }
        }


        #endregion

    }
}
