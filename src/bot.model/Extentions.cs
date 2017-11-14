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

        public static string Field(this Type type, Func<Config,string> propertyFunc)
        {
            //Config.getField(x=>nameof(x.))
            var prop = type.GetProperty(propertyFunc(new Config()));
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

        public static string Field(this Config config, Func<Config, string> propertyFunc)
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

        public static void Set(this Config config, string field, object value)
        {
            var props = typeof(Config).GetProperties();

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
