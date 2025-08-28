using SqlSugar;
using System.ComponentModel;

namespace IdentityServerModel
{
    /// <summary>
    /// �û���Ϣ��
    ///</summary>
    [DisplayName("�û���Ϣ��")]
    [SugarTable("account")]
    public class Account
    {
        /// <summary>
        /// �û�ID
        ///</summary>
        [DisplayName("�û�ID")]
        [SugarColumn(ColumnName = "UserSnowId", IsPrimaryKey = true)]
        public long UserSnowId { get; set; }
        /// <summary>
        /// �˺�
        ///</summary>
        [DisplayName("�˺�")]
        [SugarColumn(ColumnName = "UserUid", Length = 20)]
        public string UserUid { get; set; }
        /// <summary>
        /// �ǳ�
        ///</summary>
        [DisplayName("�ǳ�")]
        [SugarColumn(ColumnName = "TrueName", IsNullable = true, Length = 32)]
        public string TrueName { get; set; }
        /// <summary>
        /// ��¼����
        ///</summary>
        [DisplayName("��¼����")]
        [SugarColumn(ColumnName = "Password", Length = 60)]
        public string Password { get; set; }
        /// <summary>
        /// ��¼����
        ///</summary>
        [DisplayName("��¼����")]
        [SugarColumn(ColumnName = "LoginCount")]
        public int LoginCount { get; set; }
        /// <summary>
        /// �ϴε�¼ʱ��
        ///</summary>
        [DisplayName("�ϴε�¼ʱ��")]
        [SugarColumn(ColumnName = "LastLoginTime", IsNullable = true, Length = 20)]
        public string LastLoginTime { get; set; }
        /// <summary>
        /// �Ƿ�����(1:���� 0:����)
        ///</summary>
        [DisplayName("�Ƿ�����(1:���� 0:����)")]
        [SugarColumn(ColumnName = "IsEnable")]
        public int IsEnable { get; set; }
        /// <summary>
        /// �Ƿ�ɾ��(1:���� 0:ɾ��)
        ///</summary>
        [DisplayName("�Ƿ�ɾ��(1:���� 0:ɾ��)")]
        [SugarColumn(ColumnName = "IsDelete")]
        public int IsDelete { get; set; }
        /// <summary>
        /// �Ƿ�����(1:���� 0:������)
        ///</summary>
        [DisplayName("�Ƿ�����(1:���� 0:������)")]
        [SugarColumn(ColumnName = "OnlineState")]
        public int OnlineState { get; set; }
        /// <summary>
        /// �ϴ��˳�ʱ��
        ///</summary>
        [DisplayName("�ϴ��˳�ʱ��")]
        [SugarColumn(ColumnName = "LastOutTime", IsNullable = true, Length = 20)]
        public string LastOutTime { get; set; }
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
        [SugarColumn(ColumnName = "CreateTime")]
        public DateTime CreateTime { get; set; }
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
        [SugarColumn(ColumnName = "UpdateTime")]
        public DateTime UpdateTime { get; set; }
        /// <summary>
        /// �޸��û�����
        ///</summary>
        [DisplayName("�޸��û�����")]
        [SugarColumn(ColumnName = "UpdateName", IsNullable = true, Length = 50)]
        public string UpdateName { get; set; }
        /// <summary>
        /// ��ע
        ///</summary>
        [DisplayName("��ע")]
        [SugarColumn(ColumnName = "UserRemark", IsNullable = true, Length = 300)]
        public string UserRemark { get; set; }
    }
}