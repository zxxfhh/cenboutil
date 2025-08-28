using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSugar;
namespace ApiModel
{
    public class CodeType
    {
        public string Name { get; set; }
        public string CSharepType { get; set; }
        public DbTypeInfo[] DbType { get; set; } 
        public int Sort { get; set; }
    }
    public class DbTypeInfo 
    {
        public string Name { get; set; }
        public int? Length { get; set; }
        public int? DecimalDigits { get; set; }
    }

    /// <summary>
    /// 排序计算MODEL
    /// </summary>
    public class SortTypeInfo
    {
        public DbTypeInfo DbTypeInfo { get; set; }
        public decimal Sort { get; set; }
        public CodeType CodeType { get; set; }
    }

}
