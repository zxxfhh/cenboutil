using System;
using SqlSugar;
using System.ComponentModel;

namespace ApiModel
{
    /// <summary>
    /// ǩ���������
    ///</summary>
    [DisplayName("ǩ���������")]
    [SugarTable(TableName = "event_sign_set_cc", TableDescription = "ǩ���������", IsDisabledUpdateAll = true)]
    public class EventSignSet
    {
		/// <summary>
		/// ����ID
		///</summary>
		[DisplayName("����ID")]
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "����ID", DefaultValue = "", ColumnDataType = "int")]         
		public int Id { get; set; }
		/// <summary>
		/// ���ڼ���(1-7,|����)
		///</summary>
		[DisplayName("���ڼ���(1-7,|����)")]
        [SugarColumn(ColumnName = "date_types", IsNullable = true, Length = 50, ColumnDescription = "���ڼ���(1-7,|����)", DefaultValue = "", ColumnDataType = "varchar")]         
		public string DateTypes { get; set; }
		/// <summary>
		/// ǩ����ֹʱ
		///</summary>
		[DisplayName("ǩ����ֹʱ")]
        [SugarColumn(ColumnName = "start_hour", ColumnDescription = "ǩ����ֹʱ", DefaultValue = "0", ColumnDataType = "int")]         
		public int StartHour { get; set; }
		/// <summary>
		/// ǩ����ֹ��
		///</summary>
		[DisplayName("ǩ����ֹ��")]
        [SugarColumn(ColumnName = "start_minute", ColumnDescription = "ǩ����ֹ��", DefaultValue = "0", ColumnDataType = "int")]         
		public int StartMinute { get; set; }
		/// <summary>
		/// ǩ�˽�ֹʱ
		///</summary>
		[DisplayName("ǩ�˽�ֹʱ")]
        [SugarColumn(ColumnName = "end_hour", ColumnDescription = "ǩ�˽�ֹʱ", DefaultValue = "0", ColumnDataType = "int")]         
		public int EndHour { get; set; }
		/// <summary>
		/// ǩ�˽�ֹ��
		///</summary>
		[DisplayName("ǩ�˽�ֹ��")]
        [SugarColumn(ColumnName = "end_minute", ColumnDescription = "ǩ�˽�ֹ��", DefaultValue = "0", ColumnDataType = "int")]         
		public int EndMinute { get; set; }
		/// <summary>
		/// �ж�����(0:��ִ��1:ǩ��2:ǩ��3:ȫ��)
		///</summary>
		[DisplayName("�ж�����(0:��ִ��1:ǩ��2:ǩ��3:ȫ��)")]
        [SugarColumn(ColumnName = "sign_type", ColumnDescription = "�ж�����(0:��ִ��1:ǩ��2:ǩ��3:ȫ��)", DefaultValue = "0", ColumnDataType = "int")]         
		public int SignType { get; set; }
		/// <summary>
		/// �Ƿ����֪ͨ
		///</summary>
		[DisplayName("�Ƿ����֪ͨ")]
        [SugarColumn(ColumnName = "is_note", Length = 1, ColumnDescription = "�Ƿ����֪ͨ", DefaultValue = "0", ColumnDataType = "bit")]         
		public bool IsNote { get; set; }
		/// <summary>
		/// �Ƿ�������
		///</summary>
		[DisplayName("�Ƿ�������")]
        [SugarColumn(ColumnName = "is_pd_apply", Length = 1, ColumnDescription = "�Ƿ�������", DefaultValue = "0", ColumnDataType = "bit")]         
		public bool IsPdApply { get; set; }
		/// <summary>
		/// �Ƿ������
		///</summary>
		[DisplayName("�Ƿ������")]
        [SugarColumn(ColumnName = "is_dt_sign", Length = 1, ColumnDescription = "�Ƿ������", DefaultValue = "0", ColumnDataType = "bit")]         
		public bool IsDtSign { get; set; }
		/// <summary>
		/// �Ƿ��ⷿ��
		///</summary>
		[DisplayName("�Ƿ��ⷿ��")]
        [SugarColumn(ColumnName = "is_fj_sign", Length = 1, ColumnDescription = "�Ƿ��ⷿ��", DefaultValue = "0", ColumnDataType = "bit")]         
		public bool IsFjSign { get; set; }
		/// <summary>
		/// �Ƿ���һ�
		///</summary>
		[DisplayName("�Ƿ���һ�")]
        [SugarColumn(ColumnName = "is_dh_sign", Length = 1, ColumnDescription = "�Ƿ���һ�", DefaultValue = "0", ColumnDataType = "bit")]         
		public bool IsDhSign { get; set; }
		/// <summary>
		/// ��λID
		///</summary>
		[DisplayName("��λID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "��λID", DefaultValue = "0", ColumnDataType = "int")]         
		public int UnitId { get; set; }
	}
}