using System.Collections.Generic;
using System.ComponentModel;

namespace CenBoCommon.Zxx
{
    /// <summary>
    /// 通用参数模型
    /// </summary>
    [DisplayName("通用参数模型")]
    public class ActionPara
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        [DisplayName("开始时间")]
        public string starttime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        [DisplayName("结束时间")]
        public string endtime { get; set; }
        /// <summary>
        /// 页码
        /// </summary>
        [DisplayName("页码")]
        public int page { get; set; }
        /// <summary>
        /// 行数
        /// </summary>
        [DisplayName("行数")]
        public int pagesize { get; set; }
        /// <summary>
        /// 其他条件
        /// </summary>
        [DisplayName("其他条件")]
        public List<SelectCondition> sconlist { get; set; } = new List<SelectCondition>();

    }

    public class SelectCondition
    {

        [DisplayName("参数表字段")]
        public string ParamName { get; set; }

        //1:sort条件约束
        //当条件选择sort关键字时，该字段只是用于排序。
        [DisplayName("查询规则(=|!=|>|>=|<|<=|in|notin|like|isnull)")]
        public string ParamType { get; set; }

        [DisplayName("参数值")]
        public string ParamValue { get; set; }

        [DisplayName("排序(0:不处理 1:正序 2:倒序)")]
        public int ParamSort { get; set; }

        [DisplayName("参数分组表字段")]
        public string ParamGroupName { get; set; }

        [DisplayName("分组内部条件(and,or)")]
        public string GroupCondition { get; set; } = "and";

        [DisplayName("是否为起始字段")]
        public bool IsGroupFrist { get; set; } = false;

    }

}

