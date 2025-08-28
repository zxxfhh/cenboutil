using System;

namespace CenBoCommon.Zxx
{
    public class HttpResult
    {
        /// <summary>
        /// 结果：4:正在使用 3:成功 2:失败 1:超时
        /// </summary>
        public int result { get; set; }

        private string _message = "";
        /// <summary>
        /// 提示消息
        /// </summary>
        public string message
        {
            get
            {
                if (_message.IsZxxNullOrEmpty())
                {
                    if (result == 1)
                    {
                        _message = "控制超时";
                    }
                    else if (result == 2)
                    {
                        _message = "控制失败";
                    }
                    else if (result == 3)
                    {
                        _message = "控制成功";
                    }
                    else if (result == 4)
                    {
                        _message = "正在使用";
                    }
                }
                return _message;
            }
            set
            {
                _message = value;
                return;
            }
        }

        /// <summary>
        /// 数据信息
        /// </summary>
        public Object dataobj { get; set; }
    }
}
