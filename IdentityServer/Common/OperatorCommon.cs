using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;

namespace IdentityServer
{
    public static class OperatorCommon
    {
        /// <summary>
        /// 获取POST请求参数
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static string GetPostData(this HttpRequest httpRequest)
        {
            string result = "";
            try
            {
                httpRequest.EnableBuffering();
                var stream = httpRequest.Body;
                stream.Position = 0;//核心代码
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEndAsync().Result;
                }
            }
            catch (Exception)
            {
                result = "";
            }
            return result;
        }

        /// <summary>
        /// 获取POST请求参数2
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static string GetPostData2(this HttpRequest httpRequest)
        {
            string result = "";
            try
            {
                httpRequest.EnableBuffering();
                //leaveOpen:true标识StreamReader释放时不会自动关闭流        　　
                using var reader = new StreamReader(httpRequest.Body, leaveOpen: true, encoding: Encoding.UTF8);
                result = reader.ReadToEndAsync().Result;
                //Action中可再次读取流
                httpRequest.Body.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception)
            {
                result = "";
            }
            return result;
        }

        public static bool CheckSourceType(this HttpRequest httpRequest)
        {
            bool result = false;

            try
            {
                Microsoft.Extensions.Primitives.StringValues sourcetype = "";
                httpRequest.Headers.TryGetValue("sourcetype", out sourcetype);
                if (sourcetype == "xddp")
                {
                    result = true;
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public static List<int> ToIntList(this string ids)
        {
            List<int> result = new List<int>();

            try
            {
                var strarr = ids.Split(',');
                for (int i = 0; i < strarr.Length; i++)
                {
                    if (!result.Contains(strarr[i].ToInt2()))
                    {
                        result.Add(strarr[i].ToInt2());
                    }
                }
            }
            catch (Exception)
            {
                result.Clear();
            }
            return result;
        }

        public static int ToInt2(this object value, int result = 0)
        {
            try
            {
                result = Convert.ToInt32(value);
            }
            catch (Exception)
            { }
            return result;
        }

        public static long ToLong(this object value, long result = 0)
        {
            try
            {
                result = Convert.ToInt64(value);
            }
            catch (Exception)
            { }
            return result;
        }

        public static string ToJson(this object obj)
        {
            var timeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
            return JsonConvert.SerializeObject(obj, timeConverter);
        }

        public static string ToJsonDateNoTime(this object obj)
        {
            var timeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd" };
            return JsonConvert.SerializeObject(obj, timeConverter);
        }

        public static T ToObject<T>(this string Json)
        {
            return Json == null ? default(T) : JsonConvert.DeserializeObject<T>(Json);
        }

        /// <summary>
        /// 时间格式化(日期+时间)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToDateTimeString(this DateTime obj)
        {
            return obj.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 时间格式化(日期)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToDateString(this DateTime obj)
        {
            return obj.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 时间格式化(时间)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToTimeString(this DateTime obj)
        {
            return obj.ToString("HH:mm:ss");
        }

        /// <summary>
        /// 时间差天数计算
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static int DiffDays(this DateTime startTime, DateTime endTime)
        {
            TimeSpan daysSpan = new TimeSpan(endTime.Ticks - startTime.Ticks);
            return daysSpan.TotalDays.ToInt2();
        }

        /// <summary>
        /// 首字母小写
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FirstLetterLower(this string str)
        {
            string res = str.Substring(0, 1).ToLower() + str.Substring(1, str.Length - 1);
            return res;
        }

        /// <summary>
        /// 名称全称美化(去掉第一级)
        /// </summary>
        /// <param name="name">名称全称</param>
        /// <returns></returns>
        public static string BeautifyFullName(this string name)
        {
            string result = name;
            try
            {
                var list = name.Split('/').ToList();
                if (list.Count > 1)
                {
                    list.RemoveAt(0);
                    result = string.Join("/", list);
                }
            }
            catch (Exception)
            {
                result = name;
            }
            return result.Trim('/');
        }

        /// <summary>
        /// 全局登录锁标识
        /// </summary>
        public static object LoginLock = new object();
        /// <summary>
        /// 登录提示
        /// </summary>
        public static Dictionary<string, string> LoginMessage = new Dictionary<string, string>();
    }
}
