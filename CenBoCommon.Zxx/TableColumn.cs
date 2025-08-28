using System.ComponentModel;

namespace CenBoCommon.Zxx
{
    [DisplayName("表字段")]
    public class TableColumn
    {
        [DisplayName("参数字段")]
        public string ParamName { get; set; }
        [DisplayName("表字段")]
        public string FieldName { get; set; }
        [DisplayName("是否为主键")]
        public bool IsPrimaryKey { get; set; } = false;
        [DisplayName("是否为时间字段")]
        public bool IsTime { get; set; } = false;
        [DisplayName("是否为字符串")]
        public bool IsString { get; set; } = false;
    }
}
