using System;
using SqlSugar;
using System.ComponentModel;

namespace ApiModel
{
    /// <summary>
    /// 签到检测设置
    ///</summary>
    [DisplayName("签到检测设置")]
    [SugarTable(TableName = "event_sign_set_cc", TableDescription = "签到检测设置", IsDisabledUpdateAll = true)]
    public class EventSignSet
    {
		/// <summary>
		/// 自增ID
		///</summary>
		[DisplayName("自增ID")]
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "自增ID", DefaultValue = "", ColumnDataType = "int")]         
		public int Id { get; set; }
		/// <summary>
		/// 星期集合(1-7,|隔开)
		///</summary>
		[DisplayName("星期集合(1-7,|隔开)")]
        [SugarColumn(ColumnName = "date_types", IsNullable = true, Length = 50, ColumnDescription = "星期集合(1-7,|隔开)", DefaultValue = "", ColumnDataType = "varchar")]         
		public string DateTypes { get; set; }
		/// <summary>
		/// 签到截止时
		///</summary>
		[DisplayName("签到截止时")]
        [SugarColumn(ColumnName = "start_hour", ColumnDescription = "签到截止时", DefaultValue = "0", ColumnDataType = "int")]         
		public int StartHour { get; set; }
		/// <summary>
		/// 签到截止分
		///</summary>
		[DisplayName("签到截止分")]
        [SugarColumn(ColumnName = "start_minute", ColumnDescription = "签到截止分", DefaultValue = "0", ColumnDataType = "int")]         
		public int StartMinute { get; set; }
		/// <summary>
		/// 签退截止时
		///</summary>
		[DisplayName("签退截止时")]
        [SugarColumn(ColumnName = "end_hour", ColumnDescription = "签退截止时", DefaultValue = "0", ColumnDataType = "int")]         
		public int EndHour { get; set; }
		/// <summary>
		/// 签退截止分
		///</summary>
		[DisplayName("签退截止分")]
        [SugarColumn(ColumnName = "end_minute", ColumnDescription = "签退截止分", DefaultValue = "0", ColumnDataType = "int")]         
		public int EndMinute { get; set; }
		/// <summary>
		/// 判断条件(0:不执行1:签到2:签退3:全部)
		///</summary>
		[DisplayName("判断条件(0:不执行1:签到2:签退3:全部)")]
        [SugarColumn(ColumnName = "sign_type", ColumnDescription = "判断条件(0:不执行1:签到2:签退3:全部)", DefaultValue = "0", ColumnDataType = "int")]         
		public int SignType { get; set; }
		/// <summary>
		/// 是否短信通知
		///</summary>
		[DisplayName("是否短信通知")]
        [SugarColumn(ColumnName = "is_note", Length = 1, ColumnDescription = "是否短信通知", DefaultValue = "0", ColumnDataType = "bit")]         
		public bool IsNote { get; set; }
		/// <summary>
		/// 是否检测申请
		///</summary>
		[DisplayName("是否检测申请")]
        [SugarColumn(ColumnName = "is_pd_apply", Length = 1, ColumnDescription = "是否检测申请", DefaultValue = "0", ColumnDataType = "bit")]         
		public bool IsPdApply { get; set; }
		/// <summary>
		/// 是否检测大厅
		///</summary>
		[DisplayName("是否检测大厅")]
        [SugarColumn(ColumnName = "is_dt_sign", Length = 1, ColumnDescription = "是否检测大厅", DefaultValue = "0", ColumnDataType = "bit")]         
		public bool IsDtSign { get; set; }
		/// <summary>
		/// 是否检测房间
		///</summary>
		[DisplayName("是否检测房间")]
        [SugarColumn(ColumnName = "is_fj_sign", Length = 1, ColumnDescription = "是否检测房间", DefaultValue = "0", ColumnDataType = "bit")]         
		public bool IsFjSign { get; set; }
		/// <summary>
		/// 是否检测兑换
		///</summary>
		[DisplayName("是否检测兑换")]
        [SugarColumn(ColumnName = "is_dh_sign", Length = 1, ColumnDescription = "是否检测兑换", DefaultValue = "0", ColumnDataType = "bit")]         
		public bool IsDhSign { get; set; }
		/// <summary>
		/// 单位ID
		///</summary>
		[DisplayName("单位ID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "单位ID", DefaultValue = "0", ColumnDataType = "int")]         
		public int UnitId { get; set; }
	}
}