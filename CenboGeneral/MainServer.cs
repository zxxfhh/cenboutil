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
using static NewLife.Remoting.ApiHttpClient;

namespace CenboGeneral
{
    public class MainServer
    {
        public HttpServer _httpServer = null;
        private List<HttpMqttCheck> cmdlist = new List<HttpMqttCheck>();
        /// <summary>
        /// 常规服务看守定时器
        /// </summary>
        public TimerX timergeneraldog = null;
        /// <summary>
        /// 业务服务看守定时器
        /// </summary>
        public TimerX timerbusinessdog = null;
        /// <summary>
        /// 每天6点重启服务器定时器
        /// </summary>
        public TimerX timerfwqrestart = null;

        #region 开启和关闭

        public void Stop()
        {
            timergeneraldog?.Dispose();
            timerfwqrestart?.Dispose();
            timerbusinessdog?.Dispose();
            _httpServer?.Stop("服务停止");

            // 关闭所有MQTT连接
            DisconnectAllMqttClients();

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
                var isresult = MqttPublish(routingkey, data).Result;
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
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{cmd}\"",
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
                int waittime = 60 * 1000;
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
                int waittime = 60 * 1000;
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

        /// <summary>
        /// 多MQTT客户端连接管理
        /// </summary>
        private List<MqttClientWrapper> mqttClients = new List<MqttClientWrapper>();
        /// <summary>
        /// 线程安全锁
        /// </summary>
        private readonly object mqttLock = new object();
        public void MqttConnect(Object obj)
        {
            try
            {
                // 解析多MQTT配置
                var mqttConfigs = JsonConvert.DeserializeObject<List<MqttConfig>>(MainSetting.Current.MQServersConfig);
                if (mqttConfigs == null || !mqttConfigs.Any())
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "未找到有效的MQTT服务器配置，请检查MQServersConfig配置", "MQTT");
                    return;
                }

                lock (mqttLock)
                {
                    // 清理已有连接
                    DisconnectAllMqttClients();
                    mqttClients.Clear();

                    // 为每个配置创建连接
                    foreach (var config in mqttConfigs)
                    {
                        var wrapper = CreateMqttClientWrapper(config);
                        mqttClients.Add(wrapper);

                        // 异步连接
                        Task.Run(async () => await ConnectMqttClient(wrapper));
                    }
                }

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"开始连接{mqttConfigs.Count}个MQTT服务器", "MQTT");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"多MQTT连接失败：{ex.Message}", "MQTT", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 创建MQTT客户端包装器
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private MqttClientWrapper CreateMqttClientWrapper(MqttConfig config)
        {
            long snowId = SnowModel.Instance.NewId();
            string clientName = $"CenboGeneral_{config.MQHost}";
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(config.MQHost, 1883)
                .WithCredentials(config.MQUser, config.MQPass)
                .WithClientId($"{clientName}_{snowId}")
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                .WithCleanSession(false)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(15))
                .WithTlsOptions(new MqttClientTlsOptions
                {
                    UseTls = false
                });

            var client = new MqttClientFactory().CreateMqttClient();
            var wrapper = new MqttClientWrapper
            {
                Config = config,
                Client = client,
                Options = optionsBuilder.Build(),
                ConnectionId = clientName
            };

            // 绑定事件
            client.ConnectedAsync += (args) => MqttMultiConnectedAsync(wrapper, args);
            client.DisconnectedAsync += (args) => MqttMultiDisConnectedAsync(wrapper, args);
            client.ApplicationMessageReceivedAsync += (args) => MqttMultiReceivedAsync(wrapper, args);

            return wrapper;
        }

        /// <summary>
        /// 连接单个MQTT客户端
        /// </summary>
        /// <param name="wrapper"></param>
        private async Task ConnectMqttClient(MqttClientWrapper wrapper)
        {
            try
            {
                await wrapper.Client.ConnectAsync(wrapper.Options);
                wrapper.LastConnectTime = DateTime.Now;
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"MQTT连接尝试: ({wrapper.Config.MQHost})", "MQTT");
            }
            catch (Exception ex)
            {
                wrapper.ConnectFailCount++;
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"MQTT连接失败: {ex.Message}", "MQTT", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 断开所有MQTT连接
        /// </summary>
        private void DisconnectAllMqttClients()
        {
            lock (mqttLock)
            {
                foreach (var wrapper in mqttClients)
                {
                    try
                    {
                        wrapper.Client?.DisconnectAsync();
                        wrapper.Client?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"断开MQTT连接失败: {wrapper.Config?.MQHost} - {ex.Message}", "MQTT", LOG_TYPE.ErrorLog);
                    }
                }
            }
        }

