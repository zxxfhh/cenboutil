namespace ApiModel.Com
{
    public static class Template
    {
        public const string Entity = @"using System;
using SqlSugar;
using System.ComponentModel;

namespace @Model.name_space
{
    /// <summary>
    /// @(Model.Description)
    ///</summary>
    [DisplayName(""@(Model.Description)"")]
    [SugarTable(TableName = ""@(Model.TableName)"", TableDescription = ""@(Model.Description)"", IsDisabledUpdateAll = true)]
    public class @(Model.ClassName)
    {
	@foreach (var item in Model.PropertyGens){
		var isPrimaryKey = item.IsPrimaryKey ? "", IsPrimaryKey = true"" : """";
		var isIdentity = item.IsIdentity ? "", IsIdentity = true"" : """";
		var isNull=(item.IsNullable&&item.Type!=""string""&&item.IsSpecialType==false&&item.Type!=""byte[]"")?""?"":"""";

        var IsNullable = item.IsNullable ? "", IsNullable = true"" : """";

        var strLength="", Length = "" + item.Length;
	    var isLength = strLength;
        if(item.Type == ""decimal"")
        {
            isLength += "", DecimalDigits = "" + item.DecimalDigits;
        }

		var isIgnore=(item.IsIgnore?"", IsIgnore = true"":"""");    
        var isJson=(item.CodeType.StartsWith(""json"")?"", IsJson= true"":"""");
		var newPropertyName = item.PropertyName;
		
		if(System.Text.RegularExpressions.Regex.IsMatch(newPropertyName.Substring(0,1), ""[0-9]""))
		{
			newPropertyName=""_""+newPropertyName;//处理属性名开头为数字情况
		}
		if(newPropertyName==Model.ClassName)
		{
			newPropertyName=""_""+newPropertyName;//处理属性名不能等于类名
		}
		var desc=item.Description;
		if(isIgnore!="""")
		{
			isPrimaryKey =isIdentity =isNull="""";
		}

        string strDefaultValue = """";
        if(!item.IsText)
        {
           strDefaultValue = string.Format("", DefaultValue = \""{0}\"""", item.DefaultValue);
        }
        else
        {
           isLength = """"; 
        }

		@:/// <summary>
		@:/// @(desc)
		@:///</summary>
		@:[DisplayName(""@desc"")]
        @:[SugarColumn(ColumnName = ""@item.DbColumnName""@(isPrimaryKey)@(isIdentity)@(isIgnore)@(isJson)@(IsNullable)@(isLength), ColumnDescription = ""@item.Description""@Raw(strDefaultValue), ColumnDataType = ""@item.DbType"")]
		@:public @Raw(item.Type)@(isNull) @newPropertyName { get; set; }
	  }
	}
}";

        public const string DAO = @"using CenBoCommon.Zxx;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace @Model.name_space
{
	public sealed partial class @(Model.ClassName)DAO : DbContext<@(Model.ClassName)>
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

        public const string Controller = @"using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using CenBoCommon.Zxx;
using @Model.OrmName;

namespace @(Model.name_space).Controllers
{ 
    /// <summary> 
    /// @(Model.Description)
    /// </summary>
    [ApiController]
    [ControllSort(""1-1"")]
    public class @(Model.ClassName)Controller : ControllerBaseApi
    {
        /// <summary> 
        /// 批量保存
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route(""Api/[controller]/[action]"")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string SaveBatch(List<@(Model.ClassName)> list)
        {
            Status = false;
            Message = ""@(Model.Description)信息保存失败。"";
            if (list.IsZxxAny())
            {
               @if (Model.HasCreateId)
               {
                @:var optmdl = Request.GetToken();
                @:List<@(Model.ClassName)> insertlist = new List<@(Model.ClassName)>();
                @:List<@(Model.ClassName)> updatelist = new List<@(Model.ClassName)>();
                @:DateTime time = DateTime.Now;
                @:foreach (var item in list)
                @:{
                    @:item.UpdateId = optmdl.UserID;
                    @:item.UpdateTime = time.ToDateTimeString();
                    @:item.UpdateName = optmdl.UserName;
                    @:if (item.@(Model.PrimaryKey) == 0)
                    @:{
                        @:item.CreateId = optmdl.UserID;
                        @:item.CreateTime = time.ToDateTimeString();
                        @:item.CreateName = optmdl.UserName;
                        @:insertlist.Add(item);
                    @:}
                    @:else
                    @:{
                        @:updatelist.Add(item);
                    @:}
                @:}
                @:Status = @(Model.ClassName)DAO.Instance.TranAction(() =>
                @:{
                    @:if (insertlist.Count > 0) @(Model.ClassName)DAO.Instance.InsertRange(insertlist);
                    @:if (updatelist.Count > 0) @(Model.ClassName)DAO.Instance.UpdateIgnoreColumns(updatelist, it => new
                    @:{
                        @:it.CreateId,
                        @:it.CreateTime,
                        @:it.CreateName
                    @:});
                @:});
               }
               else
               {
                @:Status = @(Model.ClassName)DAO.Instance.SaveBatch(list);
               }
                if (Status)
                {
                    Message = ""@(Model.Description)信息保存成功。"";
                }
            }
            return Message;
        }
        
        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name=""_@(Model.PrimaryKey)"">主键</param>
        /// <returns></returns>
        [HttpPost]
        [Route(""Api/[controller]/[action]"")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public string DeleteByPk(@(Model.PrimaryKeyType) _@(Model.PrimaryKey))
        {
            Status = false;
            Message = ""@(Model.Description)删除失败。"";
            Status = @(Model.ClassName)DAO.Instance.DeleteBy(t => t.@(Model.PrimaryKey) == _@(Model.PrimaryKey));
            if (Status)
            {
                Message = ""@(Model.Description)信息删除成功。"";
            }
            return Message;
        }
        
        /// <summary>
        /// 根据主键查询单条数据
        /// </summary>
        /// <param name=""_@(Model.PrimaryKey)"">主键</param>
        /// <returns></returns>
        [HttpGet]
        [Route(""Api/[controller]/[action]"")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public @(Model.ClassName) GetInfoByPk(@(Model.PrimaryKeyType) _@(Model.PrimaryKey))
        {
            var entity = @(Model.ClassName)DAO.Instance.GetOneBy(t => t.@(Model.PrimaryKey) == _@(Model.PrimaryKey));
            return entity;
        }

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name=""model"">通用参数模型</param>
        /// <returns></returns>
        [HttpPost]
        [Route(""Api/[controller]/[action]"")]
        [Token]
        [ApiGroup(ApiGroupNames.Admin)]
        public List<@(Model.ClassName)> GetListByPage(ActionPara model)
        {
            int totalNumber = 0;
            var list = @(Model.ClassName)DAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

  }
}";

        public const string ViewDAO = @"using CenBoCommon.Zxx;;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace @Model.name_space
{
	public sealed partial class @(Model.ClassName)DAO : DbContext<@(Model.ClassName)>
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
		
		public @(Model.ClassName) GetOneBy(Expression<Func<@(Model.ClassName), bool>> wheres)
		{
			try
            {
				var one = GetSingle(wheres);
				var aa = sqlSugar;
				return one;
			}
			catch (Exception ex)
            {
                if (string.IsNullOrEmpty(sqlError))
				{
                    throw new Exception(ex.ToString());
				}
                else
				{
                    throw new Exception(sqlError);
				}
			}
		}
				
		public List<@(Model.ClassName)> GetListBy(Expression<Func<@(Model.ClassName), bool>> wheres)
		{
			try
            {
				var list = GetList(wheres);
				var aa = sqlSugar;
				return list;
			}
			catch (Exception ex)
            {
				if (string.IsNullOrEmpty(sqlError))
				{
                    throw new Exception(ex.ToString());
				}
                else
				{
                    throw new Exception(sqlError);
				}
			}
		}

        public List<@(Model.ClassName)> GetListByPage(ActionPara model, ref int total)
        {
            try
            {
                var sqlmodel = GetSqlModel(model);
                int totalNumber = 0;
                var list = Db.Queryable<@(Model.ClassName)>()
                        .Where(sqlmodel.Item1)
                        .OrderBy(sqlmodel.Item2)
                        .ToPageList(model.page, model.pagesize, ref totalNumber);
                total = totalNumber;
                var aa = sqlSugar;
                return list;
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(sqlError))
                {
                    throw new Exception(ex.ToString());
                }
                else
                {
                    throw new Exception(sqlError);
                }
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

namespace @(Model.name_space).Controllers
{ 
    /// <summary> 
    /// @(Model.Description)
    /// </summary>
    [ApiController]
    [ControllSort(""90-1"")]
    public class @(Model.ClassName)Controller : ControllerBaseApi
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
            int totalNumber = 0;
            var list = @(Model.ClassName)DAO.Instance.GetListByPage(model, ref totalNumber);
            TotalCount = totalNumber;
            return list;
        }

    }
}";

    }
}
