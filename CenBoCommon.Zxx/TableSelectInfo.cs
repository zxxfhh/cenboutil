using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CenBoCommon.Zxx
{
    public static class TableSelectInfo<T>
    {
        private static List<TableColumn> GetFieldNames()
        {
            List<TableColumn> list = new List<TableColumn>();
            PropertyInfo[] tmod = typeof(T).GetProperties();
            foreach (PropertyInfo fi in tmod)
            {
                var attrs = fi.CustomAttributes;
                if (!attrs.Any(t => t.AttributeType.Name.ToLower().Contains("sugarcolumn")))
                {
                    if (attrs.Any(t => t.AttributeType.Name.ToLower().Contains("displayname")))
                    {
                        TableColumn column = new TableColumn()
                        {
                            ParamName = fi.Name,
                            FieldName = fi.Name,
                            IsString = fi.PropertyType.Name.ToLower() == "string"
                        };
                        if (fi.PropertyType.Name.ToLower().Contains("date")) column.IsTime = true;
                        if (column.ParamName.ToLower().Contains("time")
                           && !column.ParamName.ToLower().Contains("checktime")) column.IsTime = true;
                        if (!list.Any(t => t.ParamName.ToLower() == column.ParamName.ToLower()))
                            list.Add(column);
                    }
                }
                else
                {
                    TableColumn column = new TableColumn()
                    {
                        ParamName = fi.Name,
                        IsString = fi.PropertyType.Name.ToLower() == "string",
                        IsTime = fi.PropertyType.Name.ToLower().Contains("date")
                    };
                    var cusa = fi.CustomAttributes.FirstOrDefault(t => t.AttributeType.Name.ToLower().Contains("sugarcolumn"));
                    if (cusa.NamedArguments.Count >= 1)
                    {
                        var names = cusa.NamedArguments.ToList();
                        if (names.Any(t => t.MemberName == "ColumnName"))
                            column.FieldName = names.Find(t => t.MemberName == "ColumnName").TypedValue.Value.ToString();
                        if (names.Any(t => t.MemberName == "IsPrimaryKey")) column.IsPrimaryKey = true;
                        if (!list.Any(t => t.ParamName.ToLower() == column.ParamName.ToLower()))
                            list.Add(column);
                    }
                }
            }

            return list;
        }

        public static Tuple<string, string> GetSqlModel(ActionPara model)
        {
            string orderby = string.Empty;
            string wherestr = string.Empty;
            List<string> orderlist = new List<string>();
            List<string> wherelist = new List<string>();
            List<string> wheregrouplist = new List<string>();

            var fieldlist = GetFieldNames();

            var sortlsit = model.sconlist.FindAll(t => t.ParamSort > 0);
            if (sortlsit.Any())
            {
                sortlsit.ForEach(scn =>
                {
                    if (scn.ParamSort == 1)
                    {
                        orderlist.Add($"{scn.ParamName} asc");
                    }
                    else if (scn.ParamSort == 2)
                    {
                        orderlist.Add($"{scn.ParamName} desc");
                    }
                });
            }
            else
            {
                var _fieldlist = fieldlist.FindAll(k => k.IsPrimaryKey);
                if (_fieldlist.Any())
                {
                    _fieldlist.ForEach(field =>
                    {
                        if (field.ParamName.ToLower() == "id")
                        {
                            orderlist.Add($" {field.ParamName} desc");
                        }
                        else if (field.ParamName.ToLower() == "snowid")
                        {
                            orderlist.Add($" {field.ParamName} desc");
                        }
                        else
                        {
                            orderlist.Add($" {field.ParamName} desc");
                        }
                    });
                }
            }

            if (!model.starttime.IsZxxNullOrEmpty())
            {
                var field = fieldlist.Find(k => k.IsPrimaryKey && k.ParamName.ToLower() == "snowid");
                if (field != null)
                {
                    long min = SnowModel.Instance.NewId(model.starttime.ToZxxDateTime());
                    wherelist.Add($" {field.ParamName} >= {min} ");
                }
                else
                {
                    var fieldtime = fieldlist.Find(k => k.IsTime);
                    if (fieldtime != null)
                    {
                        wherelist.Add($" {fieldtime.ParamName} >= '{model.starttime}' ");
                    }
                }
            }

            if (!model.endtime.IsZxxNullOrEmpty())
            {
                var field = fieldlist.Find(k => k.IsPrimaryKey && k.ParamName.ToLower() == "snowid");
                if (field != null)
                {
                    long max = SnowModel.Instance.NewId(model.endtime.ToZxxDateTime());
                    wherelist.Add($" {field.ParamName} <= {max} ");
                }
                else
                {
                    var fieldtime = fieldlist.Find(k => k.IsTime);
                    if (fieldtime != null)
                    {
                        wherelist.Add($" {fieldtime.ParamName} <= '{model.endtime}' ");
                    }
                }
            }

            //未分组条件
            var _sconlist = model.sconlist.FindAll(t => t.ParamGroupName.IsZxxNullOrEmpty() && !t.ParamType.IsZxxNullOrEmpty());
            if (_sconlist.Any())
            {
                _sconlist.ForEach(t =>
                {
                    var field = fieldlist.Find(k => k.ParamName.ToLower() == t.ParamName.ToLower());
                    if (field != null && !t.ParamValue.IsZxxNullOrEmpty())
                    {
                        if (field.IsString)
                        {
                            if (t.ParamType == "like")
                            {
                                if (t.ParamValue.Contains("%"))
                                {
                                    wherelist.Add($" {field.ParamName} {t.ParamType} '{t.ParamValue}' ");
                                }
                                else
                                {
                                    wherelist.Add($" {field.ParamName} {t.ParamType} '%{t.ParamValue}%' ");
                                }
                            }
                            else
                            {
                                if (t.ParamType == "in")
                                {
                                    wherelist.Add($" {field.ParamName} {t.ParamType} ({t.ParamValue}) ");
                                }
                                else
                                {
                                    wherelist.Add($" {field.ParamName} {t.ParamType} '{t.ParamValue}' ");
                                }
                            }
                        }
                        else
                        {
                            if (t.ParamType == "in")
                            {
                                wherelist.Add($" {field.ParamName} {t.ParamType} ({t.ParamValue}) ");
                            }
                            else
                            {
                                wherelist.Add($" {field.ParamName} {t.ParamType} {t.ParamValue} ");
                            }
                        }
                    }
                });
            }

            //分组条件
            var othersconlist = model.sconlist.FindAll(t => !t.ParamGroupName.IsZxxNullOrEmpty() && !t.ParamType.IsZxxNullOrEmpty());
            if (othersconlist.Any())
            {
                var grouplist = othersconlist.GroupBy(t => t.ParamGroupName).ToList();
                if (grouplist.Any())
                {
                    List<string> groupkeylist = new List<string>();
                    grouplist.ForEach(group =>
                    {
                        if (!groupkeylist.Contains(group.Key)) groupkeylist.Add(group.Key);
                    });
                    groupkeylist.ForEach(key =>
                    {
                        var _grouplist = model.sconlist.FindAll(t => t.ParamGroupName == key && !t.ParamType.IsZxxNullOrEmpty());
                        if (_grouplist.Any())
                        {
                            var first = _grouplist.Find(t => t.IsGroupFrist);
                            if (first != null)
                            {
                                var field = fieldlist.Find(k => k.ParamName.ToLower() == first.ParamName.ToLower());
                                if (field != null && !first.ParamValue.IsZxxNullOrEmpty())
                                {
                                    if (field.IsString)
                                    {
                                        wheregrouplist.Add($" {first.GroupCondition} ({field.ParamName} {first.ParamType} '{first.ParamValue}' ");
                                    }
                                    else
                                    {
                                        if (first.ParamType == "in")
                                        {
                                            wheregrouplist.Add($" {first.GroupCondition} ({field.ParamName} {first.ParamType} ({first.ParamValue}) ");
                                        }
                                        else
                                        {
                                            wheregrouplist.Add($" {first.GroupCondition} ({field.ParamName} {first.ParamType} {first.ParamValue} ");
                                        }
                                    }
                                }
                            }
                            _grouplist.FindAll(t => !t.IsGroupFrist).ForEach(t =>
                            {
                                var field = fieldlist.Find(k => k.ParamName.ToLower() == t.ParamName.ToLower());
                                if (field != null && !first.ParamValue.IsZxxNullOrEmpty())
                                {
                                    if (field.IsString)
                                    {
                                        if (t.ParamType == "in")
                                        {
                                            wheregrouplist.Add($" {t.GroupCondition} {field.ParamName} {t.ParamType} ({t.ParamValue}) ");
                                        }
                                        else
                                        {
                                            wheregrouplist.Add($" {t.GroupCondition} {field.ParamName} {t.ParamType} '{t.ParamValue}' ");
                                        }


                                    }
                                    else
                                    {
                                        if (t.ParamType == "in")
                                        {
                                            wheregrouplist.Add($" {t.GroupCondition} {field.ParamName} {t.ParamType} ({t.ParamValue}) ");
                                        }
                                        else
                                        {
                                            wheregrouplist.Add($" {t.GroupCondition} {field.ParamName} {t.ParamType} {t.ParamValue} ");
                                        }
                                    }

                                }
                            });
                            wheregrouplist[wheregrouplist.Count - 1] = wheregrouplist[wheregrouplist.Count - 1] + ")";
                        }
                    });
                }
            }

            orderby = string.Join(",", orderlist);
            wherestr = string.Join(" and ", wherelist) + string.Join(" ", wheregrouplist);

            return new Tuple<string, string>(wherestr, orderby);
        }

    }
}
