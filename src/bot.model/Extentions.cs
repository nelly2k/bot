using System;
using System.Linq;
using System.Reflection;

namespace bot.model
{
    public static class Extentions
    {
        public static TEnum ToEnum<TEnum>(this string str)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), str);
        }

        public static string GetField(this Type type, Func<Config,string> propertyFunc)
        {
            var prop = type.GetProperty(propertyFunc(new Config()));
            return prop.GetField();
        }

        public static string GetField(this PropertyInfo prop)
        {
            if (prop == null)
            {
                return string.Empty;
            }
            var attr = prop.GetCustomAttributes(typeof(FieldAttribute)).FirstOrDefault();
            if (!(attr is FieldAttribute))
            {
                return string.Empty;
            }

            return (attr as FieldAttribute).Title;
        }

        public static string GetField(this Config config, Func<Config, string> propertyFunc)
        {
            var prop = typeof(Config).GetProperty(propertyFunc(config));
            if (prop == null)
            {
                return string.Empty;
            }
            var attr = prop.GetCustomAttributes(typeof(FieldAttribute)).FirstOrDefault();
            if (!(attr is FieldAttribute))
            {
                return string.Empty;
            }

            return (attr as FieldAttribute).Title;
        }

        public static void SetField(this object config, string field, object value)
        {
            var props = config.GetType().GetProperties();

            PropertyInfo propertyInfo = null;

            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttributes(typeof(FieldAttribute)).FirstOrDefault(x => (x as FieldAttribute)?.Title == field);
                if (attr != null)
                {
                    propertyInfo = prop;
                    break;
                }
            }

            if (propertyInfo == null)
            {
                return;
            }
            propertyInfo.SetValue(config, Convert.ChangeType(value, propertyInfo.PropertyType));
        }




    }
}
