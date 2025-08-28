using System;
using SqlSugar;
using System.ComponentModel;

namespace IdentityServerModel
{
    /// <summary>
    /// �˵���
    ///</summary>
    [DisplayName("�˵���")]
    [SugarTable("module")]
    public class Module
    {
		/// <summary>
		/// ����ģ����ˮ��
		///</summary>
		[DisplayName("����ģ����ˮ��")]
        [SugarColumn(ColumnName = "Id", IsPrimaryKey = true)]
		public string Id { get; set; }
		/// <summary>
		/// �ڵ�����ID
		///</summary>
		[DisplayName("�ڵ�����ID")]
        [SugarColumn(ColumnName = "CascadeId", Length = 255)]
		public string CascadeId { get; set; }
		/// <summary>
		/// ����ģ������
		///</summary>
		[DisplayName("����ģ������")]
        [SugarColumn(ColumnName = "Name", Length = 255)]
		public string Name { get; set; }
		/// <summary>
		/// ��ҳ��URL
		///</summary>
		[DisplayName("��ҳ��URL")]
        [SugarColumn(ColumnName = "Url", Length = 255)]
		public string Url { get; set; }
		/// <summary>
		/// �Ƿ�Ҷ�ӽڵ�
		///</summary>
		[DisplayName("�Ƿ�Ҷ�ӽڵ�")]
        [SugarColumn(ColumnName = "IsLeaf", Length = 4)]
		public byte IsLeaf { get; set; }
		/// <summary>
		/// �Ƿ��Զ�չ��
		///</summary>
		[DisplayName("�Ƿ��Զ�չ��")]
        [SugarColumn(ColumnName = "IsAutoExpand", Length = 4)]
		public byte IsAutoExpand { get; set; }
		/// <summary>
		/// �ڵ�ͼ���ļ�����
		///</summary>
		[DisplayName("�ڵ�ͼ���ļ�����")]
        [SugarColumn(ColumnName = "IconName", Length = 255)]
		public string IconName { get; set; }
		/// <summary>
		/// ��ǰ״̬
		///</summary>
		[DisplayName("��ǰ״̬")]
        [SugarColumn(ColumnName = "Status")]
		public int Status { get; set; }
		/// <summary>
		/// ���ڵ�����
		///</summary>
		[DisplayName("���ڵ�����")]
        [SugarColumn(ColumnName = "ParentName", Length = 255)]
		public string ParentName { get; set; }
		/// <summary>
		/// ʸ��ͼ��
		///</summary>
		[DisplayName("ʸ��ͼ��")]
        [SugarColumn(ColumnName = "Vector", Length = 255)]
		public string Vector { get; set; }
		/// <summary>
		/// �����
		///</summary>
		[DisplayName("�����")]
        [SugarColumn(ColumnName = "SortNo")]
		public int SortNo { get; set; }
		/// <summary>
		/// ���ڵ���ˮ��
		///</summary>
		[DisplayName("���ڵ���ˮ��")]
        [SugarColumn(ColumnName = "ParentId", IsNullable = true, Length = 50)]
		public string ParentId { get; set; }
		/// <summary>
		/// ģ���ʶ
		///</summary>
		[DisplayName("ģ���ʶ")]
        [SugarColumn(ColumnName = "Code", IsNullable = true, Length = 50)]
		public string Code { get; set; }
		/// <summary>
		/// �Ƿ�Ϊϵͳģ��
		///</summary>
		[DisplayName("�Ƿ�Ϊϵͳģ��")]
        [SugarColumn(ColumnName = "IsSys", Length = 4)]
		public byte IsSys { get; set; }
	}
}