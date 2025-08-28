using System;
using SqlSugar;
using System.ComponentModel;

namespace IdentityServerModel
{
    /// <summary>
    /// 菜单按钮
    ///</summary>
    [DisplayName("菜单按钮")]
    [SugarTable("moduleelement")]
    public class Moduleelement
    {
		/// <summary>
		/// 流水号
		///</summary>
		[DisplayName("流水号")]
        [SugarColumn(ColumnName = "Id", IsPrimaryKey = true)]
		public string Id { get; set; }
		/// <summary>
		/// DOM ID
		///</summary>
		[DisplayName("DOM ID")]
        [SugarColumn(ColumnName = "DomId", Length = 255)]
		public string DomId { get; set; }
		/// <summary>
		/// 名称
		///</summary>
		[DisplayName("名称")]
        [SugarColumn(ColumnName = "Name", Length = 255)]
		public string Name { get; set; }
		/// <summary>
		/// 元素附加属性
		///</summary>
		[DisplayName("元素附加属性")]
        [SugarColumn(ColumnName = "Attr", ColumnDataType = "text")]         
		public string Attr { get; set; }
		/// <summary>
		/// 元素调用脚本
		///</summary>
		[DisplayName("元素调用脚本")]
        [SugarColumn(ColumnName = "Script", ColumnDataType = "text")]         
		public string Script { get; set; }
		/// <summary>
		/// 元素图标
		///</summary>
		[DisplayName("元素图标")]
        [SugarColumn(ColumnName = "Icon", Length = 255)]
		public string Icon { get; set; }
		/// <summary>
		/// 元素样式
		///</summary>
		[DisplayName("元素样式")]
        [SugarColumn(ColumnName = "Class", Length = 255)]
		public string Class { get; set; }
		/// <summary>
		/// 备注
		///</summary>
		[DisplayName("备注")]
        [SugarColumn(ColumnName = "Remark", Length = 200)]
		public string Remark { get; set; }
		/// <summary>
		/// 排序字段
		///</summary>
		[DisplayName("排序字段")]
        [SugarColumn(ColumnName = "Sort")]
		public int Sort { get; set; }
		/// <summary>
		/// 功能模块Id
		///</summary>
		[DisplayName("功能模块Id")]
        [SugarColumn(ColumnName = "ModuleId", Length = 50)]
		public string ModuleId { get; set; }
		/// <summary>
		/// 分类名称
		///</summary>
		[DisplayName("分类名称")]
        [SugarColumn(ColumnName = "TypeName", IsNullable = true, Length = 20)]
		public string TypeName { get; set; }
		/// <summary>
		/// 分类ID
		///</summary>
		[DisplayName("分类ID")]
        [SugarColumn(ColumnName = "TypeId", IsNullable = true, Length = 50)]
		public string TypeId { get; set; }
	}
}