using NewLife.Log;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CenboNew.ServiceLog
{
    public class ConsleWrite
    {
        #region 控制台和日志输出

        private static ConcurrentDictionary<Int32, ConsoleColor> dic = new ConcurrentDictionary<Int32, ConsoleColor>();
        private static ConsoleColor[] colors = new ConsoleColor[] {
            ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.Yellow,
            ConsoleColor.DarkGreen, ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta, ConsoleColor.DarkRed, ConsoleColor.DarkYellow };

        /// <summary>
        /// 控制台和日志输出
        /// </summary>
        /// <param name="msg">文本</param>
        /// <param name="logtype">日志类型</param>
        public static void ConsleWriteLine(string className, string methodName, string msg, string datatype = "接收", LOG_TYPE logtype = LOG_TYPE.SysLog)
        {
            // 获取当前操作系统信息
            OperatingSystem os = Environment.OSVersion;
            if (os.Platform == PlatformID.Win32NT)
            {
                int _initing = 1;
                Task.Run(() =>
                {

                    try
                    {
                        _initing = Thread.CurrentThread.ManagedThreadId; //线程ID
                    }
                    catch (Exception) { }

                    try
                    {
                        string strmsg = $"{DateTime.Now.ToString("HH:mm:ss.fff")}  {datatype}[{methodName}]：{msg}";
                        if (logtype == LOG_TYPE.SysLog)
                        {
                            Console.ForegroundColor = GetColor(_initing);
                            LogHelper.SysLogWrite(className, methodName, msg, datatype);

                        }
                        else if (logtype == LOG_TYPE.ErrorLog)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            LogHelper.ErrorLogWrite(className, methodName, msg, datatype);
                        }
                        Console.Write(strmsg + "\n");

                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);
                    }
                });
            }
            else
            {
                if (logtype == LOG_TYPE.SysLog)
                {
                    LogHelper.SysLogWrite(className, methodName, msg, datatype);

                }
                else if (logtype == LOG_TYPE.ErrorLog)
                {
                    LogHelper.ErrorLogWrite(className, methodName, msg, datatype);
                }
            }
        }

        public static ConsoleColor GetColor(Int32 threadid)
        {
            if (threadid == 1) return ConsoleColor.White;

            try
            {
                return dic.GetOrAdd(threadid, k => colors[dic.Count % colors.Length]);
            }
            catch (Exception)
            {
            }
            return ConsoleColor.White;
        }

        #endregion
    }

    public class ConsleLog
    {
        public string className = "";
        public string methodName = "";
        public string msg = "";
        public string datatype = "接收";
        public byte[] response = null;
        public int waittime = 3 * 1000;
    }

    public enum LOG_TYPE
    {
        SysLog,
        ErrorLog
    }

}
