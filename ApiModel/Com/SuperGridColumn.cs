using DevComponents.DotNetBar.SuperGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using System.Reflection;

namespace ApiModel
{
    public static class SuperGridColumn
    {
        public static void ColumnAdd(this SuperGridControl spg, string name, string text, int width = 100,
                                                    bool visible = true, bool read = false, bool columnsort = true)
        {
            try
            {
                GridColumn gd = new GridColumn();
                gd = new GridColumn();
                gd.Name = name;
                gd.DataPropertyName = name;
                gd.HeaderText = text;
                gd.Width = width;
                gd.Visible = visible;
                gd.ReadOnly = read;
                if (columnsort)
                {
                    gd.ColumnSortMode = ColumnSortMode.Single;
                    gd.SortCycle = SortCycle.NotSet;
                    gd.SortIndicator = SortIndicator.Auto;
                }
                else
                {
                    gd.ColumnSortMode = ColumnSortMode.None;
                    gd.SortCycle = SortCycle.NotSet;
                    gd.SortIndicator = SortIndicator.None;
                }
                spg.PrimaryGrid.Columns.Add(gd);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        public static void ColumnCheckBox(this SuperGridControl spg, string name, string text, int width = 100, bool visible = true, bool read = false)
        {
            try
            {
                GridColumn gd = new GridColumn();
                gd = new GridColumn();
                gd.Name = name;
                gd.DataPropertyName = name;
                gd.HeaderText = text;
                gd.Width = width;
                gd.Visible = visible;
                gd.ReadOnly = read;
                gd.ColumnSortMode = ColumnSortMode.Single;
                gd.SortCycle = SortCycle.NotSet;
                gd.SortIndicator = SortIndicator.Auto;
                gd.EditorType = typeof(GridCheckBoxXEditControl);
                spg.PrimaryGrid.Columns.Add(gd);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        public static void ColumnAdd(this GridPanel spg, string name, string text, int width = 100,
                                            bool visible = true, bool read = false, bool columnsort = true)
        {
            try
            {
                GridColumn gd = new GridColumn();
                gd = new GridColumn();
                gd.Name = name;
                gd.DataPropertyName = name;
                gd.HeaderText = text;
                gd.Width = width;
                gd.Visible = visible;
                gd.ReadOnly = read;
                if (columnsort)
                {
                    gd.ColumnSortMode = ColumnSortMode.Single;
                    gd.SortCycle = SortCycle.NotSet;
                    gd.SortIndicator = SortIndicator.Auto;
                }
                else
                {
                    gd.ColumnSortMode = ColumnSortMode.None;
                    gd.SortCycle = SortCycle.NotSet;
                    gd.SortIndicator = SortIndicator.None;
                }
                spg.Columns.Add(gd);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        public static string RowValueStr(this GridRow row, string name)
        {
            string result = "";
            try
            {
                if (row[name] != null && row[name].Value != null) result = row[name].Value.ToString();
            }
            catch (Exception ex)
            {
                result = "";
                XTrace.WriteException(ex);
            }
            return result;
        }

        public static int RowValueInt(this GridRow row, string name)
        {
            int result = 0;
            try
            {
                result = row[name].Value.ToString().ToInt();
            }
            catch (Exception ex)
            {
                result = 0;
                XTrace.WriteException(ex);
            }
            return result;
        }

        public static void CellValue(this GridRow row, string name, string text)
        {
            try
            {
                row[name].Value = text;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        public static Tuple<bool, T> CheckOneModel<T>(this SuperGridControl spg, T t)
        {
            bool result = false;

            try
            {
                if (spg.PrimaryGrid.SelectedCellCount < 1)
                {
                    return new Tuple<bool, T>(result, t);
                }

                foreach (var item in spg.PrimaryGrid.SelectedCells)
                {
                    var ot = (T)((GridCell)item).GridRow.DataItem;
                    if (ot != null && t.PGetValue("serialCode") == null)
                    {
                        result = true;
                        t = ot;
                    }
                    else
                    {
                        if (ot.PGetValue("serialCode").ToString() != t.PGetValue("serialCode").ToString())
                        {
                            result = false;
                            return new Tuple<bool, T>(result, t);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
                XTrace.WriteException(ex);
            }
            return new Tuple<bool, T>(result, t);
        }

        public static object PGetValue(this object obj, string fieldName)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(fieldName); //获取指定名称的属性             
            return propertyInfo.GetValue(obj, null);
        }

    }
}
