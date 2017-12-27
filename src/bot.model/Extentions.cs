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

        public static string GetField(this Type type, Func<Config, string> propertyFunc)
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

        public static bool HasField<T>(this T config, string field) where T : IConfig
        {
            var props = typeof(T).GetProperties();
            return props.Any(x => x.GetCustomAttributes<FieldAttribute>().Any(p => p.Title == field));
        }

        public static void SetField<T>(this T config, string field, object value) where T : IConfig
        {
            var propertyInfo = typeof(T).GetProperties().FirstOrDefault(x =>
                x.GetCustomAttributes<FieldAttribute>().Any(r => r.Title == field));
            if (propertyInfo == null)
            {
                return;
            }
            propertyInfo.SetValue(config, Convert.ChangeType(value, propertyInfo.PropertyType));
        }

    }
}
