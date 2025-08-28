using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApiModel
{
    public class PubMehtod
    {
        public static string GetCsharpName(string dbColumnName)
        {
            if (dbColumnName.Contains("_"))
            {
                dbColumnName = dbColumnName.TrimEnd('_');
                dbColumnName = dbColumnName.TrimStart('_');
                var array = dbColumnName.Split('_').Select(it => GetFirstUpper(it)).ToArray();
                return string.Join("", array);
            }
            else
            {
                return GetFirstUpper(dbColumnName);
            }
        }

        private static string GetFirstUpper(string dbColumnName, bool islower = true)
        {
            if (dbColumnName == null)
                return null;

            if (islower)
            {
                //return dbColumnName.Substring(0, 1).ToUpper() + dbColumnName.Substring(1).ToLower();
                return Regex.Replace(dbColumnName, @"(^\w)|(\s\w)", m => m.Value.ToUpper());
            }
            else
            {
                //return dbColumnName.Substring(0, 1).ToUpper() + dbColumnName.Substring(1);
                return Regex.Replace(dbColumnName, @"(^\w)|(\s\w)", m => m.Value.ToLower());
            }
        }
    }
}