        /// <summary>
        /// 多MQTT客户端连接成功事件
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task MqttMultiConnectedAsync(MqttClientWrapper wrapper, MqttClientConnectedEventArgs arg)
        {
            try
            {
                wrapper.ConnectFailCount = 0;
                // 订阅消息主题
                await wrapper.Client.SubscribeAsync(wrapper.Config.MqRoutingKey, MqttQualityOfServiceLevel.AtLeastOnce);
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"MQTT连接成功并订阅: {wrapper.Config?.MQHost}-{wrapper.Config?.MqRoutingKey}", "MQTT");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"MQTT订阅失败: {wrapper.Config?.MQHost} - {ex.Message}", "MQTT", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 多MQTT客户端断开连接事件
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task MqttMultiDisConnectedAsync(MqttClientWrapper wrapper, MqttClientDisconnectedEventArgs arg)
        {
            wrapper.ConnectFailCount++;
            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                $"MQTT连接断开: {wrapper.Config?.MQHost}, 失败次数: {wrapper.ConnectFailCount}{(wrapper.ConnectFailCount >= 10 ? "执行重启" : "")}", "MQTT");

            if (wrapper.ConnectFailCount >= 10)
            {
                bool isSuccess = false;
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    isSuccess = DockerContainerDog("rabbitmq");
                }
                else
                {
                    isSuccess = WindowsServiceDog("RabbitMQ");
                }
                wrapper.ConnectFailCount = 0;
                await Task.Delay(60 * 1000);
            }
            // 延迟重连
            await Task.Run(async () =>
             {
                 await Task.Delay(5 * 1000); // 等待1分钟再重连
                 try
                 {
                     if (!wrapper.Client.IsConnected)
                     {
                         await wrapper.Client.ConnectAsync(wrapper.Options);
                         wrapper.LastConnectTime = DateTime.Now;
                     }
                 }
                 catch (Exception ex)
                 {
                     ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                         $"MQTT重连失败: {wrapper.Config?.MQHost} - {ex.Message}", "MQTT", LOG_TYPE.ErrorLog);
                 }
             });
        }

        /// <summary>
        /// 多MQTT客户端收到消息事件
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task MqttMultiReceivedAsync(MqttClientWrapper wrapper, MqttApplicationMessageReceivedEventArgs arg)
        {
            var buffer = arg.ApplicationMessage.Payload.ToArray();
            if (buffer != null && buffer.Length > 0)
            {
                string strdata = Encoding.UTF8.GetString(buffer);
                if (strdata == "null" || strdata.IsZxxNullOrEmpty()) return;
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"【来源: {wrapper.Config?.MQHost}】【qos等级={arg.ApplicationMessage.QualityOfServiceLevel}】 Json数据：{strdata}", "MQTT");

                await Task.Run(() =>
                {
                    try
                    {
                        JObject jo = JObject.Parse(strdata);
                        if (jo != null && jo.Property("commandID") != null)
                        {
                            string key = jo["commandID"]?.ToString();
                            lock (cmdlist)
                            {
                                var check = cmdlist.FirstOrDefault(x => x.commandID == key);
                                if (check != null)
                                {
                                    check.datastr = strdata;
                                    check.cantoken.Cancel();
                                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                                        $"返回成功({wrapper.Config?.MQHost}):{strdata}", key);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                             $"MQTT数据处理: {wrapper.Config?.MQHost} - {ex.Message}", "MQTT", LOG_TYPE.ErrorLog);
                    }
                });
            }
        }

        public async Task<bool> MqttPublish(string publishtopic, string data)
        {
            bool isresult = false;
            try
            {
                if (mqttClients.IsZxxAny())
                {
                    var message = new MqttApplicationMessage
                    {
                        Topic = publishtopic,
                        PayloadSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)),
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                        Retain = false
                    };

                    List<MqttClientWrapper> sendMqtts = new List<MqttClientWrapper>();
                    lock (mqttLock)
                    {
                        var connectedClients = mqttClients.Where(x => x.IsConnected).ToList();
                        if (!connectedClients.IsZxxAny())
                        {
                            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "没有可用的MQTT连接", "MQTT");
                            return false;
                        }
                        sendMqtts.AddRange(connectedClients);
                    }
                    int successCount = 0;
                    foreach (var client in sendMqtts)
                    {
                        try
                        {
                            var ret = await client.Client.PublishAsync(message);
                            if (ret.IsSuccess)
                            {
                                successCount++;
                                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                                    $"广播发送成功: {client.Config.MQHost} -> {client.Config.MqRoutingKey}", "MQTT");
                            }
                            else
                            {
                                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                                    $"广播发送失败: {client.Config.MQHost} -> {client.Config.MqRoutingKey}", "MQTT");
                            }
                        }
                        catch (Exception ex)
                        {
                            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                                $"广播发送异常: {client.Config.MQHost} - {ex.Message}", "MQTT", LOG_TYPE.ErrorLog);
                        }
                    }
                    isresult = successCount > 0;
                }
                else
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        "MQTT客户端未连接，无法发布消息", "MQTT", LOG_TYPE.ErrorLog);
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
        public void GeneralDog(object? obj)
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
        public void CheckLinuxServices()
        {
            // 系统服务列表
            List<string> systemServices = new List<string>
            {
                "mysql",
                "mysqld",
                "docker"
            };

            // Docker容器列表
            List<string> dockerContainers = new List<string>();
            if (!MainSetting.Current.IsXinChuang)
            {
                dockerContainers.Add("mysql");
                dockerContainers.Add("rabbitmq");
                dockerContainers.Add("consul");
                dockerContainers.Add("nginx");
                dockerContainers.Add("redis");
                dockerContainers.Add("kkfileview");
            }
            else
            {
                dockerContainers.Add("nginx");
                dockerContainers.Add("consul");
                dockerContainers.Add("rabbitmq");
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
                                $"[kkfileview] 程序启动超时，等待 {maxWaitCount} 秒后端口 {targetPort} 仍未被占用，第 {currentAttempt} 次尝试", "常规服务看守");
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
                        $"[kkfileview] 程序已达到最大重试次数 {maxRetries}，最终启动失败", "常规服务看守");
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
                // 首先检查容器是否存在（包括所有状态的容器）
                string existsCmd = $"docker ps -a --filter \"name={containerName}\" --format \"{{{{.Names}}}}\"";
                var (existsSuccess, existsResult) = RunLinuxCmd(existsCmd);
                if (!existsSuccess || string.IsNullOrWhiteSpace(existsResult) || !existsResult.Contains(containerName))
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"Docker容器 [{containerName}] 不存在", "常规服务看守");
                    return false;
                }

                // 检查容器运行状态（只检查正在运行的容器）
                string checkCmd = $"docker ps --filter \"name={containerName}\" --format \"table {{{{.Names}}}}\\t{{{{.Status}}}}\"";
                var (checkSuccess, checkResult) = RunLinuxCmd(checkCmd);
                if (checkSuccess && checkResult.Contains("Up"))
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"Docker容器 [{containerName}] 运行正常", "常规服务看守");
                    return true;
                }

                string restartCmd = $"docker restart {containerName}";
                (restartSuccess, resultMessage) = RunLinuxCmd(restartCmd);
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                       $"Docker容器[{containerName}] 启动{(restartSuccess ? "成功" : "失败")}：{resultMessage}", "常规服务看守");
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
        public void FwqRestart(object? obj)
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
        /// 服务器重启后检查服务和端口状态
        /// </summary>
        /// <param name="obj"></param>
        private void FwqRestartCheck(object obj)
        {
            try
            {
                if (!MainSetting.Current.IsFwqRestartListen) return;
                //3分钟之后执行一次，确保别的服务启动完成。
                Task.Delay(3 * 60 * 1000).Wait();
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "开始执行服务器重启后检查", "服务器重启检查");

                // 服务端口映射配置
                Dictionary<string, int> servicePortMap = new Dictionary<string, int>();

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    // Linux系统检查
                    CheckLinuxServicesAfterRestart();
                }
                else
                {
                    // Windows系统检查  
                    CheckWindowsServicesAfterRestart();
                }

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "服务器重启后检查完成", "服务器重启检查");
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "服务器重启检查", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 检查Linux系统服务和端口状态
        /// </summary>
        private void CheckLinuxServicesAfterRestart()
        {
            try
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "开始检查Linux系统服务和端口", "服务器重启检查");

                // 系统服务配置
                Dictionary<string, int> systemServices = new Dictionary<string, int>
                {
                    { "mysqld", 6336 },
                    { "docker", 0 } // docker服务本身不监听特定端口
                };

                // Docker容器及端口配置
                Dictionary<string, int> dockerContainers = new Dictionary<string, int>();
                if (!MainSetting.Current.IsXinChuang)
                {
                    dockerContainers.Add("mysql", 6306);
                    dockerContainers.Add("rabbitmq", 1883);
                    dockerContainers.Add("consul", 8500);
                    dockerContainers.Add("nginx", 8000);
                    dockerContainers.Add("redis", 6379);
                    dockerContainers.Add("kkfileview", 8012);
                }
                else
                {
                    dockerContainers.Add("nginx", 8000);
                    dockerContainers.Add("consul", 8500);
                    dockerContainers.Add("rabbitmq", 1883);
                    dockerContainers.Add("tidb", 6336);
                    dockerContainers.Add("tendisplus", 30000);
                    dockerContainers.Add("easysearch01", 9002);
                    dockerContainers.Add("easysearch02", 9200);
                    dockerContainers.Add("esconsole", 9001);
                }

                // 检查系统服务
                foreach (var service in systemServices)
                {
                    CheckLinuxServiceAndPort(service.Key, service.Value);
                    Task.Delay(1000).Wait(); // 间隔1秒
                }

                // 检查Docker容器
                foreach (var container in dockerContainers)
                {
                    CheckDockerContainerAndPort(container.Key, container.Value);
                    Task.Delay(1000).Wait(); // 间隔1秒
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "Linux服务检查", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 检查Windows系统服务和端口状态
        /// </summary>
        private void CheckWindowsServicesAfterRestart()
        {
            try
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, "开始检查Windows系统服务和端口", "服务器重启检查");

                // Windows服务及端口配置
                Dictionary<string, int> windowsServices = new Dictionary<string, int>
                {
                    { "MySQL57", 3306 },
                    { "MySQL80", 3306 },
                    { "RabbitMQ", 1883 },
                    { "ConsulService", 8500 },
                    { "Nginx", 80 },
                    { "Redis", 6379 }
                };

                // 检查Windows服务
                foreach (var service in windowsServices)
                {
                    CheckWindowsServiceAndPort(service.Key, service.Value);
                    Task.Delay(1000).Wait(); // 间隔1秒
                }

                // 检查kkfileview程序
                CheckKkFileViewAfterRestart();
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), "Windows服务检查", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 检查Linux系统服务和对应端口
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <param name="port">端口号，0表示不检查端口</param>
        private void CheckLinuxServiceAndPort(string serviceName, int port)
        {
            try
            {
                // 检查服务状态
                var (serviceSuccess, serviceResult) = RunLinuxCmd($"systemctl is-active {serviceName}");
                bool serviceRunning = serviceSuccess && serviceResult.Trim().Equals("active", StringComparison.OrdinalIgnoreCase);

                // 检查端口状态（如果需要）
                bool portListening = true; // 默认为true，如果不需要检查端口
                if (port > 0)
                {
                    portListening = CheckLinuxPortInUse(port);
                }

                string status = serviceRunning ? "运行正常" : "服务异常";
                if (port > 0)
                {
                    status += portListening ? $"，端口{port}监听正常" : $"，端口{port}监听异常";
                }

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"系统服务 [{serviceName}] {status}", "服务器重启检查");

                // 如果服务异常或端口异常，尝试重启
                if (!serviceRunning || (port > 0 && !portListening))
                {
                    var restartAttempts = 0;
                    const int maxRestartAttempts = 3;

                    while (restartAttempts < maxRestartAttempts)
                    {
                        restartAttempts++;
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"尝试重启系统服务 [{serviceName}]，第{restartAttempts}次", "服务器重启检查");

                        var (restartSuccess, restartResult) = RunLinuxCmd($"systemctl restart {serviceName}");
                        Task.Delay(5000).Wait(); // 等待5秒让服务启动

                        // 重新检查状态
                        (serviceSuccess, serviceResult) = RunLinuxCmd($"systemctl is-active {serviceName}");
                        serviceRunning = serviceSuccess && serviceResult.Trim().Equals("active", StringComparison.OrdinalIgnoreCase);

                        if (port > 0)
                        {
                            portListening = CheckLinuxPortInUse(port);
                        }

                        if (serviceRunning && (port == 0 || portListening))
                        {
                            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                                $"系统服务 [{serviceName}] 重启成功", "服务器重启检查");
                            break;
                        }

                        if (restartAttempts >= maxRestartAttempts)
                        {
                            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"系统服务 [{serviceName}] 重启失败，已达到最大重试次数", "服务器重启检查");
                        }
                        else
                        {
                            Task.Delay(10000).Wait(); // 等待10秒后重试
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"检查系统服务 [{serviceName}] 时发生异常：{ex.Message}", "服务器重启检查", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 检查Docker容器和对应端口
        /// </summary>
        /// <param name="containerName">容器名称</param>
        /// <param name="port">端口号</param>
        private void CheckDockerContainerAndPort(string containerName, int port)
        {
            try
            {
                // 首先检查容器是否存在（包括所有状态的容器）
                string existsCmd = $"docker ps -a --filter \"name={containerName}\" --format \"{{{{.Names}}}}\"";
                var (existsSuccess, existsResult) = RunLinuxCmd(existsCmd);

                if (!existsSuccess || string.IsNullOrWhiteSpace(existsResult) || !existsResult.Contains(containerName))
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"Docker容器 [{containerName}] 不存在", "服务器重启检查");
                    return;
                }

                // 检查容器运行状态（只检查正在运行的容器）
                string checkCmd = $"docker ps --filter \"name={containerName}\" --format \"table {{{{.Names}}}}\\t{{{{.Status}}}}\"";
                var (containerSuccess, containerResult) = RunLinuxCmd(checkCmd);
                bool containerRunning = containerSuccess && containerResult.Contains("Up");

                // 检查端口状态
                bool portListening = CheckLinuxPortInUse(port);

                string status = containerRunning ? "运行正常" : "容器未运行";
                status += portListening ? $"，端口{port}监听正常" : $"，端口{port}监听异常";

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"Docker容器 [{containerName}] {status}", "服务器重启检查");

                // 如果容器异常或端口异常，尝试重启
                if (!containerRunning || !portListening)
                {
                    var restartAttempts = 0;
                    const int maxRestartAttempts = 3;

                    while (restartAttempts < maxRestartAttempts)
                    {
                        restartAttempts++;
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"尝试{(containerRunning ? "重启" : "启动")}Docker容器 [{containerName}]，第{restartAttempts}次", "服务器重启检查");

                        // 根据容器状态选择命令
                        string command = containerRunning ? $"docker restart {containerName}" : $"docker start {containerName}";
                        var (restartSuccess, restartResult) = RunLinuxCmd(command);
                        Task.Delay(10000).Wait(); // 等待10秒让容器启动

                        // 重新检查状态
                        (containerSuccess, containerResult) = RunLinuxCmd(checkCmd);
                        containerRunning = containerSuccess && containerResult.Contains("Up");
                        portListening = CheckLinuxPortInUse(port);

                        if (containerRunning && portListening)
                        {
                            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                                $"Docker容器 [{containerName}] 启动成功", "服务器重启检查");
                            break;
                        }

                        if (restartAttempts >= maxRestartAttempts)
                        {
                            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"Docker容器 [{containerName}] 启动失败，已达到最大重试次数", "服务器重启检查");

                            // 特别处理RabbitMQ的1883端口问题
                            if (containerName.ToLower().Contains("rabbitmq") && port == 1883)
                            {
                                HandleRabbitMqPortIssue(containerName);
                            }
                        }
                        else
                        {
                            Task.Delay(15000).Wait(); // 等待15秒后重试
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"检查Docker容器 [{containerName}] 时发生异常：{ex.Message}", "服务器重启检查", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 检查Windows服务和对应端口
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <param name="port">端口号</param>
        private void CheckWindowsServiceAndPort(string serviceName, int port)
        {
            try
            {
                // 检查服务状态
                var (serviceSuccess, serviceResult) = RunWindowCmd($"sc query \"{serviceName}\"");
                bool serviceRunning = serviceSuccess && serviceResult.Contains("RUNNING");

                // 检查端口状态
                bool portListening = CheckPortInUse(port);

                string status = serviceRunning ? "运行正常" : "服务异常";
                status += portListening ? $"，端口{port}监听正常" : $"，端口{port}监听异常";

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"Windows服务 [{serviceName}] {status}", "服务器重启检查");

                // 如果服务异常或端口异常，尝试重启
                if (!serviceRunning || !portListening)
                {
                    var restartAttempts = 0;
                    const int maxRestartAttempts = 3;

                    while (restartAttempts < maxRestartAttempts)
                    {
                        restartAttempts++;
                        ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                            $"尝试重启Windows服务 [{serviceName}]，第{restartAttempts}次", "服务器重启检查");

                        var (restartSuccess, restartResult) = RunWindowCmd($"net stop \"{serviceName}\" & net start \"{serviceName}\"");
                        Task.Delay(10000).Wait(); // 等待10秒让服务启动

                        // 重新检查状态
                        (serviceSuccess, serviceResult) = RunWindowCmd($"sc query \"{serviceName}\"");
                        serviceRunning = serviceSuccess && serviceResult.Contains("RUNNING");
                        portListening = CheckPortInUse(port);

                        if (serviceRunning && portListening)
                        {
                            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                                $"Windows服务 [{serviceName}] 重启成功", "服务器重启检查");
                            break;
                        }

                        if (restartAttempts >= maxRestartAttempts)
                        {
                            ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"Windows服务 [{serviceName}] 重启失败，已达到最大重试次数", "服务器重启检查");
                        }
                        else
                        {
                            Task.Delay(15000).Wait(); // 等待15秒后重试
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"检查Windows服务 [{serviceName}] 时发生异常：{ex.Message}", "服务器重启检查", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 检查kkfileview程序重启后状态
        /// </summary>
        private void CheckKkFileViewAfterRestart()
        {
            try
            {
                int targetPort = 8012;
                bool isPortInUse = CheckPortInUse(targetPort);

                if (isPortInUse)
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"kkfileview程序运行正常，端口 {targetPort} 监听正常", "服务器重启检查");
                }
                else
                {
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName, $"kkfileview程序异常，端口 {targetPort} 未监听", "服务器重启检查");

                    // 尝试启动kkfileview
                    KkFileViewDog(MainSetting.Current.WinKkfileviewPath);
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"检查kkfileview程序时发生异常：{ex.Message}", "服务器重启检查", LOG_TYPE.ErrorLog);
            }
        }

        /// <summary>
        /// 检查Linux系统端口是否被占用
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>true表示端口被占用，false表示端口空闲</returns>
        private bool CheckLinuxPortInUse(int port)
        {
            try
            {
                // 使用netstat命令检查端口占用情况
                var (success, result) = RunLinuxCmd($"netstat -tln | grep :{port}");
                if (success && !string.IsNullOrWhiteSpace(result))
                {
                    // 检查结果中是否包含LISTEN状态
                    return result.Contains("LISTEN");
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"检查Linux端口 {port} 占用情况时发生异常：{ex.Message}", "端口检查", LOG_TYPE.ErrorLog);
                return false;
            }
        }

        /// <summary>
        /// 处理RabbitMQ的1883端口问题
        /// </summary>
        /// <param name="containerName">容器名称</param>
        private void HandleRabbitMqPortIssue(string containerName)
        {
            try
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"开始处理RabbitMQ容器 [{containerName}] 的1883端口问题", "RabbitMQ端口修复");

                // 进入容器执行rabbitmq-plugins命令启用mqtt插件
                var commands = new List<string>
                {
                    $"docker exec {containerName} rabbitmq-plugins enable rabbitmq_mqtt",
                    $"docker exec {containerName} rabbitmq-plugins enable rabbitmq_web_mqtt",
                    $"docker exec {containerName} rabbitmqctl restart"
                };

                foreach (var cmd in commands)
                {
                    var (success, result) = RunLinuxCmd(cmd);
                    ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                        $"执行命令 [{cmd}] {(success ? "成功" : "失败")}：{result}", "RabbitMQ端口修复");

                    Task.Delay(2000).Wait(); // 每个命令间隔2秒
                }

                // 等待一段时间后再次检查端口
                Task.Delay(10000).Wait();
                bool portFixed = CheckLinuxPortInUse(1883);

                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"RabbitMQ的1883端口修复{(portFixed ? "成功" : "失败")}", "RabbitMQ端口修复");

                if (!portFixed)
                {
                    // 发送短信通知
                    var _NoteInfo = new
                    {
                        NoteContent = $"RabbitMQ容器{containerName}的1883端口启动失败，需要人工处理",
                        SendTime = DateTime.Now.AddSeconds(30),
                        AppCode = "服务器监控"
                    };
                    SmsNoteJy(_NoteInfo.ToJson());
                }
            }
            catch (Exception ex)
            {
                ConsleWrite.ConsleWriteLine(ClassHelper.ClassName, ClassHelper.MethodName,
                    $"处理RabbitMQ端口问题时发生异常：{ex.Message}", "RabbitMQ端口修复", LOG_TYPE.ErrorLog);
            }
        }


        #endregion

    }
}
