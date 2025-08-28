using System;
using System.ComponentModel;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace CenboNew.ServiceLog
{
    /// <summary>错误日志</summary>
    [Serializable]
    [DataObject]
    [Description("错误日志")]
    [BindIndex("PRIMARY", true, "snowId")]
    [BindTable("errorlog", Description = "错误日志", ConnName = "ConnName", DbType = DatabaseType.SQLite)]
    public partial class ErrorLog
    {
        #region 属性
        private Int64 _snowId;
        /// <summary>雪花主键</summary>
        [DisplayName("雪花主键")]
        [Description("雪花主键")]
        [DataObjectField(true, false, false, 20)]
        [BindColumn("snowId", "雪花主键", "bigint(20)")]
        public Int64 snowId { get => _snowId; set { if (OnPropertyChanging("snowId", value)) { _snowId = value; OnPropertyChanged("snowId"); } } }

        private String _createTime;
        /// <summary>写入时间</summary>
        [DisplayName("写入时间")]
        [Description("写入时间")]
        [DataObjectField(false, false, true, 30)]
        [BindColumn("createTime", "写入时间", "varchar(30)")]
        public String createTime { get => _createTime; set { if (OnPropertyChanging("createTime", value)) { _createTime = value; OnPropertyChanged("createTime"); } } }

        private String _className;
        /// <summary>类名</summary>
        [DisplayName("类名")]
        [Description("类名")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("className", "类名", "varchar(200)")]
        public String className { get => _className; set { if (OnPropertyChanging("className", value)) { _className = value; OnPropertyChanged("className"); } } }

        private String _methodName;
        /// <summary>方法名</summary>
        [DisplayName("方法名")]
        [Description("方法名")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("methodName", "方法名", "varchar(200)")]
        public String methodName { get => _methodName; set { if (OnPropertyChanging("methodName", value)) { _methodName = value; OnPropertyChanged("methodName"); } } }

        private String _dataType;
        /// <summary>通讯类型</summary>
        [DisplayName("通讯类型")]
        [Description("通讯类型")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("dataType", "通讯类型", "varchar(200)")]
        public String dataType { get => _dataType; set { if (OnPropertyChanging("dataType", value)) { _dataType = value; OnPropertyChanged("dataType"); } } }

        private String _logMessage;
        /// <summary>日志内容</summary>
        [DisplayName("日志内容")]
        [Description("日志内容")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("logMessage", "日志内容", "text")]
        public String logMessage { get => _logMessage; set { if (OnPropertyChanging("logMessage", value)) { _logMessage = value; OnPropertyChanged("logMessage"); } } }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public override Object this[String name]
        {
            get
            {
                switch (name)
                {
                    case "snowId": return _snowId;
                    case "createTime": return _createTime;
                    case "className": return _className;
                    case "methodName": return _methodName;
                    case "dataType": return _dataType;
                    case "logMessage": return _logMessage;
                }
                return base[name];
            }
            set
            {
                switch (name)
                {
                    case "snowId": _snowId = value.ToLong(); break;
                    case "createTime": _createTime = Convert.ToString(value); break;
                    case "className": _className = Convert.ToString(value); break;
                    case "methodName": _methodName = Convert.ToString(value); break;
                    case "dataType": _dataType = Convert.ToString(value); break;
                    case "logMessage": _logMessage = Convert.ToString(value); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 关联映射
        #endregion

        #region 字段名
        /// <summary>取得错误日志字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>雪花主键</summary>
            public static readonly Field snowId = FindByName("snowId");

            /// <summary>写入时间</summary>
            public static readonly Field createTime = FindByName("createTime");

            /// <summary>类名</summary>
            public static readonly Field className = FindByName("className");

            /// <summary>方法名</summary>
            public static readonly Field methodName = FindByName("methodName");

            /// <summary>通讯类型</summary>
            public static readonly Field dataType = FindByName("dataType");

            /// <summary>日志内容</summary>
            public static readonly Field logMessage = FindByName("logMessage");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得错误日志字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>雪花主键</summary>
            public const String snowId = "snowId";

            /// <summary>写入时间</summary>
            public const String createTime = "createTime";

            /// <summary>类名</summary>
            public const String className = "className";

            /// <summary>方法名</summary>
            public const String methodName = "methodName";

            /// <summary>通讯类型</summary>
            public const String dataType = "dataType";

            /// <summary>日志内容</summary>
            public const String logMessage = "logMessage";
        }
        #endregion
    }
}