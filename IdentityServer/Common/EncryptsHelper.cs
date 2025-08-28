using System.Security.Cryptography;
using System.Text;

namespace IdentityServer.Encrypt
{
    public class EncryptsHelper
    {
        private static string DESKey = "CenboIdentity";
        /// <summary> 
        /// 使用缺省密钥字符串加密 
        /// </summary> 
        /// <param name="key">明文</param> 
        /// <returns>密文</returns> 
        public static string Encrypt(string key)
        {
            return MEncrypt(key, DESKey);
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="key">明文</param>
        /// <param name="secret">密文</param>
        /// <returns></returns>
        public static string Encrypt(string key, string secret)
        {
            return MEncrypt(key, secret);
        }

        /// <summary> 
        /// 使用缺省密钥解密 
        /// </summary> 
        /// <param name="key">密文</param> 
        /// <returns>明文</returns> 
        public static string Decrypt(string key)
        {
            return MDecrypt(key, DESKey);
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="key">明文</param>
        /// <param name="secret">密文</param>
        /// <returns></returns>
        public static string Decrypt(string key, string secret)
        {
            return MDecrypt(key, secret);
        }

        /// <summary> 
        /// 加密数据 
        /// </summary> 
        /// <param name="Text"></param> 
        /// <param name="sKey"></param> 
        /// <returns></returns> 
        private static string MEncrypt(string Text, string sKey)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(Text);
            using (var des = DES.Create())
            {
                List<byte> keyBytes = new List<byte>();
                var sbuff = ASCIIEncoding.ASCII.GetBytes(sKey).ToList();
                if (sbuff.Count <= 8)
                {
                    keyBytes.AddRange(sbuff);
                    for (int i = sbuff.Count; i < 8; i++)
                    {
                        keyBytes.Add(0);
                    }
                }
                else if (sbuff.Count > 8)
                {
                    keyBytes.AddRange(sbuff.GetRange(0, 8));
                }
                des.Key = keyBytes.ToArray();
                des.IV = des.Key;
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(inputBytes, 0, inputBytes.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }

        }

        /// <summary> 
        /// 解密数据 
        /// </summary> 
        /// <param name="text"></param> 
        /// <param name="sKey"></param> 
        /// <returns></returns> 
        private static string MDecrypt(string text, string sKey)
        {
            byte[] inputBytes = Convert.FromBase64String(text);
            using (var des = DES.Create())
            {
                List<byte> keyBytes = new List<byte>();
                var sbuff = ASCIIEncoding.ASCII.GetBytes(sKey).ToList();
                if (sbuff.Count <= 8)
                {
                    keyBytes.AddRange(sbuff);
                    for (int i = sbuff.Count; i < 8; i++)
                    {
                        keyBytes.Add(0);
                    }
                }
                else if (sbuff.Count > 8)
                {
                    keyBytes.AddRange(sbuff.GetRange(0, 8));
                }
                des.Key = keyBytes.ToArray();
                des.IV = des.Key;
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(inputBytes, 0, inputBytes.Length);
                        cs.FlushFinalBlock();
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
        }

        /// <summary> 
        /// 返回16位Md5加密结果
        /// </summary> 
        /// <param name="original">数据源</param> 
        /// <param name="issmall">是否小写(默认是)</param> 
        /// <returns>摘要</returns> 
        public static string MD5Make16(string original, bool issmall = true)
        {
            //将要加密的字符串转换成字节数组
            byte[] strbt = Encoding.UTF8.GetBytes(original);
            var byteArray = MakeMD5(strbt);
            string result = "";
            if (issmall)
            {
                result = BitConverter.ToString(byteArray, 4, 12).Replace("-", "").ToLower();
            }
            else
            {
                result = BitConverter.ToString(byteArray, 4, 12).Replace("-", "").ToUpper();
            }
            return result;
        }

        /// <summary> 
        /// 返回32位Md5加密结果
        /// </summary> 
        /// <param name="original">数据源</param> 
        /// <param name="issmall">是否小写(默认是)</param> 
        /// <returns>摘要</returns> 
        public static string MD5Make32(string original, bool issmall = true)
        {
            //将要加密的字符串转换成字节数组
            byte[] strbt = Encoding.UTF8.GetBytes(original);
            var byteArray = MakeMD5(strbt);
            string result = "";
            if (issmall)
            {
                result = BitConverter.ToString(byteArray).Replace("-", "").ToLower();
            }
            else
            {
                result = BitConverter.ToString(byteArray).Replace("-", "").ToUpper();
            }
            return result;
        }

        /// <summary> 
        /// 生成MD5摘要 
        /// </summary> 
        /// <param name="original">数据源</param> 
        /// <returns>摘要</returns> 
        private static byte[] MakeMD5(byte[] original)
        {
            using (MD5 md5 = MD5.Create())
            {
                //对转换后的字节进行MD5加密
                byte[] keyhash = md5.ComputeHash(original);
                return keyhash;
            }
        }

    }
}
