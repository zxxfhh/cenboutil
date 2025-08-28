namespace ApiModel.Com
{
    public static class TemplateSolrHttp
    {
        public const string Entity = @"using System;
using System.ComponentModel;

namespace @Model.name_space
{
    /// <summary>
    /// @(Model.Description)
    ///</summary>
    [DisplayName(""@(Model.Description)"")]
    public class @(Model.ClassName)
    {
	@foreach (var item in Model.PropertyGens){
		var isNull=(item.IsNullable&&item.Type!=""string""&&item.IsSpecialType==false&&item.Type!=""byte[]"")?""?"":"""";

		var newPropertyName = item.PropertyName;
		if(System.Text.RegularExpressions.Regex.IsMatch(newPropertyName.Substring(0,1), ""[0-9]""))
		{
			newPropertyName=""_""+newPropertyName;//处理属性名开头为数字情况
		}
		if(newPropertyName == Model.ClassName)
		{
			newPropertyName=""_""+newPropertyName;//处理属性名不能等于类名
		}
		var desc = item.Description;//处理换行

		@:/// <summary>
		@:/// @(desc)
		@:///</summary>
		@:[DisplayName(""@desc"")]
    if(item.IsPrimaryKey)
    {
        @:[SolrUniqueKey(""@item.DbColumnName"")]         
    }
    else
    {
        @:[SolrField(""@item.DbColumnName"")]    
    }	
		@:public @Raw(item.Type)@(isNull) @newPropertyName { get; set; }
	  }
	}
}";

        public const string DAO = @"using System;

namespace @Model.name_space
{
	public sealed partial class @(Model.ClassName)DAO : SolrDbContext<@(Model.ClassName)>
    {
		private static @(Model.ClassName)DAO instance;
		public static @(Model.ClassName)DAO Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new @(Model.ClassName)DAO();
				}
				return instance;
			}
		}
		
	}
}";

        public const string ViewController = @"using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using CenBoCommon.Zxx;
using @Model.OrmName;

namespace @Model.name_space
{ 
    /// <summary> 
    /// @(Model.Description)
    /// </summary>
    [ApiController]
    [ControllSort(""90-1"")]
    public class @(Model.PrimaryKey)Controller : ControllerBaseApi
    {

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name=""model"">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route(""Api/[controller]/[action]"")]
        [Token]
        public List<@(Model.ClassName)> GetListByPage(ActionPara model)
        {
            long totalNumber = 0;
            var list = @(Model.ClassName)DAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber.ToZxxInt();
            return list;
        }

    }
}";

    }
}
