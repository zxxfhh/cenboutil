using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CenBoCommon.Zxx
{
    public static class Operator
    {
        private static readonly DateTime _dt1970 = new DateTime(1970, 1, 1);

        private static readonly DateTimeOffset _dto1970 = new DateTimeOffset(new DateTime(1970, 1, 1));

        public static List<int> ToIntList(this string ids, char sp = ',')
        {
            List<int> result = new List<int>();

            try
            {
                var strarr = ids.Split(sp);
                for (int i = 0; i < strarr.Length; i++)
                {
                    if (!result.Contains(strarr[i].ToZxxInt()))
                    {
                        result.Add(strarr[i].ToZxxInt());
                    }
                }
            }
            catch (Exception)
            {
                result.Clear();
            }
            return result;
        }

        public static List<long> ToLongList(this string ids, char sp = ',')
        {
            List<long> result = new List<long>();

            try
            {
                var strarr = ids.Split(sp);
                for (int i = 0; i < strarr.Length; i++)
                {
                    if (!result.Contains(strarr[i].ToZxxLong()))
                    {
                        result.Add(strarr[i].ToZxxLong());
                    }
                }
            }
            catch (Exception)
            {
                result.Clear();
            }
            return result;
        }

        public static List<string> ToStringList(this string values, char sp = ',')
        {
            List<string> result = new List<string>();

            try
            {
                var strarr = values.Split(sp);
                for (int i = 0; i < strarr.Length; i++)
                {
                    result.Add(strarr[i]);
                }
            }
            catch (Exception)
            {
                result.Clear();
            }
            return result;
        }

        public static int ToZxxInt(this object value)
        {
            int defaultValue = 0;
            if (value is int)
            {
                return (int)value;
            }

            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }

            if (value is string text)
            {
                string str = text.Replace(",", null);
                str = ToDBC(str).Trim();
                if (!str.IsZxxNullOrEmpty())
                {
                    if (!int.TryParse(str, out var result))
                    {
                        return defaultValue;
                    }

                    return result;
                }

                return defaultValue;
            }

            if (value is IList<string> list)
            {
                if (list.Count == 0)
                {
                    return defaultValue;
                }

                if (int.TryParse(list[0], out var result2))
                {
                    return result2;
                }
            }

            if (value is DateTime dateTime)
            {
                if (dateTime == DateTime.MinValue)
                {
                    return 0;
                }

                if (dateTime == DateTime.MaxValue)
                {
                    return -1;
                }

                double totalSeconds = (dateTime - _dt1970).TotalSeconds;
                if (!(totalSeconds >= 2147483647.0))
                {
                    return (int)totalSeconds;
                }

                throw new InvalidDataException("时间过大，数值超过Int32.MaxValue");
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                if (dateTimeOffset == DateTimeOffset.MinValue)
                {
                    return 0;
                }

                double totalSeconds2 = (dateTimeOffset - _dto1970).TotalSeconds;
                if (!(totalSeconds2 >= 2147483647.0))
                {
                    return (int)totalSeconds2;
                }

                throw new InvalidDataException("时间过大，数值超过Int32.MaxValue");
            }

            if (value is byte[] array)
            {
                if (array == null || array.Length == 0)
                {
                    return defaultValue;
                }

                switch (array.Length)
                {
                    case 1:
                        return array[0];
                    case 2:
                        return BitConverter.ToInt16(array, 0);
                    case 3:
                        return BitConverter.ToInt32(new byte[4]
                        {
                    array[0],
                    array[1],
                    array[2],
                    0
                        }, 0);
                    case 4:
                        return BitConverter.ToInt32(array, 0);
                }
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static long ToZxxLong(this object value)
        {
            long defaultValue = 0;
            if (value is long)
            {
                return (long)value;
            }

            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }

            if (value is string text)
            {
                string str = text.Replace(",", null);
                str = ToDBC(str).Trim();
                if (!str.IsZxxNullOrEmpty())
                {
                    if (!long.TryParse(str, out var result))
                    {
                        return defaultValue;
                    }

                    return result;
                }

                return defaultValue;
            }

            if (value is IList<string> list)
            {
                if (list.Count == 0)
                {
                    return defaultValue;
                }

                if (long.TryParse(list[0], out var result2))
                {
                    return result2;
                }
            }

            if (value is DateTime dateTime)
            {
                if (dateTime == DateTime.MinValue)
                {
                    return 0L;
                }

                return (long)(dateTime - _dt1970).TotalMilliseconds;
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                if (!(dateTimeOffset == DateTimeOffset.MinValue))
                {
                    return (long)(dateTimeOffset - _dto1970).TotalMilliseconds;
                }

                return 0L;
            }

            if (value is byte[] array)
            {
                if (array == null || array.Length == 0)
                {
                    return defaultValue;
                }

                switch (array.Length)
                {
                    case 1:
                        return array[0];
                    case 2:
                        return BitConverter.ToInt16(array, 0);
                    case 3:
                        return BitConverter.ToInt32(new byte[4]
                        {
                    array[0],
                    array[1],
                    array[2],
                    0
                        }, 0);
                    case 4:
                        return BitConverter.ToInt32(array, 0);
                    case 8:
                        return BitConverter.ToInt64(array, 0);
                }
            }

            try
            {
                return Convert.ToInt64(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static double ToZxxDouble(this object value)
        {
            double defaultValue = 0;
            if (value is double num)
            {
                if (!double.IsNaN(num))
                {
                    return num;
                }

                return defaultValue;
            }

            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }

            if (value is string str)
            {
                string text = ToDBC(str).Trim();
                if (!text.IsZxxNullOrEmpty())
                {
                    if (!double.TryParse(text, out var result))
                    {
                        return defaultValue;
                    }

                    return result;
                }

                return defaultValue;
            }

            if (value is IList<string> list)
            {
                if (list.Count == 0)
                {
                    return defaultValue;
                }

                if (double.TryParse(list[0], out var result2))
                {
                    return result2;
                }
            }

            if (value is byte[] array && array.Length <= 8)
            {
                return BitConverter.ToDouble(array, 0);
            }

            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static decimal ToZxxDecimal(this object value)
        {
            decimal defaultValue = 0;
            if (value is decimal)
            {
                return (decimal)value;
            }

            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }

            if (value is string str)
            {
                string text = ToDBC(str).Trim();
                if (!text.IsZxxNullOrEmpty())
                {
                    if (!decimal.TryParse(text, out var result))
                    {
                        return defaultValue;
                    }

                    return result;
                }

                return defaultValue;
            }

            if (value is IList<string> list)
            {
                if (list.Count == 0)
                {
                    return defaultValue;
                }

                if (decimal.TryParse(list[0], out var result2))
                {
                    return result2;
                }
            }

            byte[] array = value as byte[];
            if (array != null)
            {
                if (array == null || array.Length == 0)
                {
                    return defaultValue;
                }

                switch (array.Length)
                {
                    case 1:
                        return array[0];
                    case 2:
                        return BitConverter.ToInt16(array, 0);
                    case 3:
                        return BitConverter.ToInt32(new byte[4]
                        {
                    array[0],
                    array[1],
                    array[2],
                    0
                        }, 0);
                    case 4:
                        return BitConverter.ToInt32(array, 0);
                    default:
                        if (array.Length < 8)
                        {
                            byte[] array2 = new byte[8];
                            Buffer.BlockCopy(array, 0, array2, 0, array.Length);
                            array = array2;
                        }

                        return (decimal)BitConverter.ToDouble(array, 0);
                }
            }

            if (value is double num)
            {
                if (!double.IsNaN(num))
                {
                    return (decimal)num;
                }

                return defaultValue;
            }

            try
            {
                return Convert.ToDecimal(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static bool ToZxxBoolean(this object value)
        {
            bool defaultValue = false;
            if (value is bool)
            {
                return (bool)value;
            }

            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }

            if (value is string text)
            {
                string text2 = text.Trim();
                if (text2.IsZxxNullOrEmpty())
                {
                    return defaultValue;
                }

                if (bool.TryParse(text2, out var result))
                {
                    return result;
                }

                if (string.Equals(text2, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(text2, bool.FalseString, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                text2 = ToDBC(text2);
                if (!int.TryParse(text2, out var result2))
                {
                    return defaultValue;
                }

                return result2 > 0;
            }

            if (value is IList<string> list)
            {
                if (list.Count == 0)
                {
                    return defaultValue;
                }

                if (int.TryParse(list[0], out var result3))
                {
                    return result3 > 0;
                }
            }

            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        private static string ToDBC(string str)
        {
            char[] array = str.ToCharArray();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == '\u3000')
                {
                    array[i] = ' ';
                    continue;
                }

                char c = array[i];
                if (c > '\uff00' && c < '｟')
                {
                    array[i] = (char)(array[i] - 65248);
                }
            }

            return new string(array);
        }

        public static bool IsZxxNullOrEmpty(this string obj)
        {
            return string.IsNullOrEmpty(obj) && string.IsNullOrWhiteSpace(obj);
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
        /// 字符串转时间
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static DateTime ToZxxDateTime(this object obj)
        {
            DateTime time = DateTime.MinValue;
            if (obj == null) return time;
            DateTime.TryParse(obj.ToString(), out time);
            return time;
        }

        /// <summary>
        /// 强制刷成每隔N分钟的倍数(四舍五入)
        /// </summary>
        /// <param name="time">当前时间</param>
        /// <param name="round">每隔N分钟</param>
        /// <returns></returns>
        public static DateTime RangeTimeByMinute(this DateTime time, int round = 5)
        {
            int minutes = time.Minute;
            //使用MidpointRounding.AwayFromZero可以确保在四舍五入时，当数字正好处于中间位置时，将向离0更远的一侧舍入。
            int roundingMinutes = (int)Math.Round((double)minutes / round, MidpointRounding.AwayFromZero) * round;
            return time.Date.AddHours(time.Hour).AddMinutes(roundingMinutes);
        }

        /// <summary>
        /// 时间戳（毫秒）转换为DataTime
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns></returns>
        public static DateTime TimestampToDataTime(this long unixTimeStamp)
        {
            DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeStamp).LocalDateTime;
            return dateTime;
        }

        /// <summary>
        /// DataTime转时间戳（毫秒）
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long DataTimeToTimestamp(this DateTime dateTime)
        {
            // 创建一个 DateTime 对象，例如当前时间
            DateTime _dateTime = DateTime.UtcNow; // 使用 UTC 时间
            // 将 DateTime 对象转换为 DateTimeOffset 对象
            DateTimeOffset dateTimeOffset = new DateTimeOffset(_dateTime);
            // 获取 Unix 时间戳（以毫秒为单位）
            long timeStamp = dateTimeOffset.ToUnixTimeMilliseconds();

            return timeStamp;
        }

        /// <summary>
        /// 时间差小时数计算
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static int DiffMinutes(this DateTime startTime, DateTime endTime)
        {
            TimeSpan daysSpan = new TimeSpan(endTime.Ticks - startTime.Ticks);
            return daysSpan.Minutes;
        }

        /// <summary>
        /// 时间差小时数计算
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static int DiffHours(this DateTime startTime, DateTime endTime)
        {
            TimeSpan daysSpan = new TimeSpan(endTime.Ticks - startTime.Ticks);
            return daysSpan.Hours;
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
            return daysSpan.Days;
        }

        /// <summary>
        /// 时间差周数计算（按自然周分段计算，跨周即算1周）
        /// </summary>
        /// <param name="startTime">开始日期</param>
        /// <param name="endTime">结束日期</param>
        /// <returns>周数差（至少1周）</returns>
        public static int DiffWeeks(this DateTime startTime, DateTime endTime)
        {
            // 确保 startTime <= endTime
            if (startTime > endTime)
            {
                DateTime temp = startTime;
                startTime = endTime;
                endTime = temp;
            }

            // 获取起始周的周一和结束周的周日
            DateTime firstDayOfStartWeek = startTime.GetFirstDayOfWeek();
            DateTime lastDayOfEndWeek = endTime.GetLastDayOfWeek();

            // 计算完整的周数（总天数 / 7）
            int totalDays = (lastDayOfEndWeek - firstDayOfStartWeek).Days + 1;
            int totalWeeks = totalDays / 7;

            // 如果时间跨度不足7天，但跨周（如周一~周二），仍算1周
            return Math.Max(1, totalWeeks);
        }

        /// <summary>
        /// 时间差月数计算（严格按自然月分段计算，跨月即算1月）
        /// 规则：
        /// 1. 将时间跨度按自然月分段，每一段（即使部分天数）算1个月。
        /// 2. 示例：
        ///    2024-01-15 → 2024-02-16 → 2个月（1月15-31日 + 2月1-16日）
        ///    2024-01-31 → 2024-02-28 → 2个月（1月31日 + 2月1-28日）
        ///    2024-01-01 → 2024-01-31 → 1个月（完整1月）
        /// </summary>
        public static int DiffMonths(this DateTime startTime, DateTime endTime)
        {
            if (startTime > endTime)
            {
                DateTime temp = startTime;
                startTime = endTime;
                endTime = temp;
            }

            // 计算完整的年差和月差
            int totalMonths = (endTime.Year - startTime.Year) * 12 + (endTime.Month - startTime.Month);

            // 关键修正：只要跨过自然月（即使部分天数），就额外+1
            if (startTime.Day > 1 || endTime.Day < DateTime.DaysInMonth(endTime.Year, endTime.Month))
            {
                totalMonths++;
            }

            // 至少为1个月
            return Math.Max(1, totalMonths);
        }

        /// <summary>
        /// 时间差年数计算（按自然年分段计算，跨年即算1周）
        /// </summary>
        public static int DiffYears(this DateTime startTime, DateTime endTime)
        {
            // 确保 startTime <= endTime
            if (startTime > endTime)
            {
                DateTime temp = startTime;
                startTime = endTime;
                endTime = temp;
            }

            // 直接计算起始年和结束年的差值
            return endTime.Year - startTime.Year;
        }

        /// <summary>
        /// 获取第几周
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int GetWeekOfYear(this DateTime date)
        {
            CultureInfo culture = CultureInfo.InvariantCulture; // 或指定 ISO 标准的区域
            CalendarWeekRule weekRule = CalendarWeekRule.FirstFourDayWeek;
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday; // 根据习惯指定每周从星期几开始(如周一或周日)
            return culture.Calendar.GetWeekOfYear(date, weekRule, firstDayOfWeek);
        }

        /// <summary>
        /// 获取周的第一天
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetFirstDayOfWeek(this DateTime date)
        {
            int delta = DayOfWeek.Monday - date.DayOfWeek;
            if (delta > 0)
                delta -= 7;
            return date.AddDays(delta);
        }

        /// <summary>
        /// 获取周的最后一天
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetLastDayOfWeek(this DateTime date)
        {
            return GetFirstDayOfWeek(date).AddDays(6);
        }

        /// <summary>
        /// 根据年数和周数获取周的第一天
        /// </summary>
        /// <param name="year"></param>
        /// <param name="week"></param>
        /// <returns></returns>
        public static DateTime GetFirstDayOfWeek(this int year, int week)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
            DateTime firstMonday = jan1.AddDays(daysOffset);
            DateTime startOfWeek = firstMonday.AddDays((week - 1) * 7);
            return startOfWeek;
        }

        /// <summary>
        /// 根据年数和周数获取周的最后一天
        /// </summary>
        /// <param name="year"></param>
        /// <param name="week"></param>
        /// <returns></returns>
        public static DateTime GetEndDateOfWeek(this int year, int week)
        {
            DateTime startOfWeek = GetFirstDayOfWeek(year, week);
            DateTime endOfWeek = startOfWeek.AddDays(6);
            return endOfWeek;
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
        public static string BeautifyFullName(this string name, string separator = "|")
        {
            if (name.IsZxxNullOrEmpty()) return string.Empty;
            string result = name;
            try
            {
                var list = name.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (list.IsZxxAny())
                {
                    if (list.Count == 1)
                    {
                        result = list[0];
                    }
                    else
                    {
                        list.RemoveAt(0);
                        result = string.Join(separator, list);
                    }
                }
            }
            catch (Exception)
            {
                result = name;
            }
            return result.Trim();
        }

        /// <summary>
        /// 获取当前毫秒时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetZxxTimeStamp()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
        /// <summary>
        /// 取指定时间的时间戳
        /// </summary>
        /// <param name="accurateToMilliseconds">是否精确到毫秒</param>
        /// <returns>返回long类型时间戳</returns>
        public static long GetTimeStamp(this DateTime dateTime, bool accurateToMilliseconds = false)
        {
            if (accurateToMilliseconds)
            {
                return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
            }
            else
            {
                return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
            }
        }

        /// <summary>
        /// 指定时间戳转为时间。
        /// </summary>
        /// <param name="timeStamp">需要被反转的时间戳</param>
        /// <param name="accurateToMilliseconds">是否精确到毫秒</param>
        /// <returns>返回时间戳对应的DateTime</returns>
        public static DateTime GetStampTime(this long timeStamp, bool accurateToMilliseconds = false)
        {
            if (accurateToMilliseconds)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).LocalDateTime;
            }
            else
            {
                return DateTimeOffset.FromUnixTimeSeconds(timeStamp).LocalDateTime;
            }
        }

        /// <summary>
        /// List为空判断
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool IsZxxAny<T>(this List<T> list)
        {
            bool IsAny = false;
            if (list != null && list.Count > 0)
            {
                IsAny = true;
            }
            return IsAny;
        }

        /// <summary>
        /// IEnumerable为空判断
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool IsZxxAny<T>(this IEnumerable<T> list)
        {
            bool IsAny = false;
            if (list != null && list.Count() > 0)
            {
                IsAny = true;
            }
            return IsAny;
        }

        /// <summary>
        /// 集合字段转成特殊字符串(默认逗号分隔)
        /// </summary>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ListZdToString(this List<object> list, string separator = ",")
        {
            if (list.Count == 0) return string.Empty;
            return string.Join(separator, list);
        }

        /// <summary>
        /// 集合字段转成特殊字符串(默认逗号分隔)
        /// </summary>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ListZdToString(this IEnumerable<object> list, string separator = ",")
        {
            if (list.Count() == 0) return string.Empty;
            return string.Join(separator, list);
        }

        /// <summary>
        /// 集合字段转成特殊字符串(默认逗号分隔)
        /// </summary>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ListIntZdToString(this List<int> list, string separator = ",")
        {
            if (list.Count == 0) return string.Empty;
            return string.Join(separator, list);
        }

        /// <summary>
        /// 集合字段转成特殊字符串(默认逗号分隔)
        /// </summary>
        /// <param name="list"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ListIntZdToString(this IEnumerable<int> list, string separator = ",")
        {
            if (list.Count() == 0) return string.Empty;
            return string.Join(separator, list);
        }

    }
}
