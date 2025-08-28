using System;
using SqlSugar;
using System.ComponentModel;

namespace IdentityServerModel
{
    /// <summary>
    /// 客户端配置表
    ///</summary>
    [DisplayName("客户端配置表")]
    [SugarTable("clientresources")]
    public class Clientresources
    {
		/// <summary>
		/// 维一编码
		///</summary>
		[DisplayName("维一编码")]
        [SugarColumn(ColumnName = "SerialCode", IsPrimaryKey = true)]
		public string SerialCode { get; set; }
		/// <summary>
		/// 客户端ID
		///</summary>
		[DisplayName("客户端ID")]
        [SugarColumn(ColumnName = "ClientId", IsNullable = true, Length = 30)]
		public string ClientId { get; set; }
		/// <summary>
		/// 客户端名称
		///</summary>
		[DisplayName("客户端名称")]
        [SugarColumn(ColumnName = "ClientName", IsNullable = true, Length = 60)]
		public string ClientName { get; set; }
		/// <summary>
		/// 客户端密钥
		///</summary>
		[DisplayName("客户端密钥")]
        [SugarColumn(ColumnName = "ClientSecrets", IsNullable = true, Length = 60)]
		public string ClientSecrets { get; set; }
		/// <summary>
		/// Token过期时数
		///</summary>
		[DisplayName("Token过期时数")]
        [SugarColumn(ColumnName = "TokenLifeTime")]
		public int TokenLifeTime { get; set; }
		/// <summary>
		/// 登录回调URl集合(|)
		///</summary>
		[DisplayName("登录回调URl集合(|)")]
        [SugarColumn(ColumnName = "RedirectUris", IsNullable = true, ColumnDataType = "text")]         
		public string RedirectUris { get; set; }
		/// <summary>
		/// 注销回调URl集合(|)
		///</summary>
		[DisplayName("注销回调URl集合(|)")]
        [SugarColumn(ColumnName = "PostLogoutRedirectUris", IsNullable = true, ColumnDataType = "text")]         
		public string PostLogoutRedirectUris { get; set; }
		/// <summary>
		/// 允许站点集合(|)
		///</summary>
		[DisplayName("允许站点集合(|)")]
        [SugarColumn(ColumnName = "AllowedCorsOrigins", IsNullable = true, ColumnDataType = "text")]         
		public string AllowedCorsOrigins { get; set; }
		/// <summary>
		/// API作用范围集合(|)
		///</summary>
		[DisplayName("API作用范围集合(|)")]
        [SugarColumn(ColumnName = "AllowedScopes", IsNullable = true, ColumnDataType = "text")]         
		public string AllowedScopes { get; set; }
		/// <summary>
		/// 是否启用(1:启用 0:禁用)
		///</summary>
		[DisplayName("是否启用(1:启用 0:禁用)")]
        [SugarColumn(ColumnName = "IsEnable")]
		public int IsEnable { get; set; }
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
        [SugarColumn(ColumnName = "CreateTime", IsNullable = true, Length = 20)]
		public string CreateTime { get; set; }
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
        [SugarColumn(ColumnName = "UpdateTime", IsNullable = true, Length = 20)]
		public string UpdateTime { get; set; }
		/// <summary>
		/// 修改用户名称
		///</summary>
		[DisplayName("修改用户名称")]
        [SugarColumn(ColumnName = "UpdateName", IsNullable = true, Length = 50)]
		public string UpdateName { get; set; }
	}
}