using SqlSugar;

namespace ApiModel
{
    public class DbContext
    {
        public string sqlSugar = "";
        public string sqlError = "";

        public SqlSugarScope Db = null;

        public DbContext(string connection, DbType dbType = DbType.MySql)
        {
            Db = new SqlSugarScope(new ConnectionConfig()
            {
                ConnectionString = connection,
                DbType = dbType,
                InitKeyType = InitKeyType.Attribute,  //从特性读取主键和自增列信息
                IsAutoCloseConnection = true,
                MoreSettings = new ConnMoreSettings()
                {
                    IsNoReadXmlDescription = true,  //禁止读取XML中备注,true是禁用
                    SqlServerCodeFirstNvarchar = true,
                    SqliteCodeFirstEnableDefaultValue = true, //Sqlite启用默认值
                    SqliteCodeFirstEnableDescription = true, //Sqlite启用备注
                    SqliteCodeFirstEnableDropColumn = true,  //Sqlite启用删除列
                    EnableCodeFirstUpdatePrecision = true  //启用decimal和double类型精度修改
                },
                ConfigureExternalServices = new ConfigureExternalServices()
                {
                    EntityService = (x, p) => //处理列名
                    {
                        p.DbColumnName = UtilMethods.ToUnderLine(p.DbColumnName);//驼峰转下划线方法
                    },
                    EntityNameService = (x, p) => //处理表名
                    {
                        p.DbTableName = UtilMethods.ToUnderLine(p.DbTableName);//驼峰转下划线方法
                    }
                }
            }, db =>
            {
                //SQL执行完
                db.Aop.OnLogExecuted = (sql, pars) =>
                {
                    sqlError = "";
                    sqlSugar = SugarSqlFormat.FormatParam(sql, pars);
                };
                //SQL报错
                db.Aop.OnError = (exp) =>
                {
                    sqlSugar = "";
                    sqlError = SugarSqlFormat.FormatParam(exp.Sql, exp.Parametres);
                };
            });
        }

    }

}
