using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CenBoCommon.Zxx
{
    public static class TableToList
    {
        /// <summary>
        /// datatable转换为list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static List<T> GetList<T>(this DataTable table)
        {
            List<T> list = new List<T>();
            T t = default(T);
            PropertyInfo[] propertypes = null;
            string tempName = string.Empty;
            foreach (DataRow row in table.Rows)
            {
                t = Activator.CreateInstance<T>();
                propertypes = t.GetType().GetProperties();
                foreach (PropertyInfo pro in propertypes)
                {
                    tempName = pro.Name;
                    if (table.Columns.Contains(tempName))
                    {
                        object value = row[tempName];
                        if (!value.ToString().Equals(""))
                        {
                            pro.SetValue(t, value, null);
                        }
                    }
                }
                list.Add(t);
            }
            return list.Count == 0 ? null : list;
        }

        public static DataSet ToDataSetList<T>(this IList<T> list)
        {
            if (list == null || list.Count <= 0)
            {
                return null;
            }

            DataSet ds = new DataSet();
            DataTable dt = new DataTable(typeof(T).Name);
            DataColumn column;
            DataRow row;

            PropertyInfo[] myPropertyInfo = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (T t in list)
            {
                if (t == null)
                {
                    continue;
                }

                row = dt.NewRow();

                for (int i = 0, j = myPropertyInfo.Length; i < j; i++)
                {
                    PropertyInfo pi = myPropertyInfo[i];

                    string name = pi.Name;

                    if (dt.Columns[name] == null)
                    {
                        column = new DataColumn(name, pi.PropertyType);
                        dt.Columns.Add(column);
                    }

                    row[name] = pi.GetValue(t, null);
                }

                dt.Rows.Add(row);
            }

            ds.Tables.Add(dt);

            return ds;
        }

        public static DataTable ToDataTableList<T>(this IList<T> list)
        {
            if (list == null || list.Count <= 0)
            {
                return null;
            }

            DataTable dt = new DataTable(typeof(T).Name);
            DataColumn column = null;
            DataRow row;

            PropertyInfo[] myPropertyInfo = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (T t in list)
            {
                if (t == null)
                {
                    continue;
                }

                row = dt.NewRow();

                for (int i = 0, j = myPropertyInfo.Length; i < j; i++)
                {
                    PropertyInfo pi = myPropertyInfo[i];

                    string name = pi.Name;

                    if (dt.Columns[name] == null)
                    {
                        if (pi.PropertyType.ToString().Contains("Nullable"))
                        {
                            // 正则表达式，匹配方括号及其内容
                            string pattern = @"\[(.*?)\]";

                            // 使用正则表达式匹配所有符合的内容
                            MatchCollection matches = Regex.Matches(pi.PropertyType.ToString(), pattern);

                            foreach (Match match in matches)
                            {
                                // 提取括号内的值，这里 group[0] 是整个匹配，group[1] 是第一个捕获组
                                string content = match.Groups[1].Value;
                                column = new DataColumn(name, Type.GetType(content));
                                continue;
                            }
                        }
                        else
                        {
                            column = new DataColumn(name, pi.PropertyType);
                        }
                        if (column != null)
                            dt.Columns.Add(column);
                    }

                    row[name] = pi.GetValue(t, null);
                }

                dt.Rows.Add(row);
            }

            return dt;
        }

        public static DataTable ToDataTable<T>(this T t)
        {
            if (t == null)
            {
                return null;
            }

            PropertyInfo[] myPropertyInfo = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            DataTable dt = new DataTable(typeof(T).Name);
            DataColumn column;
            DataRow row = dt.NewRow();

            for (int i = 0, j = myPropertyInfo.Length; i < j; i++)
            {
                PropertyInfo pi = myPropertyInfo[i];

                string name = pi.Name;
                if (dt.Columns[name] == null)
                {
                    column = new DataColumn(name, pi.PropertyType);
                    dt.Columns.Add(column);
                }

                row[name] = pi.GetValue(t, null);
            }

            dt.Rows.Add(row);

            return dt;
        }

        /// <summary>
        /// DataTable的行转类对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T TableRowToEntity<T>(this DataRow dr)
        {
            Type type = typeof(T);
            var t = Activator.CreateInstance(type);
            try
            {
                PropertyInfo[] prolist = type.GetProperties();
                if (prolist != null && prolist.Length > 0)
                {
                    foreach (var property in prolist)
                    {
                        if (dr.Table.Columns.Contains(property.Name))
                        {
                            if (property.PropertyType == typeof(String))
                            {
                                property.SetValue(t, dr[property.Name].ToString());
                            }
                            else if (property.PropertyType == typeof(Decimal))
                            {
                                property.SetValue(t, dr[property.Name].ToZxxDecimal());
                            }
                            else if (property.PropertyType == typeof(DateTime))
                            {
                                property.SetValue(t, dr[property.Name].ToZxxDateTime());
                            }
                            else if (property.PropertyType == typeof(Int32))
                            {
                                property.SetValue(t, dr[property.Name].ToZxxInt());
                            }
                            else if (property.PropertyType == typeof(Int64))
                            {
                                property.SetValue(t, dr[property.Name].ToZxxLong());
                            }
                            else if (property.PropertyType == typeof(Double))
                            {
                                property.SetValue(t, dr[property.Name].ToZxxDouble());
                            }
                            else
                            {
                                property.SetValue(t, dr[property.Name]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return (T)t;
        }

        public static DataRow EntityToRow<T>(this T t, DataRow dr)
        {
            try
            {
                PropertyInfo[] prolist = t.GetType().GetProperties();
                if (prolist != null && prolist.Length > 0)
                {
                    foreach (var property in prolist)
                    {
                        if (dr.Table.Columns.Contains(property.Name))
                        {
                            dr[property.Name] = property.GetValue(t);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dr;
        }

        public static DataRow RowToRow(this DataRow olddr, DataRow newdr)
        {
            try
            {
                foreach (DataColumn col in olddr.Table.Columns)
                {
                    if (newdr.Table.Columns.Contains(col.ColumnName))
                    {
                        newdr[col.ColumnName] = olddr[col.ColumnName];
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return newdr;
        }


    }
}
