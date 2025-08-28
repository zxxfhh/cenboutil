using CenBoCommon.Zxx;
using NewLife;
using SqlSugar;
using System.Linq.Expressions;
using System.Reflection;

namespace IdentityServerModel
{
    public class SqlSugarHelper
    {
        public static string SqlSugar = "";
        public static string SqlError = "";

        //用单例模式   AllowLoadLocalInfile=true;(启用bulkCopy=大数据快速入库，报错：MYSQL库中执行：SET GLOBAL local_infile=1)
        public static SqlSugarScope Db = new SqlSugarScope(new ConnectionConfig()
        {
            ConnectionString = DbSetting.Current.MysqlConString,
            DbType = DbType.MySql,
            InitKeyType = InitKeyType.Attribute,//从特性读取主键和自增列信息
            IsAutoCloseConnection = true    //开启自动释放模式和EF原理一样我就不多解释了
        }, db =>
        {
            //SQL执行完
            db.Aop.OnLogExecuted = (sql, pars) =>
            {
                SqlError = "";
                SqlSugar = SugarSqlFormat.FormatParam(sql, pars);
            };
            //SQL报错
            db.Aop.OnError = (exp) =>
            {
                SqlSugar = "";
                SqlError = SugarSqlFormat.FormatParam(exp.Sql, exp.Parametres);
            };
        });

        private static Dictionary<string, ConditionalType> _diccontype = new();
        /// <summary>
        /// 查询条件操作符
        /// </summary>
        public static Dictionary<string, ConditionalType> diccontype
        {
            get
            {
                lock (_diccontype)
                {
                    if (_diccontype.Count == 0)
                    {
                        _diccontype.Add("=", ConditionalType.Equal);
                        _diccontype.Add("!=", ConditionalType.NoEqual);
                        _diccontype.Add(">", ConditionalType.GreaterThan);
                        _diccontype.Add(">=", ConditionalType.GreaterThanOrEqual);
                        _diccontype.Add("<", ConditionalType.LessThan);
                        _diccontype.Add("<=", ConditionalType.LessThanOrEqual);
                        _diccontype.Add("in", ConditionalType.In);
                        _diccontype.Add("notin", ConditionalType.NotIn);
                        _diccontype.Add("like", ConditionalType.Like);
                        _diccontype.Add("isnull", ConditionalType.IsNullOrEmpty);
                    }
                }
                return _diccontype;
            }
        }

        private static Dictionary<string, WhereType> _wheretype = new();
        /// <summary>
        /// 查询条件操作符
        /// </summary>
        public static Dictionary<string, WhereType> wheretype
        {
            get
            {
                lock (_wheretype)
                {
                    if (_wheretype.Count == 0)
                    {
                        _wheretype.Add("and", WhereType.And);
                        _wheretype.Add("or", WhereType.Or);
                        _wheretype.Add("null", WhereType.Null);
                    }
                }
                return _wheretype;
            }
        }
    }

    public class DbContext<T> where T : class, new()
    {
        public string sqlSugar
        {
            get
            {
                return SqlSugarHelper.SqlSugar;
            }
        }
        public string sqlError
        {
            get
            {
                return SqlSugarHelper.SqlError;
            }
        }
        public SqlSugarScope Db = SqlSugarHelper.Db; //在建一个类
        private Dictionary<string, ConditionalType> diccontype = SqlSugarHelper.diccontype;
        private Dictionary<string, WhereType> wheretype = SqlSugarHelper.wheretype;

