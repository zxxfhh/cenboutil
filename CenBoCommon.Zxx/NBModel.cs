using System;
using System.Collections.Generic;

namespace CenBoCommon.Zxx
{
    public class NBModel
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int userId { get; set; } = 0;

        /// <summary>
        /// 用户名称
        /// </summary>
        public string userName { get; set; } = "";

        /// <summary>
        /// 控制来源
        /// </summary>
        public string sourceType { get; set; } = "Web";

        /// <summary>
        /// 请求唯一id(发送mq是生成GUID)
        /// </summary>
        public string commandID { get; set; } = "";

        /// <summary>
        /// 发送关键字
        /// </summary>
        public string routingkey { get; set; } = "";

        /// <summary>
        /// 5:部分失败 4:正在使用 3:成功 2:失败 1:超时
        /// </summary>
        public int result { get; set; } = 0;

        /// <summary>
        /// 过期时间(自动判断)
        /// </summary>
        public DateTime expireTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 详细内容
        /// </summary>
        public string commandstr { get; set; } = "";

        /// <summary>
        /// 未成功数据
        /// </summary>
        public List<Object> unSuccessDatas { get; set; } = new List<Object>();

    }
}
