using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CenboGeneral
{
    public static class Operator
    {

        public static string ToJson(this object obj)
        {
            try
            {
                var timeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
                return JsonConvert.SerializeObject(obj, timeConverter);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string ToJsonDateNoTime(this object obj)
        {
            try
            {
                var timeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd" };
                return JsonConvert.SerializeObject(obj, timeConverter);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static T ToObject<T>(this string Json)
        {
            try
            {
                return Json == null ? default(T) : JsonConvert.DeserializeObject<T>(Json);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
