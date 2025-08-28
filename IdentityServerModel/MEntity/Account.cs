using SqlSugar;
using System.ComponentModel;

namespace IdentityServerModel
{
    /// <summary>
    /// 用户信息表
    ///</summary>
    [DisplayName("用户信息表")]
    [SugarTable("account")]
    public class Account
    {
        /// <summary>
        /// 用户ID
        ///</summary>
        [DisplayName("用户ID")]
        [SugarColumn(ColumnName = "UserSnowId", IsPrimaryKey = true)]
        public long UserSnowId { get; set; }
        /// <summary>
        /// 账号
        ///</summary>
        [DisplayName("账号")]
        [SugarColumn(ColumnName = "UserUid", Length = 20)]
        public string UserUid { get; set; }
        /// <summary>
        /// 昵称
        ///</summary>
        [DisplayName("昵称")]
        [SugarColumn(ColumnName = "TrueName", IsNullable = true, Length = 32)]
        public string TrueName { get; set; }
        /// <summary>
        /// 登录密码
        ///</summary>
        [DisplayName("登录密码")]
        [SugarColumn(ColumnName = "Password", Length = 60)]
        public string Password { get; set; }
        /// <summary>
        /// 登录次数
        ///</summary>
        [DisplayName("登录次数")]
        [SugarColumn(ColumnName = "LoginCount")]
        public int LoginCount { get; set; }
        /// <summary>
        /// 上次登录时间
        ///</summary>
        [DisplayName("上次登录时间")]
        [SugarColumn(ColumnName = "LastLoginTime", IsNullable = true, Length = 20)]
        public string LastLoginTime { get; set; }
        /// <summary>
        /// 是否启用(1:启用 0:禁用)
        ///</summary>
        [DisplayName("是否启用(1:启用 0:禁用)")]
        [SugarColumn(ColumnName = "IsEnable")]
        public int IsEnable { get; set; }
        /// <summary>
        /// 是否删除(1:正常 0:删除)
        ///</summary>
        [DisplayName("是否删除(1:正常 0:删除)")]
        [SugarColumn(ColumnName = "IsDelete")]
        public int IsDelete { get; set; }
        /// <summary>
        /// 是否在线(1:在线 0:不在线)
        ///</summary>
        [DisplayName("是否在线(1:在线 0:不在线)")]
        [SugarColumn(ColumnName = "OnlineState")]
        public int OnlineState { get; set; }
        /// <summary>
        /// 上次退出时间
        ///</summary>
        [DisplayName("上次退出时间")]
        [SugarColumn(ColumnName = "LastOutTime", IsNullable = true, Length = 20)]
        public string LastOutTime { get; set; }
        /// <summary>
        /// 创建用户ID
        ///</summary>
        [DisplayName("创建用户ID")]
        [SugarColumn(ColumnName = "CreateId")]
        public int CreateId { get; set; }
        /// <summary>
        /// 创建时间
        ///</summary>
        [DisplayName("创建时间")]
        [SugarColumn(ColumnName = "CreateTime")]
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 创建用户名称
        ///</summary>
        [DisplayName("创建用户名称")]
        [SugarColumn(ColumnName = "CreateName", IsNullable = true, Length = 50)]
        public string CreateName { get; set; }
        /// <summary>
        /// 修改用户ID
        ///</summary>
        [DisplayName("修改用户ID")]
        [SugarColumn(ColumnName = "UpdateId")]
        public int UpdateId { get; set; }
        /// <summary>
        /// 修改时间
        ///</summary>
        [DisplayName("修改时间")]
        [SugarColumn(ColumnName = "UpdateTime")]
        public DateTime UpdateTime { get; set; }
        /// <summary>
        /// 修改用户名称
        ///</summary>
        [DisplayName("修改用户名称")]
        [SugarColumn(ColumnName = "UpdateName", IsNullable = true, Length = 50)]
        public string UpdateName { get; set; }
        /// <summary>
        /// 备注
        ///</summary>
        [DisplayName("备注")]
        [SugarColumn(ColumnName = "UserRemark", IsNullable = true, Length = 300)]
        public string UserRemark { get; set; }
    }
}