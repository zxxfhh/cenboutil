using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace CenBoCommon.Zxx
{
    public static class ClassChangeHelper
    {
        /// <summary>
        /// 首字母小写写
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FirstCharToLower(this string input)
        {
            if (String.IsNullOrEmpty(input))
                return input;
            string str = input.First().ToString().ToLower() + input.Substring(1);
            return str;
        }

        /// <summary>
        /// 首字母大写
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FirstCharToUpper(this string input)
        {
            if (String.IsNullOrEmpty(input))
                return input;
            string str = input.First().ToString().ToUpper() + input.Substring(1);
            return str;
        }

        /// <summary>
        /// 类对象复制
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="t"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static void CopyTypeValue<T, K>(this T t, K k)
            where T : class
            where K : class
        {
            if (t == null) return;

            PropertyInfo[] tmod = t.GetType().GetProperties();
            var kmod = k.GetType().GetProperties();
            var klist = kmod.ToList();

            foreach (PropertyInfo fi in tmod)
            {
                var ki = klist.Find(l => l.Name == fi.Name);
                if (ki != null && fi.PropertyType == ki.PropertyType)
                {
                    ki.SetValue(k, fi.GetValue(t, null), null);
                }
            }
        }

        public static Tuple<string, string> GetFieldNames<T>(this T t)
        {
            List<string> enlist = new List<string>();
            List<string> zhlist = new List<string>();
            PropertyInfo[] tmod = t.GetType().GetProperties();
            foreach (PropertyInfo fi in tmod)
            {
                enlist.Add(fi.Name);
                foreach (var cusa in fi.CustomAttributes)
                {
                    if (cusa.AttributeType.Name == "DisplayNameAttribute")
                    {
                        zhlist.Add(cusa.ConstructorArguments[0].Value.ToString());
                    }
                }
            }

            return new Tuple<string, string>(string.Join(",", enlist), string.Join(",", zhlist));
        }

        /// <summary>
        /// 根据字段名称更改值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="typename">字段名称</param>
        /// <param name="value">设置值</param>
        public static void SetTypeValue<T>(this T t, string typename, string value)
        {
            if (t == null) return;

            Type type = t.GetType();
            var field = type.GetProperty(typename);
            if (field != null)
            {
                var oldv = field.GetValue(t);
                // 将新的值赋给字段
                object valueToSet = value;

                field.SetValue(t, valueToSet);
            }
        }

        /// <summary>
        /// 根据字段名称获取值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="typename">字段名称</param>
        public static object GetTypeValue<T>(this T t, string typename)
        {
            if (t != null)
            {
                Type type = t.GetType();
                var field = type.GetProperty(typename);
                if (field != null)
                {
                    return field.GetValue(t);
                }
            }
            return null;
        }

        /// <summary>
        /// DataRow转实体类T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T ChangeTableToEntity<T>(this DataRow dr, T t)
        {
            try
            {
                Type type = typeof(T);
                PropertyInfo[] prolist = type.GetProperties();
                if (prolist != null && prolist.Length > 0)
                {
                    foreach (var property in prolist)
                    {
                        if (dr.Table.Columns.Contains(property.Name))
                        {
                            property.SetValue(t, Convert.ChangeType(dr[property.Name], property.PropertyType), null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return t;
        }

        /// <summary>
        /// DataTable转List<T>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<T> ToList<T>(this DataTable dt) where T : new()
        {
            Type type = typeof(T);
            var properties = type.GetProperties();
            var list = new List<T>();

            foreach (DataRow row in dt.Rows)
            {
                T item = new T();

                foreach (var prop in properties)
                {
                    if (dt.Columns.Contains(prop.Name))
                    {
                        PropertyInfo propertyInfo = item.GetType().GetProperty(prop.Name);
                        if (propertyInfo != null && row[prop.Name] != DBNull.Value)
                        {
                            propertyInfo.SetValue(item, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                        }
                    }
                }

                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// List<T>转DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="varlist"></param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this List<T> varlist)
        {
            DataTable dtReturn = new DataTable(typeof(T).Name);

            // 获取T的所有属性
            PropertyInfo[] oProps = null;

            if (varlist == null) return dtReturn;
            foreach (T rec in varlist)
            {
                // 获取当前对象的所有属性
                if (oProps == null)
                {
                    oProps = ((Type)rec.GetType()).GetProperties();
                    foreach (PropertyInfo pi in oProps)
                    {
                        Type colType = pi.PropertyType;

                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dtReturn.Columns.Add(new DataColumn(pi.Name, colType));
                    }
                }

                DataRow dr = dtReturn.NewRow();
                foreach (PropertyInfo pi in oProps)
                {
                    dr[pi.Name] = pi.GetValue(rec, null) ?? DBNull.Value;
                }

                dtReturn.Rows.Add(dr);
            }

            return dtReturn;
        }
    }

}
