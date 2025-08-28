using NewLife.Configuration;
using System;
using System.ComponentModel;

namespace IdentityServerModel
{

    /// <summary>DB配置</summary>
    [Description("DB配置")]
    [Config("Config/DbSetting.config")]
    public class DbSetting : Config<DbSetting>
    {
        /// <summary>Mysql数据库连接字符串</summary>
        [Description("Mysql数据库连接字符串")]
        public String MysqlConString { get; set; } = "Server=120.26.243.159;Port=4006;Database=cenbo.account;UserId=root;Password=cenboaliyunlinux;CharSet=utf8;SslMode=None;";

        /// <summary>Sqlite数据库连接字符串</summary>
        [Description("Sqlite数据库连接字符串")]
        public String SqliteConString { get; set; } = "DataSource=D:/DRMAdb.db";
    }
}
