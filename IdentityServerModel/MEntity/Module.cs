using System;
using SqlSugar;
using System.ComponentModel;

namespace IdentityServerModel
{
    /// <summary>
    /// 菜单表
    ///</summary>
    [DisplayName("菜单表")]
    [SugarTable("module")]
    public class Module
    {
		/// <summary>
		/// 功能模块流水号
		///</summary>
		[DisplayName("功能模块流水号")]
        [SugarColumn(ColumnName = "Id", IsPrimaryKey = true)]
		public string Id { get; set; }
		/// <summary>
		/// 节点语义ID
		///</summary>
		[DisplayName("节点语义ID")]
        [SugarColumn(ColumnName = "CascadeId", Length = 255)]
		public string CascadeId { get; set; }
		/// <summary>
		/// 功能模块名称
		///</summary>
		[DisplayName("功能模块名称")]
        [SugarColumn(ColumnName = "Name", Length = 255)]
		public string Name { get; set; }
		/// <summary>
		/// 主页面URL
		///</summary>
		[DisplayName("主页面URL")]
        [SugarColumn(ColumnName = "Url", Length = 255)]
		public string Url { get; set; }
		/// <summary>
		/// 是否叶子节点
		///</summary>
		[DisplayName("是否叶子节点")]
        [SugarColumn(ColumnName = "IsLeaf", Length = 4)]
		public byte IsLeaf { get; set; }
		/// <summary>
		/// 是否自动展开
		///</summary>
		[DisplayName("是否自动展开")]
        [SugarColumn(ColumnName = "IsAutoExpand", Length = 4)]
		public byte IsAutoExpand { get; set; }
		/// <summary>
		/// 节点图标文件名称
		///</summary>
		[DisplayName("节点图标文件名称")]
        [SugarColumn(ColumnName = "IconName", Length = 255)]
		public string IconName { get; set; }
		/// <summary>
		/// 当前状态
		///</summary>
		[DisplayName("当前状态")]
        [SugarColumn(ColumnName = "Status")]
		public int Status { get; set; }
		/// <summary>
		/// 父节点名称
		///</summary>
		[DisplayName("父节点名称")]
        [SugarColumn(ColumnName = "ParentName", Length = 255)]
		public string ParentName { get; set; }
		/// <summary>
		/// 矢量图标
		///</summary>
		[DisplayName("矢量图标")]
        [SugarColumn(ColumnName = "Vector", Length = 255)]
		public string Vector { get; set; }
		/// <summary>
		/// 排序号
		///</summary>
		[DisplayName("排序号")]
        [SugarColumn(ColumnName = "SortNo")]
		public int SortNo { get; set; }
		/// <summary>
		/// 父节点流水号
		///</summary>
		[DisplayName("父节点流水号")]
        [SugarColumn(ColumnName = "ParentId", IsNullable = true, Length = 50)]
		public string ParentId { get; set; }
		/// <summary>
		/// 模块标识
		///</summary>
		[DisplayName("模块标识")]
        [SugarColumn(ColumnName = "Code", IsNullable = true, Length = 50)]
		public string Code { get; set; }
		/// <summary>
		/// 是否为系统模块
		///</summary>
		[DisplayName("是否为系统模块")]
        [SugarColumn(ColumnName = "IsSys", Length = 4)]
		public byte IsSys { get; set; }
	}
}