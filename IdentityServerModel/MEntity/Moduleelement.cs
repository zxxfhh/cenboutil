using System;
using SqlSugar;
using System.ComponentModel;

namespace IdentityServerModel
{
    /// <summary>
    /// �˵���ť
    ///</summary>
    [DisplayName("�˵���ť")]
    [SugarTable("moduleelement")]
    public class Moduleelement
    {
		/// <summary>
		/// ��ˮ��
		///</summary>
		[DisplayName("��ˮ��")]
        [SugarColumn(ColumnName = "Id", IsPrimaryKey = true)]
		public string Id { get; set; }
		/// <summary>
		/// DOM ID
		///</summary>
		[DisplayName("DOM ID")]
        [SugarColumn(ColumnName = "DomId", Length = 255)]
		public string DomId { get; set; }
		/// <summary>
		/// ����
		///</summary>
		[DisplayName("����")]
        [SugarColumn(ColumnName = "Name", Length = 255)]
		public string Name { get; set; }
		/// <summary>
		/// Ԫ�ظ�������
		///</summary>
		[DisplayName("Ԫ�ظ�������")]
        [SugarColumn(ColumnName = "Attr", ColumnDataType = "text")]         
		public string Attr { get; set; }
		/// <summary>
		/// Ԫ�ص��ýű�
		///</summary>
		[DisplayName("Ԫ�ص��ýű�")]
        [SugarColumn(ColumnName = "Script", ColumnDataType = "text")]         
		public string Script { get; set; }
		/// <summary>
		/// Ԫ��ͼ��
		///</summary>
		[DisplayName("Ԫ��ͼ��")]
        [SugarColumn(ColumnName = "Icon", Length = 255)]
		public string Icon { get; set; }
		/// <summary>
		/// Ԫ����ʽ
		///</summary>
		[DisplayName("Ԫ����ʽ")]
        [SugarColumn(ColumnName = "Class", Length = 255)]
		public string Class { get; set; }
		/// <summary>
		/// ��ע
		///</summary>
		[DisplayName("��ע")]
        [SugarColumn(ColumnName = "Remark", Length = 200)]
		public string Remark { get; set; }
		/// <summary>
		/// �����ֶ�
		///</summary>
		[DisplayName("�����ֶ�")]
        [SugarColumn(ColumnName = "Sort")]
		public int Sort { get; set; }
		/// <summary>
		/// ����ģ��Id
		///</summary>
		[DisplayName("����ģ��Id")]
        [SugarColumn(ColumnName = "ModuleId", Length = 50)]
		public string ModuleId { get; set; }
		/// <summary>
		/// ��������
		///</summary>
		[DisplayName("��������")]
        [SugarColumn(ColumnName = "TypeName", IsNullable = true, Length = 20)]
		public string TypeName { get; set; }
		/// <summary>
		/// ����ID
		///</summary>
		[DisplayName("����ID")]
        [SugarColumn(ColumnName = "TypeId", IsNullable = true, Length = 50)]
		public string TypeId { get; set; }
	}
}