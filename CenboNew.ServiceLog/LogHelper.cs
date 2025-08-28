using NewLife.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XCode;
using XCode.DataAccessLayer;

namespace CenboNew.ServiceLog
{
    public class LogHelper
    {
        private static SnowModelLog snow = new SnowModelLog();
        private static bool isdb = false;
        private static string ConnName = "";
        private static string TableNameSysLog = "";
        private static string TableNameErrorLog = "";
        private static void SetConnName()
        {
            string dbdir = "LogDB";
            string dirpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbdir);
            if (LogSetting.Current.IsOtherDir)
            {
                dirpath = LogSetting.Current.LogPathDir;
            }
            if (!Directory.Exists(dirpath)) Directory.CreateDirectory(dirpath);

            DateTime dt = DateTime.Now;
            string oldFilename = $"Log_{dt.AddMonths(-LogSetting.Current.FileSaveNum):yyMM}.db";
            string oldfilepath = Path.Combine(dirpath, oldFilename);
            File.Delete(oldfilepath);

            ConnName = $"Log_{dt:yyMM}";
            string filename = Path.Combine(dirpath, ConnName + ".db");
            lock (LogSetting.Current)
            {
                LogSetting.Current.DBName = ConnName;
                LogSetting.Current.DBPath = filename;
                LogSetting.Current.Save();
            }
            DAL.AddConnStr(ConnName, $"Data Source={filename}", null, "sqlite");
            isdb = true;
        }

        private static void Init(LOG_TYPE logtype)
        {
            taskhmtime = LogSetting.Current.DbCacheTime;

            if (!File.Exists(LogSetting.Current.DBPath) || ConnName != LogSetting.Current.DBName)
            {
                SetConnName();
            }

            DateTime dt = DateTime.Now;
            if (ConnName != $"Log_{dt:yyMM}") isdb = false;

            int index = 0;
            while (!isdb)
            {
                Task.Delay(100).Wait();
                SetConnName();
                index++;
                if (index > 11) return;
            }

            string _TableNameSysLog = $"SysLog{dt:dd}";
            if (logtype == LOG_TYPE.SysLog)
            {
                if (TableNameSysLog != _TableNameSysLog)
                {
                    var _dal = DAL.Create(ConnName);
                    _dal.CheckDatabase();
                    TableNameSysLog = _TableNameSysLog;
                    string sql = $"CREATE TABLE IF NOT EXISTS {TableNameSysLog}(snowId BIGINT(20) PRIMARY KEY,createTime VARCHAR(30),className VARCHAR(200), methodName VARCHAR(200),dataType VARCHAR(200),logMessage TEXT);";
                    _dal.Execute(sql);
                    _dal.Session.Dispose();
                }
            }

            string _TableNameErrorLog = $"ErrorLog{dt:dd}";
            if (logtype == LOG_TYPE.ErrorLog)
            {
                if (TableNameErrorLog != _TableNameErrorLog)
                {
                    var _dal = DAL.Create(ConnName);
                    _dal.CheckDatabase();
                    TableNameErrorLog = _TableNameErrorLog;
                    string sql = $"CREATE TABLE IF NOT EXISTS {TableNameErrorLog}(snowId BIGINT(20) PRIMARY KEY,createTime VARCHAR(30),className VARCHAR(200), methodName VARCHAR(200),dataType VARCHAR(200),logMessage TEXT);";
                    _dal.Execute(sql);
                    _dal.Session.Dispose();
                }
            }
        }

        private static int taskhmtime = 1000;
        private static CancellationTokenSource _ctsSysLog = new CancellationTokenSource();
        private static object SysLogLock = new object();
        private static List<SysLog> SysLogList = new List<SysLog>();

        private static void AddSysLog(SysLog log)
        {
            lock (SysLogLock)
            {
                SysLogList.Add(log);
                if (SysLogList.Count == 1) // 如果集合之前为空，则启动定时器
                {
                    Task.Delay(taskhmtime, _ctsSysLog.Token).ContinueWith(_ => DbSysLogRuKu(), _ctsSysLog.Token);
                }
            }
        }
        private static void DbSysLogRuKu()
        {
            try
            {
                List<SysLog> list = new List<SysLog>();
                lock (SysLogLock)
                {
                    list.AddRange(SysLogList);
                    SysLogList.Clear();
                }
                if (list.Count > 0)
                {
                    Init(LOG_TYPE.SysLog);
                    SysLog.Meta.ConnName = ConnName;
                    SysLog.Meta.TableName = TableNameSysLog;
                    list.Save();
                }
            }
            catch (Exception) { }
        }

        private static CancellationTokenSource _ctsErrorLog = new CancellationTokenSource();
        private static object ErrorLogLock = new object();
        private static List<ErrorLog> ErrorLogList = new List<ErrorLog>();
        private static void AddErrorLog(ErrorLog log)
        {
            lock (ErrorLogLock)
            {
                ErrorLogList.Add(log);
                if (ErrorLogList.Count == 1) // 如果集合之前为空，则启动定时器
                {
                    Task.Delay(taskhmtime, _ctsErrorLog.Token).ContinueWith(_ => DbErrorLogRuKu(), _ctsErrorLog.Token);
                }
            }
        }
        private static void DbErrorLogRuKu()
        {
            try
            {
                List<ErrorLog> list = new List<ErrorLog>();
                lock (ErrorLogLock)
                {
                    list.AddRange(ErrorLogList);
                    ErrorLogList.Clear();
                }
                if (list.Count > 0)
                {
                    Init(LOG_TYPE.ErrorLog);
                    ErrorLog.Meta.ConnName = ConnName;
                    ErrorLog.Meta.TableName = TableNameErrorLog;
                    list.Save();
                }
            }
            catch (Exception) { }
        }

        public static void SysLogWrite(string className, string methodName, string logMessage, string datatype)
        {
            try
            {
                //using var split = SysLog.Meta.CreateSplit(connName, tablename);
                //SysLog.Meta.TableName = tablename;
                //SysLog.Meta.ConnName = connName;
                //entity.SaveAsync();

                if (LogSetting.Current.FileType == 1)
                {
                    var entity = new SysLog
                    {
                        snowId = snow.NewId(),
                        className = className,
                        dataType = datatype,
                        methodName = methodName,
                        createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        logMessage = logMessage
                    };
                    AddSysLog(entity);
                }
                else if (LogSetting.Current.FileType == 2)
                {
                    string text = datatype + "[" + methodName + "]：" + logMessage;
                    XTrace.WriteLine(text);
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        public static void ErrorLogWrite(string className, string methodName, string logMessage, string datatype)
        {
            try
            {
                if (LogSetting.Current.FileType == 1)
                {
                    var entity = new ErrorLog
                    {
                        snowId = snow.NewId(),
                        className = className,
                        dataType = datatype,
                        methodName = methodName,
                        createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        logMessage = logMessage
                    };
                    AddErrorLog(entity);
                }
                else if (LogSetting.Current.FileType == 2)
                {
                    string text = datatype + "[" + methodName + "]：" + logMessage;
                    Exception ex = new Exception(text);
                    XTrace.WriteException(ex);
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

    }
}
