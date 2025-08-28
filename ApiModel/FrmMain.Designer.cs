namespace ApiModel
{
    partial class FrmMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.txtdb = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtormname = new System.Windows.Forms.TextBox();
            this.txtControllername = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.ckbv97 = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.btnTemplateClear = new System.Windows.Forms.Button();
            this.ckbsolrhttp = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.chkSqlite = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.chkMySql = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.chksqlserver = new DevComponents.DotNetBar.Controls.CheckBoxX();
            this.lblmes = new DevComponents.DotNetBar.LabelX();
            this.btndb = new System.Windows.Forms.Button();
            this.btnmuban = new System.Windows.Forms.Button();
            this.btnnotall = new System.Windows.Forms.Button();
            this.btnall = new System.Windows.Forms.Button();
            this.superTabControl1 = new DevComponents.DotNetBar.SuperTabControl();
            this.superTabControlPanel1 = new DevComponents.DotNetBar.SuperTabControlPanel();
            this.spg = new DevComponents.DotNetBar.SuperGrid.SuperGridControl();
            this.superTabItem1 = new DevComponents.DotNetBar.SuperTabItem();
            this.superTabControlPanel2 = new DevComponents.DotNetBar.SuperTabControlPanel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.superTabControl1)).BeginInit();
            this.superTabControl1.SuspendLayout();
            this.superTabControlPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "数据库字符串：";
            // 
            // txtdb
            // 
            this.txtdb.Location = new System.Drawing.Point(112, 34);
            this.txtdb.Name = "txtdb";
            this.txtdb.Size = new System.Drawing.Size(737, 21);
            this.txtdb.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "ORM命名空间：";
            // 
            // txtormname
            // 
            this.txtormname.Location = new System.Drawing.Point(112, 66);
            this.txtormname.Name = "txtormname";
            this.txtormname.Size = new System.Drawing.Size(229, 21);
            this.txtormname.TabIndex = 3;
            // 
            // txtControllername
            // 
            this.txtControllername.Location = new System.Drawing.Point(526, 66);
            this.txtControllername.Name = "txtControllername";
            this.txtControllername.Size = new System.Drawing.Size(323, 21);
            this.txtControllername.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(390, 71);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(131, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "Controllers命名空间：";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.ckbv97);
            this.splitContainer1.Panel1.Controls.Add(this.btnTemplateClear);
            this.splitContainer1.Panel1.Controls.Add(this.ckbsolrhttp);
            this.splitContainer1.Panel1.Controls.Add(this.chkSqlite);
            this.splitContainer1.Panel1.Controls.Add(this.chkMySql);
            this.splitContainer1.Panel1.Controls.Add(this.chksqlserver);
            this.splitContainer1.Panel1.Controls.Add(this.lblmes);
            this.splitContainer1.Panel1.Controls.Add(this.btndb);
            this.splitContainer1.Panel1.Controls.Add(this.btnmuban);
            this.splitContainer1.Panel1.Controls.Add(this.btnnotall);
            this.splitContainer1.Panel1.Controls.Add(this.btnall);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.txtControllername);
            this.splitContainer1.Panel1.Controls.Add(this.txtdb);
            this.splitContainer1.Panel1.Controls.Add(this.label3);
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.txtormname);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.superTabControl1);
            this.splitContainer1.Size = new System.Drawing.Size(862, 670);
            this.splitContainer1.SplitterDistance = 124;
            this.splitContainer1.TabIndex = 6;
            // 
            // ckbv97
            // 
            // 
            // 
            // 
            this.ckbv97.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.ckbv97.Location = new System.Drawing.Point(636, 5);
            this.ckbv97.Name = "ckbv97";
            this.ckbv97.Size = new System.Drawing.Size(73, 23);
            this.ckbv97.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.ckbv97.TabIndex = 18;
            this.ckbv97.Text = "9.7版本";
            // 
            // btnTemplateClear
            // 
            this.btnTemplateClear.Location = new System.Drawing.Point(776, 7);
            this.btnTemplateClear.Name = "btnTemplateClear";
            this.btnTemplateClear.Size = new System.Drawing.Size(73, 23);
            this.btnTemplateClear.TabIndex = 17;
            this.btnTemplateClear.Text = "清空调试模板";
            this.btnTemplateClear.UseVisualStyleBackColor = true;
            this.btnTemplateClear.Click += new System.EventHandler(this.btnTemplateClear_Click);
            // 
            // ckbsolrhttp
            // 
            // 
            // 
            // 
            this.ckbsolrhttp.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.ckbsolrhttp.Location = new System.Drawing.Point(508, 5);
            this.ckbsolrhttp.Name = "ckbsolrhttp";
            this.ckbsolrhttp.Size = new System.Drawing.Size(109, 23);
            this.ckbsolrhttp.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.ckbsolrhttp.TabIndex = 16;
            this.ckbsolrhttp.Text = "SolrHttp生成";
            this.ckbsolrhttp.CheckedChanged += new System.EventHandler(this.ckbsolrhttp_CheckedChanged);
            // 
            // chkSqlite
            // 
            // 
            // 
            // 
            this.chkSqlite.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkSqlite.Location = new System.Drawing.Point(336, 6);
            this.chkSqlite.Name = "chkSqlite";
            this.chkSqlite.Size = new System.Drawing.Size(122, 23);
            this.chkSqlite.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkSqlite.TabIndex = 14;
            this.chkSqlite.Text = "Sqlite数据库";
            this.chkSqlite.CheckedChanged += new System.EventHandler(this.chkSqlite_CheckedChanged);
            // 
            // chkMySql
            // 
            // 
            // 
            // 
            this.chkMySql.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chkMySql.Checked = true;
            this.chkMySql.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMySql.CheckValue = "Y";
            this.chkMySql.Location = new System.Drawing.Point(184, 6);
            this.chkMySql.Name = "chkMySql";
            this.chkMySql.Size = new System.Drawing.Size(122, 23);
            this.chkMySql.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chkMySql.TabIndex = 13;
            this.chkMySql.Text = "MySql数据库";
            this.chkMySql.CheckedChanged += new System.EventHandler(this.chkMySql_CheckedChanged);
            // 
            // chksqlserver
            // 
            // 
            // 
            // 
            this.chksqlserver.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.chksqlserver.Location = new System.Drawing.Point(19, 6);
            this.chksqlserver.Name = "chksqlserver";
            this.chksqlserver.Size = new System.Drawing.Size(122, 23);
            this.chksqlserver.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.chksqlserver.TabIndex = 12;
            this.chksqlserver.Text = "SqlServer数据库";
            this.chksqlserver.CheckedChanged += new System.EventHandler(this.chksqlserver_CheckedChanged);
            // 
            // lblmes
            // 
            // 
            // 
            // 
            this.lblmes.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblmes.ForeColor = System.Drawing.Color.Red;
            this.lblmes.Location = new System.Drawing.Point(231, 96);
            this.lblmes.Name = "lblmes";
            this.lblmes.Size = new System.Drawing.Size(257, 23);
            this.lblmes.TabIndex = 10;
            this.lblmes.Text = "提示：";
            // 
            // btndb
            // 
            this.btndb.Location = new System.Drawing.Point(572, 95);
            this.btndb.Name = "btndb";
            this.btndb.Size = new System.Drawing.Size(111, 23);
            this.btndb.TabIndex = 9;
            this.btndb.Text = "1-DB连接";
            this.btndb.UseVisualStyleBackColor = true;
            this.btndb.Click += new System.EventHandler(this.btndb_Click);
            // 
            // btnmuban
            // 
            this.btnmuban.Location = new System.Drawing.Point(717, 95);
            this.btnmuban.Name = "btnmuban";
            this.btnmuban.Size = new System.Drawing.Size(111, 23);
            this.btnmuban.TabIndex = 10;
            this.btnmuban.Text = "2-模板生成";
            this.btnmuban.UseVisualStyleBackColor = true;
            this.btnmuban.Click += new System.EventHandler(this.btnmuban_Click);
            // 
            // btnnotall
            // 
            this.btnnotall.Location = new System.Drawing.Point(91, 97);
            this.btnnotall.Name = "btnnotall";
            this.btnnotall.Size = new System.Drawing.Size(76, 23);
            this.btnnotall.TabIndex = 7;
            this.btnnotall.Text = "取消全选";
            this.btnnotall.UseVisualStyleBackColor = true;
            this.btnnotall.Click += new System.EventHandler(this.btnnotall_Click);
            // 
            // btnall
            // 
            this.btnall.Location = new System.Drawing.Point(19, 97);
            this.btnall.Name = "btnall";
            this.btnall.Size = new System.Drawing.Size(55, 23);
            this.btnall.TabIndex = 6;
            this.btnall.Text = "全选";
            this.btnall.UseVisualStyleBackColor = true;
            this.btnall.Click += new System.EventHandler(this.btnall_Click);
            // 
            // superTabControl1
            // 
            // 
            // 
            // 
            // 
            // 
            // 
            this.superTabControl1.ControlBox.CloseBox.Name = "";
            // 
            // 
            // 
            this.superTabControl1.ControlBox.MenuBox.Name = "";
            this.superTabControl1.ControlBox.Name = "";
            this.superTabControl1.ControlBox.SubItems.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.superTabControl1.ControlBox.MenuBox,
            this.superTabControl1.ControlBox.CloseBox});
            this.superTabControl1.Controls.Add(this.superTabControlPanel1);
            this.superTabControl1.Controls.Add(this.superTabControlPanel2);
            this.superTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControl1.Location = new System.Drawing.Point(0, 0);
            this.superTabControl1.Name = "superTabControl1";
            this.superTabControl1.ReorderTabsEnabled = true;
            this.superTabControl1.SelectedTabFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold);
            this.superTabControl1.SelectedTabIndex = 0;
            this.superTabControl1.Size = new System.Drawing.Size(862, 542);
            this.superTabControl1.TabFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.superTabControl1.TabIndex = 2;
            this.superTabControl1.Tabs.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.superTabItem1});
            this.superTabControl1.Text = "附件";
            // 
            // superTabControlPanel1
            // 
            this.superTabControlPanel1.Controls.Add(this.spg);
            this.superTabControlPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControlPanel1.Location = new System.Drawing.Point(0, 28);
            this.superTabControlPanel1.Name = "superTabControlPanel1";
            this.superTabControlPanel1.Size = new System.Drawing.Size(862, 514);
            this.superTabControlPanel1.TabIndex = 1;
            this.superTabControlPanel1.TabItem = this.superTabItem1;
            // 
            // spg
            // 
            this.spg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.spg.FilterExprColors.SysFunction = System.Drawing.Color.DarkRed;
            this.spg.Location = new System.Drawing.Point(0, 0);
            this.spg.Name = "spg";
            // 
            // 
            // 
            this.spg.PrimaryGrid.AutoSelectDeleteBoundRows = false;
            this.spg.PrimaryGrid.AutoSelectNewBoundRows = false;
            this.spg.PrimaryGrid.RowFocusMode = DevComponents.DotNetBar.SuperGrid.RowFocusMode.CellsOnly;
            this.spg.Size = new System.Drawing.Size(862, 514);
            this.spg.TabIndex = 1;
            this.spg.Text = "superGridControl1";
            // 
            // superTabItem1
            // 
            this.superTabItem1.AttachedControl = this.superTabControlPanel1;
            this.superTabItem1.GlobalItem = false;
            this.superTabItem1.Name = "superTabItem1";
            this.superTabItem1.Text = "单据明细";
            // 
            // superTabControlPanel2
            // 
            this.superTabControlPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControlPanel2.Location = new System.Drawing.Point(0, 0);
            this.superTabControlPanel2.Name = "superTabControlPanel2";
            this.superTabControlPanel2.Size = new System.Drawing.Size(862, 542);
            this.superTabControlPanel2.TabIndex = 0;
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(862, 670);
            this.Controls.Add(this.splitContainer1);
            this.MaximizeBox = false;
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SqlSugar模板生成";
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.superTabControl1)).EndInit();
            this.superTabControl1.ResumeLayout(false);
            this.superTabControlPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtdb;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtormname;
        private System.Windows.Forms.TextBox txtControllername;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnall;
        private System.Windows.Forms.Button btnnotall;
        private DevComponents.DotNetBar.SuperTabControl superTabControl1;
        private DevComponents.DotNetBar.SuperTabControlPanel superTabControlPanel1;
        private DevComponents.DotNetBar.SuperGrid.SuperGridControl spg;
        private DevComponents.DotNetBar.SuperTabItem superTabItem1;
        private DevComponents.DotNetBar.SuperTabControlPanel superTabControlPanel2;
        private System.Windows.Forms.Button btnmuban;
        private System.Windows.Forms.Button btndb;
        private DevComponents.DotNetBar.LabelX lblmes;
        private DevComponents.DotNetBar.Controls.CheckBoxX chksqlserver;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkMySql;
        private DevComponents.DotNetBar.Controls.CheckBoxX chkSqlite;
        private DevComponents.DotNetBar.Controls.CheckBoxX ckbsolrhttp;
        private System.Windows.Forms.Button btnTemplateClear;
        private DevComponents.DotNetBar.Controls.CheckBoxX ckbv97;
    }
}