        /// <summary>
        /// 获取Order by语句
        /// </summary>
        /// <param name="sconlist"></param>
        /// <returns></returns>
        private string GetSqlOrderByCondition(List<SelectCondition> sconlist)
        {
            List<string> sqllist = new();
            try
            {
                sconlist.FindAll(t => t.ParamSort > 0).ForEach(scn =>
                {
                    if (scn.ParamSort == 1)
                    {
                        sqllist.Add($"{scn.ParamName} asc");
                    }
                    else if (scn.ParamSort == 2)
                    {
                        sqllist.Add($"{scn.ParamName} desc");
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
            return string.Join(" , ", sqllist);
        }

        private List<TableColumn> GetFieldNames()
        {
            List<TableColumn> list = new();
            PropertyInfo[] tmod = typeof(T).GetProperties();
            foreach (PropertyInfo fi in tmod)
            {
                foreach (var cusa in fi.CustomAttributes)
                {
                    if (cusa.AttributeType.Name == "SugarColumn")
                    {
                        TableColumn column = new()
                        {
                            ParamName = fi.Name
                        };

                        if (cusa.NamedArguments.Count >= 1)
                        {
                            string ColumnName = cusa.NamedArguments[0].MemberName;
                            if (ColumnName == "ColumnName")
                            {
                                column.FieldName = cusa.NamedArguments[0].TypedValue.Value.ToString();
                                if (column.FieldName.ToLower().Contains("time")
                                && !column.FieldName.ToLower().Contains("checktime"))
                                {
                                    column.IsTime = true;
                                }
                            }

                            if (cusa.NamedArguments.Count >= 2)
                            {
                                if (cusa.NamedArguments[1].MemberName == "IsPrimaryKey")
                                {
                                    column.IsPrimaryKey = true;
                                }
                            }
                            list.Add(column);
                        }
                    }
                }
            }

            return list;
        }

        public Tuple<List<IConditionalModel>, string> GetSqlModel(ActionPara model)
        {
            string orderby = string.Empty;
            List<IConditionalModel> conModel = new();
            var fieldlist = GetFieldNames();
            if (model.sconlist.Count > 0)
            {
                var sortlsit = model.sconlist.FindAll(t => t.ParamSort > 0);
                if (sortlsit.Any())
                {
                    orderby = GetSqlOrderByCondition(sortlsit);
                }
                else
                {
                    var _fieldlist = fieldlist.FindAll(k => k.IsPrimaryKey);
                    if (_fieldlist.Any())
                    {
                        List<string> orderbyList = new();
                        _fieldlist.ForEach(field =>
                        {
                            if (field.FieldName.ToLower() == "id")
                            {
                                orderbyList.Add($" {field.FieldName} desc");
                            }
                            else if (field.FieldName.ToLower() == "snowid")
                            {
                                orderbyList.Add($" {field.FieldName} desc");
                            }
                            else
                            {
                                orderbyList.Add($" {field.FieldName} desc");
                            }
                        });
                        if (orderbyList.Any())
                        {
                            orderby = string.Join(",", orderbyList);
                        }
                    }
                }

                //未分组条件
                var _sconlist = model.sconlist.FindAll(t => t.ParamGroupName.IsNullOrEmpty() && !t.ParamType.IsNullOrEmpty());
                if (_sconlist.Any())
                {
                    _sconlist.ForEach(t =>
                    {
                        var field = fieldlist.Find(k => k.ParamName.ToLower() == t.ParamName.ToLower());
                        if (field != null && !string.IsNullOrEmpty(t.ParamValue))
                        {
                            conModel.Add(new ConditionalModel()
                            {
                                FieldName = field.FieldName,
                                ConditionalType = diccontype[t.ParamType],
                                FieldValue = t.ParamValue
                            });
                        }
                    });
                }
                //分组条件
                var othersconlist = model.sconlist.FindAll(t => !t.ParamGroupName.IsNullOrEmpty() && !t.ParamType.IsNullOrEmpty());
                if (othersconlist.Any())
                {
                    var grouplist = othersconlist.GroupBy(t => t.ParamGroupName).ToList();
                    if (grouplist.Any())
                    {
                        List<string> groupkeylist = new();
                        grouplist.ForEach(group =>
                        {
                            if (!groupkeylist.Contains(group.Key)) groupkeylist.Add(group.Key);
                        });
                        groupkeylist.ForEach(key =>
                        {
                            var _grouplist = model.sconlist.FindAll(t => t.ParamGroupName == key && !t.ParamType.IsNullOrEmpty());
                            if (_grouplist.Any())
                            {
                                List<KeyValuePair<WhereType, ConditionalModel>> ConditionalList = new();
                                var first = _grouplist.Find(t => t.IsGroupFrist);
                                if (first != null && !string.IsNullOrEmpty(first.ParamValue))
                                {
                                    var field = fieldlist.Find(k => k.ParamName.ToLower() == first.ParamName.ToLower());
                                    if (field != null)
                                    {
                                        ConditionalList.Add(new KeyValuePair<WhereType, ConditionalModel>(
                                           wheretype[first.GroupCondition],
                                           new ConditionalModel()
                                           {
                                               FieldName = field.FieldName,
                                               ConditionalType = diccontype[first.ParamType],
                                               FieldValue = first.ParamValue
                                           }
                                        ));
                                    }
                                }
                                _grouplist.FindAll(t => !t.IsGroupFrist).ForEach(t =>
                                {
                                    var field = fieldlist.Find(k => k.ParamName.ToLower() == t.ParamName.ToLower());
                                    if (field != null && !string.IsNullOrEmpty(t.ParamValue))
                                    {
                                        ConditionalList.Add(new KeyValuePair<WhereType, ConditionalModel>(
                                           wheretype[t.GroupCondition],
                                           new ConditionalModel()
                                           {
                                               FieldName = field.FieldName,
                                               ConditionalType = diccontype[t.ParamType],
                                               FieldValue = t.ParamValue
                                           }
                                        ));
                                    }
                                });
                                conModel.Add(new ConditionalCollections
                                {
                                    ConditionalList = ConditionalList
                                });
                            }
                        });
                    }
                }
            }

            if (!string.IsNullOrEmpty(model.starttime))
            {
                var field = fieldlist.Find(k => k.IsPrimaryKey && k.FieldName.ToLower() == "snowid");
                if (field != null)
                {
                    long min = SnowModel.Instance.NewId(model.starttime.ToDateTime());
                    conModel.Add(new ConditionalModel()
                    {
                        FieldName = field.FieldName,
                        ConditionalType = ConditionalType.GreaterThanOrEqual,
                        FieldValue = min + ""
                    });
                }
                else
                {
                    var fieldtime = fieldlist.Find(k => k.IsTime);
                    if (fieldtime != null)
                    {
                        conModel.Add(new ConditionalModel()
                        {
                            FieldName = fieldtime.FieldName,
                            ConditionalType = ConditionalType.GreaterThanOrEqual,
                            FieldValue = model.starttime
                        });
                    }
                }
            }
            if (!string.IsNullOrEmpty(model.endtime))
            {
                var field = fieldlist.Find(k => k.IsPrimaryKey && k.FieldName.ToLower() == "snowid");
                if (field != null)
                {
                    long max = SnowModel.Instance.NewId(model.endtime.ToDateTime());
                    conModel.Add(new ConditionalModel()
                    {
                        FieldName = field.FieldName,
                        ConditionalType = ConditionalType.LessThanOrEqual,
                        FieldValue = max + ""
                    });
                }
                else
                {
                    var fieldtime = fieldlist.Find(k => k.IsTime);
                    if (fieldtime != null)
                    {
                        conModel.Add(new ConditionalModel()
                        {
                            FieldName = fieldtime.FieldName,
                            ConditionalType = ConditionalType.LessThanOrEqual,
                            FieldValue = model.endtime
                        });
                    }
                }
            }

            return new Tuple<List<IConditionalModel>, string>(conModel, orderby);
        }

        public virtual T GetSingle(Expression<Func<T, bool>> whereExpression)
        {
            try
            {
                return Db.Queryable<T>().Where(whereExpression).First();
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

        /// <summary>
        /// 获取所有
        /// </summary>
        /// <returns></returns>
        public virtual List<T> GetList()
        {
            try
            {
                return Db.Queryable<T>().ToList();
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

        /// <summary>
        /// 根据表达式查询
        /// </summary>
        /// <returns></returns>
        public virtual List<T> GetList(Expression<Func<T, bool>> whereExpression)
        {
            try
            {
                return Db.Queryable<T>().Where(whereExpression).ToList();
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

        public virtual List<T> GetList(List<IConditionalModel> conModel)
        {
            try
            {
                return Db.Queryable<T>().Where(conModel).ToList();
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

        /// <summary>
        /// 根据表达式查询分页
        /// </summary>
        /// <returns></returns>
        public virtual List<T> GetPageList(Expression<Func<T, bool>> whereExpression, PageModel page)
        {
            try
            {
                int totalNumber = 0;
                List<T> result = Db.Queryable<T>().Where(whereExpression).ToPageList(page.PageIndex, page.PageSize, ref totalNumber);
                page.TotalCount = totalNumber;
                return result;
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

        public virtual List<T> GetPageList(List<IConditionalModel> conModel, PageModel page)
        {
            try
            {
                int totalNumber = 0;
                List<T> result = Db.Queryable<T>().Where(conModel).ToPageList(page.PageIndex, page.PageSize, ref totalNumber);
                page.TotalCount = totalNumber;
                return result;
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

        /// <summary>
        /// 根据表达式查询分页并排序
        /// </summary>
        /// <param name="whereExpression">it</param>
        /// <param name="pageModel"></param>
        /// <param name="orderByExpression">it=>it.id或者it=>new{it.id,it.name}</param>
        /// <param name="orderByType">OrderByType.Desc</param>
        /// <returns></returns>
        public virtual List<T> GetPageList(Expression<Func<T, bool>> whereExpression, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
        {
            try
            {
                int totalNumber = 0;
                List<T> result = Db.Queryable<T>().OrderByIF(orderByExpression != null, orderByExpression, orderByType)
                    .Where(whereExpression)
                    .ToPageList(page.PageIndex, page.PageSize, ref totalNumber);
                page.TotalCount = totalNumber;
                return result;
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

        /// <summary>
        /// 根据主键查询
        /// </summary>
        /// <returns></returns>
        public virtual T GetById(dynamic id)
        {
            try
            {
                return Db.Queryable<T>().InSingle(id);
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

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual bool Delete(dynamic id)
        {
            try
            {
                return Db.Deleteable<T>().In(id).ExecuteCommand() > 0;
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

        /// <summary>
        /// 根据实体删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual bool Delete(T data)
        {
            try
            {
                return Db.Deleteable<T>().Where(data).ExecuteCommand() > 0;
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

        /// <summary>
        /// 根据主键删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual bool Delete(dynamic[] ids)
        {
            try
            {
                return Db.Deleteable<T>().In(ids).ExecuteCommand() > 0;
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

        /// <summary>
        /// 根据表达式删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual bool Delete(Expression<Func<T, bool>> whereExpression)
        {
            try
            {
                return Db.Deleteable<T>().Where(whereExpression).ExecuteCommand() > 0;
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

        /// <summary>
        /// 根据实体更新，实体需要有主键
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual bool Update(T obj)
        {
            try
            {
                return Db.Updateable(obj).ExecuteCommand() > 0;
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

        public virtual bool UpdateColumns(T obj, Expression<Func<T, object>> column)
        {
            try
            {
                var count = Db.Updateable(obj).UpdateColumns(column).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool UpdateColumns(List<T> updateObjs, Expression<Func<T, object>> column)
        {
            try
            {
                var count = Db.Updateable(updateObjs).UpdateColumns(column).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool UpdateColumns(T obj, Expression<Func<T, object>> column, Expression<Func<T, bool>> where)
        {
            try
            {
                var count = Db.Updateable(obj).UpdateColumns(column).Where(where).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool UpdateColumns(List<T> updateObjs, Expression<Func<T, object>> column, Expression<Func<T, bool>> where)
        {
            try
            {
                var count = Db.Updateable(updateObjs).UpdateColumns(column).Where(where).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool UpdateIgnoreColumns(T obj, Expression<Func<T, object>> column)
        {
            try
            {
                var count = Db.Updateable(obj).IgnoreColumns(column).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool UpdateIgnoreColumns(List<T> updateObjs, Expression<Func<T, object>> column)
        {
            try
            {
                var count = Db.Updateable(updateObjs).IgnoreColumns(column).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool UpdateIgnoreColumns(T obj, Expression<Func<T, object>> column, Expression<Func<T, bool>> where)
        {
            try
            {
                var count = Db.Updateable(obj).IgnoreColumns(column).Where(where).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool UpdateIgnoreColumns(List<T> updateObjs, Expression<Func<T, object>> column, Expression<Func<T, bool>> where)
        {
            try
            {
                var count = Db.Updateable(updateObjs).IgnoreColumns(column).Where(where).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool UpdateRange(List<T> updateObjs)
        {
            try
            {
                var count = Db.Updateable(updateObjs).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        /// <summary>
        /// 大数据批量更新
        /// </summary>
        /// <param name="updateObjs"></param>
        /// <returns></returns>
        public virtual bool UpdateRangeFast(List<T> updateObjs)
        {
            try
            {
                return Db.Fastest<T>().PageSize(20000).BulkUpdate(updateObjs) > 0;
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

        public virtual bool Insert(T obj)
        {
            try
            {
                var count = Db.Insertable(obj).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool InsertRange(List<T> insertObjs)
        {
            try
            {
                var count = Db.Insertable(insertObjs).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool InsertColumns(T obj, Expression<Func<T, object>> column)
        {
            try
            {
                var count = Db.Insertable(obj).InsertColumns(column).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool InsertRangeColumns(List<T> updateObjs, Expression<Func<T, object>> column)
        {
            try
            {
                var count = Db.Insertable(updateObjs).InsertColumns(column).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool InsertIgnoreColumns(T obj, Expression<Func<T, object>> column)
        {
            try
            {
                var count = Db.Insertable(obj).IgnoreColumns(column).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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

        public virtual bool InsertIgnoreColumns(List<T> updateObjs, Expression<Func<T, object>> column)
        {
            try
            {
                var count = Db.Insertable(updateObjs).IgnoreColumns(column).ExecuteCommand();
                var aa = sqlSugar;
                return count > 0;
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


        /// <summary>
        /// 大数据批量插入
        /// </summary>
        /// <param name="insertObjs"></param>
        /// <returns></returns>
        public virtual bool InsertRangeFast(List<T> insertObjs)
        {
            try
            {
                return Db.Fastest<T>().PageSize(20000).BulkCopy(insertObjs) > 0;
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

        public virtual int InsertReturnIdentity(T insertObj)
        {
            try
            {
                return Db.Insertable(insertObj).ExecuteReturnIdentity();
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

        public virtual T InsertReturnEntity(T insertObj)
        {
            try
            {
                return Db.Insertable(insertObj).ExecuteReturnEntity();
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

        private string GetDebuggerDisplay()
        {
            return ToString();
        }

        /// <summary>
        /// 事务函数
        /// </summary>
        /// <param name="tranAction"></param>
        /// <returns></returns>
        public virtual bool TranAction(Action tranAction)
        {
            bool isresult = false;
            try
            {
                Db.BeginTran();

                tranAction();

                Db.CommitTran();
                isresult = true;
            }
            catch
            {
                Db.RollbackTran();
            }

            return isresult;
        }

        public virtual T GetOneBy(Expression<Func<T, bool>> wheres)
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

        public virtual List<T> GetListBy(Expression<Func<T, bool>> wheres)
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

        public virtual bool Save(T info)
        {
            bool isok = false;
            try
            {
                var rl = Db.Storageable(info).ToStorage();
                rl.AsInsertable.ExecuteCommand();
                rl.AsUpdateable.ExecuteCommand();
                isok = true;
                var aa = sqlSugar;
                return isok;
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

        public virtual bool SaveBatch(List<T> list)
        {
            bool isok = false;
            try
            {
                var rl = Db.Storageable(list).ToStorage();
                rl.AsInsertable.ExecuteCommand();
                rl.AsUpdateable.ExecuteCommand();
                isok = true;
                var aa = sqlSugar;
                return isok;
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

        public virtual bool SaveBatchIgnoreColumns(List<T> list, Expression<Func<T, object>> column)
        {
            bool isok = false;
            try
            {
                var rl = Db.Storageable(list).ToStorage();
                rl.AsInsertable.IgnoreColumns(column).ExecuteCommand();
                rl.AsUpdateable.IgnoreColumns(column).ExecuteCommand();
                isok = true;
                var aa = sqlSugar;
                return isok;
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

        public virtual bool DeleteBy(Expression<Func<T, bool>> wheres)
        {
            try
            {
                var list = Delete(wheres);
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

        public virtual Tuple<bool, int> InsertReturnPk(T info)
        {
            bool isok = false;
            try
            {
                int id = InsertReturnIdentity(info);
                if (id > 0)
                {
                    isok = true;
                }
                var aa = sqlSugar;
                return new Tuple<bool, int>(isok, id);
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

        public virtual Tuple<bool, T> InsertReturnEntityT(T info)
        {
            bool isok = false;
            try
            {
                var entity = InsertReturnEntity(info);
                if (entity != null)
                {
                    isok = true;
                }
                var aa = sqlSugar;
                return new Tuple<bool, T>(isok, entity);
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

        public virtual List<T> GetListByPage(ActionPara model, ref int total)
        {
            try
            {
                var sqlmodel = GetSqlModel(model);
                int totalNumber = 0;
                var list = Db.Queryable<T>()
                        .Where(sqlmodel.Item1)
                        .OrderByIF(!string.IsNullOrWhiteSpace(sqlmodel.Item2), sqlmodel.Item2)
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
}
