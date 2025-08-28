using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CenBoCommon.Zxx
{
    /// <summary>
    /// 缓存集合查询
    /// </summary>
    public static class CacheEntityList
    {
        public static (List<T>, int) GetListByPage<T>(this ActionPara model, List<T> oldlist)
        {
            List<T> list = new List<T>();
            try
            {
                model.sconlist.RemoveAll(t => t.ParamName.IsZxxNullOrEmpty());
                var sqlTvalue = TableSelectInfo<T>.GetSqlModel(model);
                if (!sqlTvalue.Item1.IsZxxNullOrEmpty())
                {
                    DataTable table = oldlist.ToDataTableList();
                    var rows = table.Select(sqlTvalue.Item1);
                    if (rows.Length > 0)
                    {
                        if (!sqlTvalue.Item2.IsZxxNullOrEmpty())
                        {
                            DataTable table2 = table.Clone();
                            foreach (var oldrow in rows)
                            {
                                var newrow = table2.NewRow();
                                table2.Rows.Add(oldrow.RowToRow(newrow));
                            }
                            DataView dv = table2.DefaultView;
                            dv.Sort = sqlTvalue.Item2;
                            var dt = dv.ToTable();
                            list.AddRange(dt.GetList<T>());
                        }
                        else
                        {
                            foreach (var row in rows)
                            {
                                list.Add(row.TableRowToEntity<T>());
                            }
                        }
                    }
                }
                else
                {
                    if (!sqlTvalue.Item2.IsZxxNullOrEmpty())
                    {
                        DataTable table = oldlist.ToDataTableList();
                        var rows = table.Select(" 1=1 ");
                        if (rows.Length > 0)
                        {
                            DataTable table2 = table.Clone();
                            foreach (var oldrow in rows)
                            {
                                var newrow = table2.NewRow();
                                table2.Rows.Add(oldrow.RowToRow(newrow));
                            }
                            DataView dv = table2.DefaultView;
                            dv.Sort = sqlTvalue.Item2;
                            var dt = dv.ToTable();
                            list.AddRange(dt.GetList<T>());
                        }
                    }
                    else
                    {
                        list.AddRange(oldlist);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            int totalcount = 0;
            if (list.Any())
            {
                totalcount = list.Count;
                if (model.page > 0 && model.pagesize > 0)
                {
                    int startindex = (model.page - 1) * model.pagesize;
                    var _list = list.Skip(startindex).Take(model.pagesize).ToList();
                    if (_list.Any())
                    {
                        list.Clear();
                        list.AddRange(_list);
                    }
                }
            }
            return (list, totalcount);
        }


    }
}
