using System;
using SqlSugar;
using System.ComponentModel;

namespace IdentityServerModel
{
    /// <summary>
    /// �ͻ������ñ�
    ///</summary>
    [DisplayName("�ͻ������ñ�")]
    [SugarTable("clientresources")]
    public class Clientresources
    {
		/// <summary>
		/// άһ����
		///</summary>
		[DisplayName("άһ����")]
        [SugarColumn(ColumnName = "SerialCode", IsPrimaryKey = true)]
		public string SerialCode { get; set; }
		/// <summary>
		/// �ͻ���ID
		///</summary>
		[DisplayName("�ͻ���ID")]
        [SugarColumn(ColumnName = "ClientId", IsNullable = true, Length = 30)]
		public string ClientId { get; set; }
		/// <summary>
		/// �ͻ�������
		///</summary>
		[DisplayName("�ͻ�������")]
        [SugarColumn(ColumnName = "ClientName", IsNullable = true, Length = 60)]
		public string ClientName { get; set; }
		/// <summary>
		/// �ͻ�����Կ
		///</summary>
		[DisplayName("�ͻ�����Կ")]
        [SugarColumn(ColumnName = "ClientSecrets", IsNullable = true, Length = 60)]
		public string ClientSecrets { get; set; }
		/// <summary>
		/// Token����ʱ��
		///</summary>
		[DisplayName("Token����ʱ��")]
        [SugarColumn(ColumnName = "TokenLifeTime")]
		public int TokenLifeTime { get; set; }
		/// <summary>
		/// ��¼�ص�URl����(|)
		///</summary>
		[DisplayName("��¼�ص�URl����(|)")]
        [SugarColumn(ColumnName = "RedirectUris", IsNullable = true, ColumnDataType = "text")]         
		public string RedirectUris { get; set; }
		/// <summary>
		/// ע���ص�URl����(|)
		///</summary>
		[DisplayName("ע���ص�URl����(|)")]
        [SugarColumn(ColumnName = "PostLogoutRedirectUris", IsNullable = true, ColumnDataType = "text")]         
		public string PostLogoutRedirectUris { get; set; }
		/// <summary>
		/// ����վ�㼯��(|)
		///</summary>
		[DisplayName("����վ�㼯��(|)")]
        [SugarColumn(ColumnName = "AllowedCorsOrigins", IsNullable = true, ColumnDataType = "text")]         
		public string AllowedCorsOrigins { get; set; }
		/// <summary>
		/// API���÷�Χ����(|)
		///</summary>
		[DisplayName("API���÷�Χ����(|)")]
        [SugarColumn(ColumnName = "AllowedScopes", IsNullable = true, ColumnDataType = "text")]         
		public string AllowedScopes { get; set; }
		/// <summary>
		/// �Ƿ�����(1:���� 0:����)
		///</summary>
		[DisplayName("�Ƿ�����(1:���� 0:����)")]
        [SugarColumn(ColumnName = "IsEnable")]
		public int IsEnable { get; set; }
		/// <summary>
		/// �����û�ID
		///</summary>
		[DisplayName("�����û�ID")]
        [SugarColumn(ColumnName = "CreateId")]
		public int CreateId { get; set; }
		/// <summary>
		/// ����ʱ��
		///</summary>
		[DisplayName("����ʱ��")]
        [SugarColumn(ColumnName = "CreateTime", IsNullable = true, Length = 20)]
		public string CreateTime { get; set; }
		/// <summary>
		/// �����û�����
		///</summary>
		[DisplayName("�����û�����")]
        [SugarColumn(ColumnName = "CreateName", IsNullable = true, Length = 50)]
		public string CreateName { get; set; }
		/// <summary>
		/// �޸��û�ID
		///</summary>
		[DisplayName("�޸��û�ID")]
        [SugarColumn(ColumnName = "UpdateId")]
		public int UpdateId { get; set; }
		/// <summary>
		/// �޸�ʱ��
		///</summary>
		[DisplayName("�޸�ʱ��")]
        [SugarColumn(ColumnName = "UpdateTime", IsNullable = true, Length = 20)]
		public string UpdateTime { get; set; }
		/// <summary>
		/// �޸��û�����
		///</summary>
		[DisplayName("�޸��û�����")]
        [SugarColumn(ColumnName = "UpdateName", IsNullable = true, Length = 50)]
		public string UpdateName { get; set; }
	}
}