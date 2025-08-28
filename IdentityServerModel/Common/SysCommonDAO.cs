using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServerModel
{
    /// <summary>
    /// 通用查询DAO
    /// </summary>
    /// <typeparam name="T">实体类</typeparam>
    public sealed partial class SysCommonDAO<T> : DbContext<T> where T : class, new()
    {
        private static SysCommonDAO<T> instance;
        public static SysCommonDAO<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SysCommonDAO<T>();
                }
                return instance;
            }
        }

    }
}
