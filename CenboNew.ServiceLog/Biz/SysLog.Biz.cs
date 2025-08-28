using NewLife.Log;
using System;
using System.ComponentModel;
using XCode;
using XCode.Membership;
using XCode.Shards;

namespace CenboNew.ServiceLog
{
    public partial class SysLog : Entity<SysLog>
    {
        #region 对象操作
        static SysLog()
        {
            // 按天分表
            Meta.ShardPolicy = new TimeShardPolicy(nameof(snowId), Meta.Factory)
            {
                ConnPolicy = "Log_{1:yyMM}",
                TablePolicy = "{0}{1:dd}",
                Step = TimeSpan.FromDays(1),
            };

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<TimeModule>();
        }

        /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            // 建议先调用基类方法，基类方法会做一些统一处理
            base.Valid(isNew);

            // 在新插入数据或者修改了指定字段时进行修正
            //if (isNew && !Dirtys[nameof(createTime)]) createTime = DateTime.Now;

            // 检查唯一索引
            // CheckExist(isNew, nameof(snowId));
        }

        ///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //protected override void InitData()
        //{
        //    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用
        //    if (Meta.Session.Count > 0) return;

        //    if (XTrace.Debug) XTrace.WriteLine("开始初始化Syslog[Syslog]数据……");

        //    var entity = new SysLog();
        //    entity.snowId = 0;
        //    entity.createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        //    entity.className = "测试";
        //    entity.methodName = "测试";
        //    entity.dataType = "测试";
        //    entity.logMessage = "测试";
        //    entity.Insert();

        //    if (XTrace.Debug) XTrace.WriteLine("完成初始化Syslog[Syslog]数据！");
        //}

        ///// <summary>已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert</summary>
        ///// <returns></returns>
        //public override Int32 Insert()
        //{
        //    return base.Insert();
        //}

        ///// <summary>已重载。在事务保护范围内处理业务，位于Valid之后</summary>
        ///// <returns></returns>
        //protected override Int32 OnDelete()
        //{
        //    return base.OnDelete();
        //}
        #endregion

        #region 扩展属性
        #endregion

        #region 扩展查询
        /// <summary>根据雪花主键查找</summary>
        /// <param name="snowId">雪花主键</param>
        /// <returns>实体对象</returns>
        public static SysLog FindBysnowId(Int64 snowId)
        {
            if (snowId <= 0) return null;

            // 实体缓存
            if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.snowId == snowId);

            // 单对象缓存
            return Meta.SingleCache[snowId];

            //return Find(_.snowId == snowId);
        }
        #endregion

        #region 高级查询

        // Select Count(Id) as Id,Category From syslog Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By Id Desc limit 20
        //static readonly FieldCache<Syslog> _CategoryCache = new FieldCache<Syslog>(nameof(Category))
        //{
        //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
        //};

        ///// <summary>获取类别列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
        ///// <returns></returns>
        //public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();
        #endregion

        #region 业务操作
        #endregion
    }
}
