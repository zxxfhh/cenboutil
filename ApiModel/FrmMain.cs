using ApiModel.Com;
using DevComponents.DotNetBar.SuperGrid;
using NewLife;
using Newtonsoft.Json;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WaitWindowForm;

namespace ApiModel
{
    public partial class FrmMain : Form
    {
        #region 属性

        private static FrmMain instance;
        /// <summary>
        /// 窗体唯一实例化一次
        /// </summary>
        public static FrmMain Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FrmMain();
                }
                return instance;
            }
        }

        public static void Delay(int ms)
        {
            int start = Environment.TickCount;
            while (Math.Abs(Environment.TickCount - start) < ms)
            {
                System.Windows.Forms.Application.DoEvents();
            }
        }

        #endregion

        private DbContext _DbContext = null;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            this.txtdb.Text = ConfigurationManager.AppSettings["dbcon"].ToString();
            this.txtormname.Text = ConfigurationManager.AppSettings["ormname"].ToString();
            this.txtControllername.Text = ConfigurationManager.AppSettings["controllername"].ToString();

            #region spg列设置
            spg.PrimaryGrid.MinRowHeight = 35;
            spg.PrimaryGrid.Filter.Visible = true;
            spg.PrimaryGrid.Filter.RowHeight = 35;
            spg.PrimaryGrid.FilterMatchType = FilterMatchType.RegularExpressions; //模糊匹配
            spg.PrimaryGrid.ColumnAutoSizeMode = ColumnAutoSizeMode.None;
            //控制表格可以选中多行
            spg.PrimaryGrid.MultiSelect = true;
            spg.PrimaryGrid.InitialSelection = RelativeSelection.PrimaryCell;
            //只能选中一个单元格，而不是一行单元格
            spg.PrimaryGrid.SelectionGranularity = SelectionGranularity.Cell;
            //是否显示序列号
            spg.PrimaryGrid.ShowRowHeaders = true;
            spg.PrimaryGrid.ShowRowGridIndex = true;
            spg.PrimaryGrid.RowHeaderIndexOffset = 1;//设置行号的开始值
            spg.PrimaryGrid.RowHeaderWidth = 30;
            //允许单元格拖动成为集合组
            spg.PrimaryGrid.ColumnHeader.AllowSelection = true;
            spg.PrimaryGrid.GroupByRow.Visible = false;
            //设置表格中文字的位置居中     
            spg.PrimaryGrid.DefaultVisualStyles.CellStyles.Default.Alignment = DevComponents.DotNetBar.SuperGrid.Style.Alignment.MiddleCenter;
            spg.PrimaryGrid.DefaultVisualStyles.CellStyles.Default.AllowMultiLine = DevComponents.DotNetBar.SuperGrid.Style.Tbool.True;
            spg.PrimaryGrid.DefaultVisualStyles.CellStyles.Default.AllowWrap = DevComponents.DotNetBar.SuperGrid.Style.Tbool.True;
            spg.PrimaryGrid.EnableFiltering = true;//让列头显示筛选图标
            spg.PrimaryGrid.EnableColumnFiltering = true;//让列头显示筛选图标
            spg.PrimaryGrid.AllowRowHeaderResize = true;
            spg.PrimaryGrid.AllowRowResize = true;
            spg.PrimaryGrid.ReadOnly = false;

            spg.PrimaryGrid.Columns.Clear();
            spg.ColumnAdd("ClassCheck", "选择", 60);
            spg.ColumnAdd("ClassName", "类名称", 190, true, true);
            spg.ColumnAdd("TableName", "表名称", 190, true, true);
            spg.ColumnAdd("IsView", "视图", 80, true, true);
            spg.ColumnAdd("Description", "表注释", 230, true, true);
            spg.PrimaryGrid.Columns["ClassCheck"].EditorType = typeof(DevComponents.DotNetBar.SuperGrid.GridCheckBoxEditControl);
            spg.PrimaryGrid.Columns["IsView"].EditorType = typeof(DevComponents.DotNetBar.SuperGrid.GridCheckBoxEditControl);

            spg.FilterPopupOpening += new EventHandler<GridFilterPopupOpeningEventArgs>((s, ec) => { ec.Cancel = true; });
            #endregion

        }

        private void btnall_Click(object sender, EventArgs e)
        {
            var detaillist = this.spg.PrimaryGrid.DataSource as List<CodeTable>;
            if (detaillist != null && detaillist.Count > 0)
            {
                detaillist.ForEach(t =>
                {
                    t.ClassCheck = 1;
                });
                spg.PrimaryGrid.DataSource = null;
                spg.PrimaryGrid.DataSource = detaillist;
            }
        }

        private void btnnotall_Click(object sender, EventArgs e)
        {
            var detaillist = this.spg.PrimaryGrid.DataSource as List<CodeTable>;
            if (detaillist != null && detaillist.Count > 0)
            {
                detaillist.ForEach(t =>
                {
                    t.ClassCheck = 0;
                });
                spg.PrimaryGrid.DataSource = null;
                spg.PrimaryGrid.DataSource = detaillist;
            }
        }

        private void btndb_Click(object sender, EventArgs e)
        {
            string dbstr = this.txtdb.Text.Trim();
            if (dbstr.IsNullOrEmpty())
            {
                MessageBox.Show("请输入数据库字符串");
                return;
            }

            if (chkMySql.Checked)
            {
                _DbContext = new DbContext(dbstr);
            }
            else if (chkSqlite.Checked)
            {
                _DbContext = new DbContext(dbstr, SqlSugar.DbType.Sqlite);
            }
            else if (chksqlserver.Checked)
            {
                _DbContext = new DbContext(dbstr, SqlSugar.DbType.SqlServer);
            }

            List<CodeTable> list = new List<CodeTable>();
            var tablelist = _DbContext.Db.DbMaintenance.GetTableInfoList(false);
            if (tablelist != null && tablelist.Count > 0)
            {
                tablelist.ForEach(t =>
                {
                    CodeTable table = new CodeTable();
                    table.Description = t.Description;
                    table.TableName = t.Name;
                    table.ClassName = PubMehtod.GetCsharpName(t.Name);
                    list.Add(table);
                });
            }
            var viewlist = _DbContext.Db.DbMaintenance.GetViewInfoList(false);
            if (viewlist != null && viewlist.Count > 0)
            {
                viewlist.ForEach(t =>
                {
                    CodeTable table = new CodeTable();
                    table.Description = t.Description;
                    table.TableName = t.Name;
                    table.IsView = true;
                    table.ClassName = PubMehtod.GetCsharpName(t.Name);
                    list.Add(table);
                });
            }

            spg.PrimaryGrid.DataSource = null;
            spg.PrimaryGrid.DataSource = list;
        }

        private void btnmuban_Click(object sender, EventArgs e)
        {
            if (this.spg.PrimaryGrid.Rows.Count == 0)
            {
                MessageBox.Show("请点击【DB连接】按钮");
                return;
            }
            if (this.txtormname.Text.Trim().IsNullOrEmpty())
            {
                MessageBox.Show("请输入ORM命名空间");
                return;
            }
            if (this.txtControllername.Text.Trim().IsNullOrEmpty())
            {
                MessageBox.Show("请输入Controllers命名空间");
                return;
            }

            try
            {
                this.lblmes.Text = "提示：";
                var _tableList = this.spg.PrimaryGrid.DataSource as List<CodeTable>;
                var tableList = _tableList.FindAll(t => t.ClassCheck == 1);
                if (tableList.Count == 0)
                {
                    MessageBox.Show("请选择具体数据表");
                    return;
                }

                var result = WaitWindow.Show(SyncData1, "数据处理中，请等待...", new object[] { });
                if ((bool)result)
                {
                    this.lblmes.Text = "提示：模板全部生成完成。";
                }
                else
                {
                    this.lblmes.Text = "提示：模板全部生成失败。";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void SyncData1(object sender, WaitWindowEventArgs e)
        {
            try
            {
                var obj = e.Arguments;

                string entitypathdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model");
                if (Directory.Exists(entitypathdir))
                {
                    Directory.Delete(entitypathdir, true);
                    Directory.CreateDirectory(entitypathdir);
                }

                if (ckbsolrhttp.CheckState == CheckState.Checked)
                {
                    GetSolrHttpEntitys();
                    GetSolrDAOs();
                    GetSolrControllers();
                }
                else
                {
                    GetEntitys();
                    GetDAOs();
                    GetControllers();
                }

                e.Result = true;
            }
            catch (Exception ex)
            {
                e.Result = false;
                throw ex;
            }
        }

        #region 获取SqlSugar数据类型
        private List<CodeType> GetCodeTypeList()
        {
            List<CodeType> codeTypelist = new List<CodeType>();

            codeTypelist.Add(GetCodeType("int", "int", "[{\"Name\":\"int\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"int4\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"number\",\"Length\":9,\"DecimalDigits\":0},{\"Name\":\"integer\",\"Length\":null,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("string10", "string", "[{\"Name\":\"varchar\",\"Length\":10,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("ignore", "建表忽略该类型字段，生成实体中@Model.IsIgnore 值为 true ", "[]", 1000));
            codeTypelist.Add(GetCodeType("string36", "string", "[{\"Name\":\"varchar\",\"Length\":36,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("string100", "string", "[{\"Name\":\"varchar\",\"Length\":100,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("string200", "string", "[{\"Name\":\"varchar\",\"Length\":200,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("string500", "string", "[{\"Name\":\"varchar\",\"Length\":500,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("string2000", "string", "[{\"Name\":\"varchar\",\"Length\":2000,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("nString10", "string", "[{\"Name\":\"nvarchar\",\"Length\":10,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":10,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("nString36", "string", "[{\"Name\":\"nvarchar\",\"Length\":36,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":36,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("nString100", "string", "[{\"Name\":\"nvarchar\",\"Length\":100,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":100,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("nString200", "string", "[{\"Name\":\"nvarchar\",\"Length\":200,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":200,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("nString500", "string", "[{\"Name\":\"nvarchar\",\"Length\":500,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":500,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("nString2000", "string", "[{\"Name\":\"nvarchar\",\"Length\":2000,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":2000,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("maxString", "string", "[{\"Name\":\"text\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"clob\",\"Length\":null,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("bool", "bool", "[{\"Name\":\"bit\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"number\",\"Length\":1,\"DecimalDigits\":null},{\"Name\":\"boolean\",\"Length\":null,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("DateTime", "DateTime", "[{\"Name\":\"datetime\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"date\",\"Length\":null,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("timestamp", "byte[]", "[{\"Name\":\"timestamp\",\"Length\":null,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("decimal_18_8", "decimal", "[{\"Name\":\"decimal\",\"Length\":18,\"DecimalDigits\":8},{\"Name\":\"number\",\"Length\":18,\"DecimalDigits\":8},{\"Name\":\"numeric\",\"Length\":18,\"DecimalDigits\":8}]", 1000));
            codeTypelist.Add(GetCodeType("decimal_18_4", "decimal", "[{\"Name\":\"decimal\",\"Length\":18,\"DecimalDigits\":4},{\"Name\":\"number\",\"Length\":18,\"DecimalDigits\":4},{\"Name\":\"money\",\"Length\":0,\"DecimalDigits\":0}]", 1000));
            codeTypelist.Add(GetCodeType("decimal_18_2", "decimal", "[{\"Name\":\"decimal\",\"Length\":18,\"DecimalDigits\":2},{\"Name\":\"number\",\"Length\":18,\"DecimalDigits\":2}]", 1000));
            codeTypelist.Add(GetCodeType("guid", "Guid", "[{\"Name\":\"uniqueidentifier\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"guid\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"char\",\"Length\":36,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("byte", "byte", "[{\"Name\":\"tinyint\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"varbit\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"number\",\"Length\":3,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("short", "short", "[{\"Name\":\"short\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"int2\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"int16\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"smallint\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"number\",\"Length\":5,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("long", "long", "[{\"Name\":\"long\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"int8\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"int64\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"bigint\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"number\",\"Length\":19,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("byteArray", "byte[]", "[{\"Name\":\"blob\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"longblob\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"binary\",\"Length\":null,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("datetimeoffset", "DateTimeOffset", "[{\"Name\":\"datetimeoffset\",\"Length\":null,\"DecimalDigits\":null}]", 1000));
            codeTypelist.Add(GetCodeType("json_default", "object", "[{\"Name\":\"json\",\"Length\":null,\"DecimalDigits\":null},{\"Name\":\"varchar\",\"Length\":3999,\"DecimalDigits\":null}]", 1000));

            codeTypelist.Add(GetCodeType("string_char10", "string", "[{\"Name\":\"char\",\"Length\":10,\"DecimalDigits\":null}]", 10000));
            codeTypelist.Add(GetCodeType("float", "decimal", "[{\"Name\":\"float\",\"Length\":null,\"DecimalDigits\":null}]", 10000));
            codeTypelist.Add(GetCodeType("double", "decimal", "[{\"Name\":\"double\",\"Length\":null,\"DecimalDigits\":null}]", 10000));
            return codeTypelist;
        }
        private CodeType GetCodeType(string _Name, string _CSharepType, string _DbType, int _Sort)
        {
            CodeType type = null;
            type = new CodeType()
            {
                Name = _Name,
                CSharepType = _CSharepType,
                DbType = JsonConvert.DeserializeObject<DbTypeInfo[]>(_DbType),
                Sort = _Sort
            };
            return type;
        }
        #endregion

        #region 获取表字段类型
        private string GetColumnName(string dbColumnName)
        {
            if (dbColumnName == null)
                return null;
            if (Regex.IsMatch(dbColumnName, @"\[.+\]"))
            {
                return Regex.Replace(dbColumnName, @"\[.+\]", "");
            }
            return UtilMethods.ToUnderLine(dbColumnName);
        }
        private SortTypeInfo GetEntityType(DbColumnInfo columnInfo)
        {
            List<CodeType> types = GetCodeTypeList();
            var typeInfo = types.FirstOrDefault(y => y.DbType.Any(it => it.Name.Equals(columnInfo.DataType, StringComparison.OrdinalIgnoreCase)));
            if (typeInfo == null)
            {
                var type = types.First(it => it.Name == "string100");
                return new SortTypeInfo() { CodeType = type, DbTypeInfo = type.DbType[0] };
            }
            else
            {
                List<SortTypeInfo> SortTypeInfoList = new List<SortTypeInfo>();
                foreach (var type in types)
                {
                    foreach (var cstype in type.DbType)
                    {
                        SortTypeInfo item = new SortTypeInfo();
                        item.DbTypeInfo = cstype;
                        item.CodeType = type;
                        item.Sort = GetSort(cstype, type, columnInfo);
                        SortTypeInfoList.Add(item);
                    }
                }
                var result = SortTypeInfoList.Where(it => it.CodeType.Name != "json_default").OrderByDescending(it => it.Sort).FirstOrDefault();
                if (result == null)
                {
                    throw new Exception($"没有匹配到类型{columnInfo.DataType} 来自 {columnInfo.TableName} 表 {columnInfo.DbColumnName} ，请到类型管理添加");
                }
                if (result.CodeType.Name == "guid" && columnInfo.DataType == "char" && columnInfo.Length != 36)
                {
                    result = SortTypeInfoList.FirstOrDefault(it => it.CodeType.Name == "string100");
                }
                return result;
            }
        }
        /// <summary>
        /// 匹配出最符合的类型
        /// </summary>
        /// <param name="cstype"></param>
        /// <param name="type"></param>
        /// <param name="columnInfo"></param>
        /// <param name="dbtype"></param>
        /// <returns></returns>
        private decimal GetSort(DbTypeInfo cstype, CodeType type, DbColumnInfo columnInfo)
        {
            decimal result = 0;
            if (columnInfo.DataType.Equals(cstype.Name, StringComparison.OrdinalIgnoreCase))
            {
                result = result + 10000;
            }
            else
            {
                result = result - 30000;
            }
            if (columnInfo.Length == Convert.ToInt32(cstype.Length))
            {
                result = result + 5000;
            }
            else if (columnInfo.Length > Convert.ToInt32(cstype.Length))
            {
                result = result + (columnInfo.Length - Convert.ToInt32(cstype.Length)) * -3;
            }
            else
            {
                result = result - 500;
            }
            if (columnInfo.DecimalDigits == Convert.ToInt32(cstype.DecimalDigits))
            {
                result = result + 5000;
            }
            else if (columnInfo.DecimalDigits > Convert.ToInt32(cstype.DecimalDigits))
            {
                result = result + (columnInfo.DecimalDigits - Convert.ToInt32(cstype.DecimalDigits)) * -3;
            }
            else
            {
                result = result - 500;
            }
            if (type.Name.Contains("nString") && columnInfo.DataType == "varchar")
            {
                result = result - 500;
            }
            return result;
        }
        #endregion

        #region  Entity
        private void GetEntitys()
        {
            try
            {
                string entitypathdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model", "Entity");
                if (!Directory.Exists(entitypathdir))
                {
                    Directory.CreateDirectory(entitypathdir);
                }

                List<CodeTable> tableList = new List<CodeTable>();
                var _tableList = this.spg.PrimaryGrid.DataSource as List<CodeTable>;
                if (_tableList != null && _tableList.Count > 0)
                {
                    tableList.AddRange(_tableList.FindAll(t => t.ClassCheck == 1));
                }
                if (tableList.Count > 0)
                {
                    List<CodeColumns> columnlist = new List<CodeColumns>();
                    tableList.ForEach(t =>
                    {
                        var cols = _DbContext.Db.DbMaintenance.GetColumnInfosByTableName(t.TableName, false);
                        foreach (var columnInfo in cols)
                        {
                            var typeInfo = GetEntityType(columnInfo);
                            string dbcol = GetColumnName(columnInfo.DbColumnName);
                            CodeColumns column = new CodeColumns()
                            {
                                TableName = t.TableName,
                                ClassProperName = PubMehtod.GetCsharpName(dbcol),
                                DbColumnName = dbcol,
                                Description = columnInfo.ColumnDescription,
                                DefaultValue = columnInfo.DefaultValue,
                                IsIdentity = columnInfo.IsIdentity,
                                IsPrimaryKey = columnInfo.IsPrimarykey,
                                Required = columnInfo.IsNullable == false,
                                CodeType = typeInfo.CodeType.Name,
                                IsText = columnInfo.DataType == "text" ? true : false,
                                isNullable = columnInfo.IsNullable,
                                Length = columnInfo.Length,
                                DecimalDigits = columnInfo.DecimalDigits,
                                DbType = columnInfo.DataType
                            };
                            if (column.DbType == "bit")
                            {
                                if (column.DefaultValue.Contains("0")) column.DefaultValue = "0";
                                if (column.DefaultValue.Contains("1")) column.DefaultValue = "1";
                            }
                            columnlist.Add(column);
                        }
                    });

                    List<EntitiesGen> genList = GetGenList(tableList, columnlist, _DbContext.Db.CurrentConnectionConfig.DbType);
                    int index = 0;
                    foreach (var item in genList)
                    {
                        string key = this.txtormname.Text.Trim() + ".Entity." + index;
                        item.name_space = this.txtormname.Text.Trim();
                        string content = TemplateHelper.GetTemplateValue(key, Template.Entity, item);
                        if (!content.IsNullOrEmpty())
                        {
                            string path = Path.Combine(entitypathdir, $"{item.ClassName}.cs");
                            var data = Encoding.Default.GetBytes(content);
                            File.WriteAllBytes(path, data);
                        }
                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private List<EntitiesGen> GetGenList(List<CodeTable> tableList, List<CodeColumns> columnlist, SqlSugar.DbType databasedbType)
        {
            List<CodeType> types = GetCodeTypeList();
            List<EntitiesGen> result = new List<EntitiesGen>();
            if (databasedbType == SqlSugar.DbType.MySql)
            {
                var timestamp = types.FirstOrDefault(it => it.Name == "timestamp");
                if (timestamp != null)
                {
                    timestamp.CSharepType = "DateTime";
                }
            }
            foreach (var item in tableList)
            {
                EntitiesGen gen = new EntitiesGen()
                {
                    ClassName = item.ClassName,
                    Description = item.Description == null ? "" : item.Description.Replace("\r", "").Replace("\r", ""),
                    TableName = item.TableName,
                    PropertyGens = new List<PropertyGen>()
                };
                foreach (var column in columnlist.FindAll(it => it.TableName == item.TableName))
                {
                    var codeType = types.First(it => it.Name == column.CodeType);
                    if (codeType.Name == "ignore")
                    {
                        PropertyGen proGen = new PropertyGen()
                        {
                            DbColumnName = column.DbColumnName,
                            Description = column.Description == null ? "" : column.Description.Replace("\r", "").Replace("\r", ""),
                            DefaultValue = column.DefaultValue,
                            IsIdentity = column.IsIdentity,
                            IsPrimaryKey = column.IsPrimaryKey,
                            PropertyName = GetPropertyName(column.ClassProperName),
                            Type = GetType(column),
                            IsNullable = column.isNullable,
                            IsText = column.IsText,
                            Length = column.Length,
                            DecimalDigits = column.DecimalDigits,
                            DbType = column.DbType,
                            IsIgnore = true,
                            CodeType = column.CodeType
                        };
                        gen.PropertyGens.Add(proGen);
                    }
                    else
                    {
                        var dbType = GetTypeInfoByDatabaseType(codeType.DbType, databasedbType);
                        PropertyGen proGen = new PropertyGen()
                        {
                            DbColumnName = column.DbColumnName,
                            Description = column.Description == null ? "" : column.Description.Replace("\r", "").Replace("\r", ""),
                            DefaultValue = column.DefaultValue,
                            IsIdentity = column.IsIdentity,
                            IsPrimaryKey = column.IsPrimaryKey,
                            PropertyName = GetPropertyName(column.ClassProperName),
                            Type = IsSpecialType(column) ? GetType(column) : codeType.CSharepType,
                            IsNullable = column.Required == false,
                            IsText = column.IsText,
                            Length = column.Length,
                            DecimalDigits = column.DecimalDigits,
                            DbType = dbType.Name,
                            IsSpecialType = IsSpecialType(column),
                            CodeType = column.CodeType
                        };
                        gen.PropertyGens.Add(proGen);
                    }
                }
                gen.PropertyGens.ForEach(t =>
                {
                    if (!t.IsNullable && t.DefaultValue.IsNullOrEmpty())
                        t.DefaultValue = "0";
                });
                result.Add(gen);
            }
            return result;
        }

        private static string GetPropertyName(string name)
        {
            return Regex.Replace(name, @"\[(.+)\]", "");
        }

        private static bool IsSpecialType(CodeColumns column)
        {
            return Regex.IsMatch(column.ClassProperName, @"\[.+\]");
        }

        private static string GetType(CodeColumns column)
        {
            string type = "string";
            if (IsSpecialType(column))
            {
                type = Regex.Match(column.ClassProperName, @"\[(.+)\]").Groups[1].Value;
            }

            return type;
        }

        private DbTypeInfo GetTypeInfoByDatabaseType(DbTypeInfo[] dbType, SqlSugar.DbType databasedbType)
        {
            DbTypeInfo result = dbType.First();
            List<string> mstypes = new List<string>();
            switch (databasedbType)
            {
                case SqlSugar.DbType.MySql:
                    mstypes = SqlSugar.MySqlDbBind.MappingTypesConst.Select(it => it.Key.ToLower()).ToList();
                    break;
                case SqlSugar.DbType.SqlServer:
                    mstypes = SqlSugar.SqlServerDbBind.MappingTypesConst.Select(it => it.Key.ToLower()).ToList();
                    break;
                case SqlSugar.DbType.Sqlite:
                    mstypes = SqlSugar.SqliteDbBind.MappingTypesConst.Select(it => it.Key.ToLower()).ToList();
                    break;
                case SqlSugar.DbType.Oracle:
                    mstypes = SqlSugar.OracleDbBind.MappingTypesConst.Select(it => it.Key.ToLower()).ToList();
                    break;
                default:
                    break;
            }
            result = dbType.FirstOrDefault(it => mstypes.Contains(it.Name.ToLower()));
            if (result == null)
            {
                throw new Exception("暂时不支持类型" + string.Join(",", dbType.Select(it => it.Name)));
            }
            return result;
        }

        #endregion

        #region  DAO

        private void GetDAOs()
        {
            try
            {
                string daopathdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model", "DAO");
                if (!Directory.Exists(daopathdir))
                {
                    Directory.CreateDirectory(daopathdir);
                }
                List<CodeTable> tableList = new List<CodeTable>();
                var _tableList = this.spg.PrimaryGrid.DataSource as List<CodeTable>;
                if (_tableList != null && _tableList.Count > 0)
                {
                    tableList.AddRange(_tableList.FindAll(t => t.ClassCheck == 1));
                }
                if (tableList.Count > 0)
                {
                    int index = 0;
                    tableList.ForEach(t =>
                    {
                        List<CodeColumns> columnlist = new List<CodeColumns>();
                        foreach (var columnInfo in _DbContext.Db.DbMaintenance.GetColumnInfosByTableName(t.TableName, false))
                        {
                            var typeInfo = GetEntityType(columnInfo);
                            string dbcol = GetColumnName(columnInfo.DbColumnName);
                            CodeColumns column = new CodeColumns()
                            {
                                TableName = t.TableName,
                                ClassProperName = PubMehtod.GetCsharpName(dbcol),
                                DbColumnName = dbcol,
                                Description = columnInfo.ColumnDescription == null ? "" : columnInfo.ColumnDescription.Replace("\r", "").Replace("\r", ""),
                                IsIdentity = columnInfo.IsIdentity,
                                IsPrimaryKey = columnInfo.IsPrimarykey,
                                Required = columnInfo.IsNullable == false,
                                CodeType = typeInfo.CodeType.CSharepType
                            };
                            columnlist.Add(column);
                        }

                        string key = this.txtormname.Text.Trim() + ".DAO." + index;
                        BOGen item = new BOGen();
                        item.name_space = this.txtormname.Text.Trim();
                        item.ClassName = t.ClassName;

                        string content = "";
                        if (!t.IsView)
                        {
                            var colp = columnlist.Find(k => k.IsPrimaryKey);
                            if (colp != null)
                            {
                                item.PrimaryKey = colp.ClassProperName;
                                item.PrimaryKeyType = colp.CodeType;
                            }
                            content = TemplateHelper.GetTemplateValue(key, Template.DAO, item);
                        }
                        else
                        {
                            content = TemplateHelper.GetTemplateValue(key, Template.ViewDAO, item);
                        }

                        if (!content.IsNullOrEmpty())
                        {
                            string path = Path.Combine(daopathdir, $"{item.ClassName}DAO.cs");
                            var data = Encoding.Default.GetBytes(content);
                            File.WriteAllBytes(path, data);
                        }
                        index++;
                    });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region  Controller
        private void GetControllers()
        {
            try
            {
                string controllerpathdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model", "Controller");
                if (!Directory.Exists(controllerpathdir))
                {
                    Directory.CreateDirectory(controllerpathdir);
                }
                List<CodeTable> tableList = new List<CodeTable>();
                var _tableList = this.spg.PrimaryGrid.DataSource as List<CodeTable>;
                if (_tableList != null && _tableList.Count > 0)
                {
                    tableList.AddRange(_tableList.FindAll(t => t.ClassCheck == 1));
                }
                if (tableList.Count > 0)
                {
                    int index = 0;
                    tableList.ForEach(t =>
                    {
                        List<CodeColumns> columnlist = new List<CodeColumns>();
                        foreach (var columnInfo in _DbContext.Db.DbMaintenance.GetColumnInfosByTableName(t.TableName, false))
                        {
                            var typeInfo = GetEntityType(columnInfo);
                            string dbcol = GetColumnName(columnInfo.DbColumnName);
                            CodeColumns column = new CodeColumns()
                            {
                                TableName = t.TableName,
                                ClassProperName = PubMehtod.GetCsharpName(dbcol),
                                DbColumnName = dbcol,
                                Description = columnInfo.ColumnDescription == null ? "" : columnInfo.ColumnDescription.Replace("\r", "").Replace("\r", ""),
                                IsIdentity = columnInfo.IsIdentity,
                                IsPrimaryKey = columnInfo.IsPrimarykey,
                                Required = columnInfo.IsNullable == false,
                                CodeType = typeInfo.CodeType.CSharepType
                            };
                            columnlist.Add(column);
                        }

                        string key = this.txtControllername.Text.Trim() + ".Controller." + index;
                        ControllerGen item = new ControllerGen();
                        item.name_space = this.txtControllername.Text.Trim();
                        item.ClassName = t.ClassName;
                        item.Description = t.Description == null ? "" : t.Description.Replace("\r", "").Replace("\r", "");
                        item.OrmName = this.txtormname.Text.Trim();
                        item.HasCreateId = columnlist.Find(k => k.DbColumnName.Replace("_", "").Contains("createid")) != null;

                        string content = "";
                        if (!t.IsView)
                        {
                            var colp = columnlist.Find(k => k.IsPrimaryKey);

                            if (colp != null)
                            {
                                item.PrimaryKey = colp.ClassProperName;
                                item.PrimaryKeyType = colp.CodeType;
                            }
                            content = TemplateHelper.GetTemplateValue(key, Template.Controller, item);
                        }
                        else
                        {
                            content = TemplateHelper.GetTemplateValue(key, Template.ViewController, item);
                        }

                        if (!content.IsNullOrEmpty())
                        {
                            string path = Path.Combine(controllerpathdir, $"{item.ClassName}Controller.cs");
                            var data = Encoding.Default.GetBytes(content);
                            File.WriteAllBytes(path, data);
                        }
                        index++;

                    });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region  SolrHttpEntity

        private void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            // 创建目标文件夹，如果它不存在
            Directory.CreateDirectory(targetPath);

            // 获取源文件夹信息
            DirectoryInfo dir = new DirectoryInfo(sourcePath);

            // 获取源文件夹的文件列表
            foreach (FileInfo file in dir.GetFiles())
            {
                // 设置目标文件路径
                string targetFilePath = Path.Combine(targetPath, file.Name);
                // 复制文件到目标路径
                file.CopyTo(targetFilePath, true); // true 表示覆盖同名文件
            }

            // 获取源文件夹的子文件夹列表，以便递归处理
            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                // 设置目标子文件夹路径
                string newDestinationDir = Path.Combine(targetPath, subdir.Name);
                // 递归调用方法自身
                CopyFilesRecursively(subdir.FullName, newDestinationDir);
            }
        }

        private string GetZyfTxtValue(string value)
        {
            string result = @"\" + "\"" + value + @"\" + "\"";
            return result;
        }

        private void GetSolrHttpEntitys()
        {
            try
            {
                string entitypathdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model", "Entity");
                if (!Directory.Exists(entitypathdir))
                {
                    Directory.CreateDirectory(entitypathdir);
                }
                string solrdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model", "Solr");
                if (!Directory.Exists(solrdir))
                {
                    Directory.CreateDirectory(solrdir);
                }
                string configdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model", "Config");
                if (!Directory.Exists(configdir))
                {
                    Directory.CreateDirectory(configdir);
                }

                List<CodeTable> tableList = new List<CodeTable>();
                var _tableList = this.spg.PrimaryGrid.DataSource as List<CodeTable>;
                if (_tableList != null && _tableList.Count > 0)
                {
                    tableList.AddRange(_tableList.FindAll(t => t.ClassCheck == 1));
                }
                if (tableList.Count > 0)
                {
                    List<CodeColumns> columnlist = new List<CodeColumns>();
                    tableList.ForEach(t =>
                    {
                        foreach (var columnInfo in _DbContext.Db.DbMaintenance.GetColumnInfosByTableName(t.TableName, false))
                        {
                            var typeInfo = GetEntityType(columnInfo);
                            string dbcol = GetColumnName(columnInfo.DbColumnName);
                            CodeColumns column = new CodeColumns()
                            {
                                TableName = t.TableName,
                                ClassProperName = PubMehtod.GetCsharpName(dbcol),
                                DbColumnName = dbcol,
                                Description = columnInfo.ColumnDescription,
                                IsIdentity = columnInfo.IsIdentity,
                                IsPrimaryKey = columnInfo.IsPrimarykey,
                                Required = columnInfo.IsNullable == false,
                                CodeType = typeInfo.CodeType.Name,
                                IsText = columnInfo.DataType == "text" ? true : false,
                                DbType = columnInfo.DataType,
                                isNullable = columnInfo.IsNullable,
                                Length = columnInfo.Length,
                                DecimalDigits = columnInfo.DecimalDigits,
                            };
                            if (column.IsPrimaryKey)
                            {
                                column.CodeType = "string36";
                                column.DbType = "varchar";
                            }
                            columnlist.Add(column);
                        }
                    });

                    Dictionary<string, string> dicSolrColumn = new Dictionary<string, string>();
                    dicSolrColumn.Add("bigint", "plong");
                    dicSolrColumn.Add("int", "pint");
                    dicSolrColumn.Add("varchar", "string");
                    dicSolrColumn.Add("bit", "boolean");
                    dicSolrColumn.Add("text", "text_general");
                    dicSolrColumn.Add("date", "pdate");
                    dicSolrColumn.Add("datetime", "pdate");
                    dicSolrColumn.Add("float", "pfloat");
                    dicSolrColumn.Add("decimal", "pfloat");
                    dicSolrColumn.Add("double", "pdouble");

                    bool issolrname = false;
                    List<string> solrnames = new List<string>();

                    List<EntitiesGen> genList = GetGenList(tableList, columnlist, _DbContext.Db.CurrentConnectionConfig.DbType);
                    int index = 0;
                    foreach (var item in genList)
                    {
                        string key = this.txtormname.Text.Trim() + ".Entity." + index;
                        item.name_space = this.txtormname.Text.Trim();
                        item.ClassName = $"{item.ClassName}Solr";
                        string content = TemplateHelper.GetTemplateValue(key, TemplateSolrHttp.Entity, item);
                        if (!content.IsNullOrEmpty())
                        {
                            string path = Path.Combine(entitypathdir, $"{item.ClassName}.cs");
                            var data = Encoding.Default.GetBytes(content);
                            File.WriteAllBytes(path, data);
                        }
                        index++;
                        string solrname = GetZyfTxtValue(item.ClassName);
                        string solrnamevalue = GetZyfTxtValue($"{txtControllername.Text.Trim()}{item.ClassName}");
                        solrnames.Add($"{solrname}:{solrnamevalue}");
                        issolrname = true;
                    }

                    string solrconfdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "solr-9.5.0", "conf");
                    if (ckbv97.Checked) solrconfdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "solr-9.7.0");
                    if (Directory.Exists(solrconfdir))
                    {
                        foreach (var item in genList)
                        {
                            List<string> fieldlist = new List<string>();
                            string uniqueKey = "";
                            foreach (var proclass in item.PropertyGens)
                            {
                                string solrtype = proclass.DbType.ToLower();
                                if (dicSolrColumn.ContainsKey(solrtype))
                                {
                                    solrtype = dicSolrColumn[solrtype];
                                }
                                string required = "false";
                                if (proclass.IsPrimaryKey)
                                {
                                    required = "true";
                                    uniqueKey = $"    <uniqueKey>{proclass.DbColumnName}</uniqueKey>";
                                }
                                fieldlist.Add($"    <field name=\"{proclass.DbColumnName}\" type=\"{solrtype}\" indexed=\"true\" stored=\"true\" required=\"{required}\" multiValued=\"false\" /> ");
                            }
                            if (fieldlist.Any())
                            {
                                fieldlist.Add(uniqueKey);
                                string classdirpath = Path.Combine(solrdir,
                                    $"{txtControllername.Text.Trim()}{item.ClassName}");
                                Directory.CreateDirectory(classdirpath);

                                string classdirpathconf = Path.Combine(classdirpath, "conf");
                                if (ckbv97.Checked) classdirpathconf = classdirpath;
                                Directory.CreateDirectory(classdirpathconf);

                                CopyFilesRecursively(solrconfdir, classdirpathconf);
                                string schemapath = Path.Combine(classdirpathconf, "managed-schema.xml");
                                if (File.Exists(schemapath))
                                {
                                    // 读取所有行到列表中
                                    List<string> lines = new List<string>(File.ReadAllLines(schemapath));

                                    // 检查文件是否至少有900行
                                    if (lines.Count >= 900)
                                    {
                                        int minline = 119;
                                        if (ckbv97.Checked) minline = 124;
                                        foreach (string line in fieldlist)
                                        {
                                            lines.Insert(minline, line);
                                            minline++;
                                        }

                                        // 将修改后的内容写回文件
                                        File.WriteAllLines(schemapath, lines);

                                        if (ckbv97.Checked)
                                        {
                                            ZipSharpHelper zip = new ZipSharpHelper();
                                            zip.ZipFileFromDirectory(classdirpathconf, classdirpath + ".zip");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (issolrname)
                    {
                        string configdirpath = Path.Combine(configdir, "SolrSetting.txt");
                        string strdata = $"{{{string.Join(",", solrnames)}}}";
                        var buffer = Encoding.Default.GetBytes(strdata);
                        File.WriteAllBytes(configdirpath, buffer);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region  SolrDAO

        private void GetSolrDAOs()
        {
            try
            {
                string daopathdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model", "DAO");
                if (!Directory.Exists(daopathdir))
                {
                    Directory.CreateDirectory(daopathdir);
                }
                List<CodeTable> tableList = new List<CodeTable>();
                var _tableList = this.spg.PrimaryGrid.DataSource as List<CodeTable>;
                if (_tableList != null && _tableList.Count > 0)
                {
                    tableList.AddRange(_tableList.FindAll(t => t.ClassCheck == 1));
                }
                if (tableList.Count > 0)
                {
                    int index = 0;
                    tableList.ForEach(t =>
                    {
                        List<CodeColumns> columnlist = new List<CodeColumns>();
                        foreach (var columnInfo in _DbContext.Db.DbMaintenance.GetColumnInfosByTableName(t.TableName, false))
                        {
                            var typeInfo = GetEntityType(columnInfo);
                            string dbcol = GetColumnName(columnInfo.DbColumnName);
                            CodeColumns column = new CodeColumns()
                            {
                                TableName = t.TableName,
                                ClassProperName = PubMehtod.GetCsharpName(dbcol),
                                DbColumnName = dbcol,
                                Description = columnInfo.ColumnDescription == null ? "" : columnInfo.ColumnDescription.Replace("\r", "").Replace("\r", ""),
                                IsIdentity = columnInfo.IsIdentity,
                                IsPrimaryKey = columnInfo.IsPrimarykey,
                                Required = columnInfo.IsNullable == false,
                                CodeType = typeInfo.CodeType.CSharepType
                            };
                            columnlist.Add(column);
                        }

                        string key = this.txtormname.Text.Trim() + ".DAO." + index;
                        BOGen item = new BOGen();
                        item.name_space = this.txtormname.Text.Trim();
                        item.ClassName = $"{t.ClassName}Solr";

                        string content = "";
                        var colp = columnlist.Find(k => k.IsPrimaryKey);
                        if (colp != null)
                        {
                            item.PrimaryKey = colp.ClassProperName;
                            item.PrimaryKeyType = colp.CodeType;
                        }
                        content = TemplateHelper.GetTemplateValue(key, TemplateSolrHttp.DAO, item);

                        if (!content.IsNullOrEmpty())
                        {
                            string path = Path.Combine(daopathdir, $"{item.ClassName}DAO.cs");
                            var data = Encoding.Default.GetBytes(content);
                            File.WriteAllBytes(path, data);
                        }
                        index++;
                    });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region  SolrController
        private void GetSolrControllers()
        {
            try
            {
                string controllerpathdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model", "Controller");
                if (!Directory.Exists(controllerpathdir))
                {
                    Directory.CreateDirectory(controllerpathdir);
                }
                List<CodeTable> tableList = new List<CodeTable>();
                var _tableList = this.spg.PrimaryGrid.DataSource as List<CodeTable>;
                if (_tableList != null && _tableList.Count > 0)
                {
                    tableList.AddRange(_tableList.FindAll(t => t.ClassCheck == 1));
                }
                if (tableList.Count > 0)
                {
                    int index = 0;
                    tableList.ForEach(t =>
                    {
                        List<CodeColumns> columnlist = new List<CodeColumns>();
                        foreach (var columnInfo in _DbContext.Db.DbMaintenance.GetColumnInfosByTableName(t.TableName, false))
                        {
                            var typeInfo = GetEntityType(columnInfo);
                            string dbcol = GetColumnName(columnInfo.DbColumnName);
                            CodeColumns column = new CodeColumns()
                            {
                                TableName = t.TableName,
                                ClassProperName = PubMehtod.GetCsharpName(dbcol),
                                DbColumnName = dbcol,
                                Description = columnInfo.ColumnDescription == null ? "" : columnInfo.ColumnDescription.Replace("\r", "").Replace("\r", ""),
                                IsIdentity = columnInfo.IsIdentity,
                                IsPrimaryKey = columnInfo.IsPrimarykey,
                                Required = columnInfo.IsNullable == false,
                                CodeType = typeInfo.CodeType.CSharepType
                            };
                            columnlist.Add(column);
                        }

                        string key = this.txtControllername.Text.Trim() + ".Controller." + index;
                        ControllerGen item = new ControllerGen();
                        item.name_space = this.txtControllername.Text.Trim();
                        item.ClassName = $"{t.ClassName}Solr";
                        item.Description = t.Description == null ? "" : t.Description.Replace("\r", "").Replace("\r", "");
                        item.OrmName = this.txtormname.Text.Trim();
                        item.PrimaryKey = t.ClassName;

                        string content = TemplateHelper.GetTemplateValue(key, TemplateSolrHttp.ViewController, item);
                        if (!content.IsNullOrEmpty())
                        {
                            string path = Path.Combine(controllerpathdir, $"{item.PrimaryKey}Controller.cs");
                            var data = Encoding.Default.GetBytes(content);
                            File.WriteAllBytes(path, data);
                        }
                        index++;

                    });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        private void chksqlserver_CheckedChanged(object sender, EventArgs e)
        {
            if (chksqlserver.Checked)
            {
                chkMySql.Checked = false;
                chkSqlite.Checked = false;
            }
        }

        private void chkMySql_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMySql.Checked)
            {
                chksqlserver.Checked = false;
                chkSqlite.Checked = false;
            }
        }

        private void chkSqlite_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSqlite.Checked)
            {
                chksqlserver.Checked = false;
                chkMySql.Checked = false;
            }
        }

        private void ckbsolrhttp_CheckedChanged(object sender, EventArgs e)
        {
            if (ckbsolrhttp.Checked)
            {
                this.label2.Text = "Solr命名空间：";
                this.label3.Text = "Core Admin文件夹命名：";
            }
            else
            {
                this.label2.Text = "ORM命名空间：";
                this.label3.Text = "Controllers命名空间：";
            }
        }


        private void btnTemplateClear_Click(object sender, EventArgs e)
        {
            string dir = ConfigurationManager.AppSettings["TemplateDir"].ToString();
            if (Directory.Exists(dir))
            {
                DirectoryInfo dirinfo = new DirectoryInfo(dir);
                foreach (var item in dirinfo.EnumerateDirectories())
                {
                    if (item.Name.Contains("RazorEngine")) item.Delete(true);
                }

            }
        }
    }
}