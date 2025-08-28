using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CenBoCommon.Zxx
{
    /// <summary>
    /// 进制转换
    /// </summary>
    public static class YjJinZhiChange
    {
        /// <summary>
        /// 2进制字符串转字典
        /// </summary>
        /// <param name="digit">2进制字符串</param>
        /// <returns></returns>
        public static Dictionary<int, int> Change2ToDic(this string digit)
        {
            Dictionary<int, int> dic = new Dictionary<int, int>();

            try
            {
                char[] charArray = digit.ToArray();
                Array.Reverse(charArray);
                for (int i = 0; i < charArray.Length; i++)
                {
                    dic.Add(i, charArray[i].ToZxxInt() - 48);
                }
            }
            catch (Exception ex)
            {
                throw null;
            }
            return dic;
        }

        /// <summary>
        /// 字符串转字典
        /// </summary>
        /// <param name="digit">字符串</param>
        /// <returns></returns>
        public static Dictionary<int, int> ChangeToDic(this string digit)
        {
            Dictionary<int, int> dic = new Dictionary<int, int>();

            try
            {
                char[] charArray = digit.ToArray();
                for (int i = 0; i < charArray.Length; i++)
                {
                    dic.Add(i, charArray[i].ToZxxInt() - 48);
                }
            }
            catch (Exception ex)
            {
                throw null;
            }
            return dic;
        }

        public static byte ConvertBCDToInt(this byte b)
        {
            //高四位  
            byte b1 = (byte)((b >> 4) & 0xF);
            //低四位  
            byte b2 = (byte)(b & 0xF);

            return (byte)(b1 * 10 + b2);
        }

        /// <summary>
        /// 字符串反转
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ReversStr(this string input)
        {
            char[] array = input.ToCharArray();
            IEnumerable<char> cs = array.Reverse<char>();
            char[] array1 = cs.ToArray<char>();
            string result = new string(array1);
            return result;
        }

        /// <summary>
        /// 需转2进制字符串再转成16进制
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FirstTo2ThenTo16(this string input)
        {
            string hex = "";
            var idlist = input.ToIntList();
            if (idlist.Any())
            {
                string value2jz1 = "";
                for (int i = 0; i < 16; i++)
                {
                    if (idlist.Contains(i))
                    {
                        value2jz1 += "1";
                    }
                    else
                    {
                        value2jz1 += "0";
                    }
                }
                //2进制转16进制
                hex += value2jz1.ReversStr().ConvertJzChange(2, 16);
            }
            return hex;
        }

        /// <summary>
        /// 进制转换
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fromType">原来的进制格式</param>
        /// <param name="toType">要转换成的进制格式</param>
        /// <returns></returns>
        public static string ConvertJzChange(this string input, byte fromType, byte toType)
        {
            string output = input;
            switch (fromType)
            {
                case 2:
                    output = ConvertGenericBinaryFromBinary(input, toType);
                    break;
                case 8:
                    output = ConvertGenericBinaryFromOctal(input, toType);
                    break;
                case 10:
                    output = ConvertGenericBinaryFromDecimal(input, toType);
                    break;
                case 16:
                    output = ConvertGenericBinaryFromHexadecimal(input, toType);
                    break;
                default:
                    break;
            }
            return output.ToUpper();
        }

        /// <summary>
        /// 从二进制转换成其他进制
        /// </summary>
        /// <param name="input"></param>
        /// <param name="toType"></param>
        /// <returns></returns>
        private static string ConvertGenericBinaryFromBinary(string input, byte toType)
        {
            switch (toType)
            {
                case 8:
                    //先转换成十进制然后转八进制
                    input = Convert.ToString(Convert.ToInt32(input, 2), 8);
                    break;
                case 10:
                    input = Convert.ToInt32(input, 2).ToString();
                    break;
                case 16:
                    input = Convert.ToString(Convert.ToInt32(input, 2), 16);
                    break;
                default:
                    break;
            }
            return input;
        }

        /// <summary>
        /// 从八进制转换成其他进制
        /// </summary>
        /// <param name="input"></param>
        /// <param name="toType"></param>
        /// <returns></returns>
        private static string ConvertGenericBinaryFromOctal(string input, byte toType)
        {
            switch (toType)
            {
                case 2:
                    input = Convert.ToString(Convert.ToInt32(input, 8), 2);
                    break;
                case 10:
                    input = Convert.ToInt32(input, 8).ToString();
                    break;
                case 16:
                    input = Convert.ToString(Convert.ToInt32(input, 8), 16);
                    break;
                default:
                    break;
            }
            return input;
        }

        /// <summary>
        /// 从十进制转换成其他进制
        /// </summary>
        /// <param name="input"></param>
        /// <param name="toType"></param>
        /// <returns></returns>
        private static string ConvertGenericBinaryFromDecimal(string input, int toType)
        {
            string output = "";
            int intInput = Convert.ToInt32(input);
            switch (toType)
            {
                case 2:
                    output = Convert.ToString(intInput, 2);
                    break;
                case 8:
                    output = Convert.ToString(intInput, 8);
                    break;
                case 16:

                    break;
                default:
                    output = input;
                    break;
            }
            return output;
        }

        /// <summary>
        /// 从十六进制转换成其他进制
        /// </summary>
        /// <param name="input"></param>
        /// <param name="toType"></param>
        /// <returns></returns>
        private static string ConvertGenericBinaryFromHexadecimal(string input, int toType)
        {
            switch (toType)
            {
                case 2:
                    input = Convert.ToString(Convert.ToInt32(input, 16), 2);
                    break;
                case 8:
                    input = Convert.ToString(Convert.ToInt32(input, 16), 8);
                    break;
                case 10:
                    input = Convert.ToInt32(input, 16).ToString();
                    break;
                default:
                    break;
            }
            return input;
        }

        /// <summary>
        /// 10进制转16
        /// </summary>
        /// <param name="input">10进制数字</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public static string Change10To16(this int input, int length)
        {
            return Convert.ToString(input, 16).PadLeft(length, '0').ToUpper();
        }

        /// <summary>
        /// 16进制转10
        /// </summary>
        /// <param name="input">16进制数字</param>
        /// <returns></returns>
        public static int Change16To10(this string input)
        {
            return Convert.ToInt32(input, 16);
        }

        #region 字节数组和16进制字符串相互转换

        /// <summary>
        /// 字节数组转16进制字符串
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string ToZxxHex(this byte[] data, int offset = 0, int count = -1)
        {
            if (data == null || data.Length == 0)
            {
                return "";
            }

            if (count < 0)
            {
                count = data.Length - offset;
            }
            else if (offset + count > data.Length)
            {
                count = data.Length - offset;
            }

            if (count == 0)
            {
                return "";
            }

            char[] array = new char[count * 2];
            int num = 0;
            int num2 = 0;
            while (num < count)
            {
                byte b = data[offset + num];
                array[num2] = GetHexValue(b >> 4);
                array[num2 + 1] = GetHexValue(b & 0xF);
                num++;
                num2 += 2;
            }

            return new string(array);
        }

        private static char GetHexValue(this int i)
        {
            if (i >= 10)
            {
                return (char)(i - 10 + 65);
            }

            return (char)(i + 48);
        }

        /// <summary>
        /// 字节转16进制字符串
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string ToZxxHex(this byte b)
        {
            char[] array = new char[2];
            int num = b >> 4;
            int num2 = b & 0xF;
            array[0] = (char)((num >= 10) ? (65 + num - 10) : (48 + num));
            array[1] = (char)((num2 >= 10) ? (65 + num2 - 10) : (48 + num2));
            return new string(array);
        }

        /// <summary>
        /// 16进制字符串转字节数组
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] ToZxxHex(this string data, int startIndex = 0, int length = -1)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new byte[0];
            }

            data = data.Trim().Replace("-", null).Replace("0x", null)
                .Replace("0X", null)
                .Replace(" ", null)
                .Replace("\r", null)
                .Replace("\n", null)
                .Replace(",", null);
            if (length <= 0)
            {
                length = data.Length - startIndex;
            }

            byte[] array = new byte[length / 2];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = byte.Parse(data.Substring(startIndex + 2 * i, 2), NumberStyles.HexNumber);
            }

            return array;
        }

        #endregion

    }
}
