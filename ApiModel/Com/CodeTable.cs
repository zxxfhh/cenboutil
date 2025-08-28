namespace ApiModel
{
    public class CodeTable
    {
        public int ClassCheck { get; set; } = 0;
        public string ClassName { get; set; }
        public string TableName { get; set; }
        public bool IsView { get; set; } = false;
        public string Description { get; set; }
    }
    public class CodeColumns
    {
        public string TableName { get; set; }
        public string ClassProperName { get; set; }
        public string DbColumnName { get; set; }
        public bool Required { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string Description { get; set; }
        public string DefaultValue { get; set; }
        public string CodeType { get; set; }
        public bool IsText { get; set; }
        public bool isNullable { get; set; }
        public int Length { get; set; }
        public int DecimalDigits { get; set; }
        public string DbType { get; set; }
    }

}
