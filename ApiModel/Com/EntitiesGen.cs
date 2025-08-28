﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiModel
{
    /// <summary>
    /// 生成BO
    /// </summary>
    public class BOGen
    {
        public BOGen()
        {
            name_space = "DefaultModels";
        }
        public string name_space { get; set; }
        /// <summary>
        /// 类名
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// 主键
        /// </summary>
        public string PrimaryKey { get; set; }

        /// <summary>
        /// 主键数据类型
        /// </summary>
        public string PrimaryKeyType { get; set; }

    }

    /// <summary>
    /// 生成实体结构
    /// </summary>
    public class EntitiesGen
    {
        public EntitiesGen()
        {
            name_space = "DefaultModels";
        }
        public string name_space { get; set; }
        /// <summary>
        /// 类名
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 备注 
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 列集合
        /// </summary>
        public List<PropertyGen> PropertyGens { get; set; }

    }

    /// <summary>
    /// 属性和列
    /// </summary>
    public class PropertyGen
    {
        /// <summary>
        /// 属性名
        /// </summary>
        public string PropertyName { get; set; }
        /// <summary>
        /// 列名
        /// </summary>
        public string DbColumnName { get; set; }
        /// <summary>
        /// 主键
        /// </summary>
        public bool IsPrimaryKey { get; set; }
        /// <summary>
        /// 自增列 
        /// </summary>
        public bool IsIdentity { get; set; }
        /// <summary>
        /// 备注 
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 默认值 
        /// </summary>
        public string DefaultValue { get; set; }
        /// <summary>
        /// 属性类型
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 是否是为NULL
        /// </summary>
        public bool IsNullable { get; set; }
        /// <summary>
        /// 数据库类型
        /// </summary>
        public string DbType { get; set; }
        /// <summary>
        /// 是否忽略
        /// </summary>
        public bool IsIgnore { get; set; }
        /// <summary>
        /// 特殊类型
        /// </summary>
        public bool IsSpecialType { get; set; }
        /// <summary>
        /// 配置的类型名称 (比如 string100)
        /// </summary>
        public string CodeType { get; set; }
        /// <summary>
        /// 是否Text
        /// </summary>
        public bool IsText { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// 小数位
        /// </summary>
        public int DecimalDigits { get; set; }
    }

    /// <summary>
    /// 生成Controller
    /// </summary>
    public class ControllerGen
    {
        public ControllerGen()
        {
            name_space = "DefaultModels";
        }
        public string name_space { get; set; }
        /// <summary>
        /// 类名
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// ORM命名空间
        /// </summary>
        public string OrmName { get; set; }
        /// <summary>
        /// 主键
        /// </summary>
        public string PrimaryKey { get; set; }
        /// <summary>
        /// 主键数据类型
        /// </summary>
        public string PrimaryKeyType { get; set; }
        /// <summary>
        /// 主键
        /// </summary>
        public bool HasCreateId { get; set; }
    }

}
