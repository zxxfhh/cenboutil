using NewLife.Configuration;
using System.ComponentModel;

namespace CenboNew.ServiceLog
{

    /// <summary>圣博日志配置</summary>
    [Description("圣博日志配置")]
    [Config("Config/LogSetting.config")]
    public class LogSetting : Config<LogSetting>
    {
        /// <summary>日志保留个数</summary>
        [Description("日志保留个数")]
        public int FileSaveNum { get; set; } = 3;

        /// <summary>日志保留个数</summary>
        [Description("日志文件类型(1:db,2:txt)")]
        public int FileType { get; set; } = 1;

        /// <summary>批量入库缓存时间(毫秒)</summary>
        [Description("批量入库缓存时间(毫秒)")]
        public int DbCacheTime { get; set; } = 1500;

        /// <summary>数据库名称</summary>
        [Description("数据库名称")]
        public string DBName { get; set; } = "";

        /// <summary>数据库路径</summary>
        [Description("数据库路径")]
        public string DBPath { get; set; } = "";

        /// <summary>是否启用其他文件夹</summary>
        [Description("是否启用其他文件夹")]
        public bool IsOtherDir { get; set; } = false;

        /// <summary>日志路径文件夹</summary>
        [Description("日志路径文件夹")]
        public string LogPathDir { get; set; } = "LogDB";

    }
}
