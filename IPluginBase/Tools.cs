using System;
using System.Collections.Generic;
using System.Text;

namespace IPluginBase
{
    public static class Tools
    {
        public static T? DeepClone<T>(this T model) where T : class
        {
            if (model == null)
            {
                return null;
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Newtonsoft.Json.JsonConvert.SerializeObject(model));
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static int ToInt(this string str)
        {
            return int.Parse(str);
        }

        public static int ToInt(this string str, int defaultVal)
        {
            if (str == null) return defaultVal;
            return int.Parse(str);
        }
    }
}
