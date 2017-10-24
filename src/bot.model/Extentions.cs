using System;

namespace bot.model
{
    public static class Extentions
    {
        public static TEnum ToEnum<TEnum>(this string str)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), str);
        }
    }
}
