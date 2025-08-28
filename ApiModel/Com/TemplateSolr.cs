namespace ApiModel.Com
{
    public static class TemplateSolr
    {
        public const string Entity = @"using System;
using SolrNet.Attributes;
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
		var isPrimaryKey = item.IsPrimaryKey ? "", IsPrimaryKey = true"" : """";
		var isIdentity = item.IsIdentity ? "", IsIdentity = true"" : """";
		var isNull=(item.IsNullable&&item.Type!=""string""&&item.IsSpecialType==false&&item.Type!=""byte[]"")?""?"":"""";

        var IsNullable = item.IsNullable ? "", IsNullable = true"" : """";

        var strLength="", Length = "" + item.Length;
	    var isLength = """";
        if(item.Type != ""int"" && !item.IsPrimaryKey && item.Length > 0)
        {
            isLength = strLength;
            if(item.Type == ""decimal"")
            {
                isLength += "", DecimalDigits = "" + item.DecimalDigits;
            }
        }

		var isIgnore=(item.IsIgnore?"", IsIgnore = true"":"""");    
        var isJson=(item.CodeType.StartsWith(""json"")?"", IsJson= true"":"""");
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
		if(isIgnore != """")
		{
			isPrimaryKey = isIdentity =isNull="""";
		}

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

        public const string ViewDAO = @"using System;

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

    }
}
