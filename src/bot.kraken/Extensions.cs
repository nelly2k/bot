using System.Collections.Generic;

namespace bot.kraken
{
    public static class Extensions
    {
        public static Dictionary<string, string> AddParam(this Dictionary<string, string> dic, string field, string par)
        {
            if (!string.IsNullOrEmpty(par))
            {
                dic.Add(field, par);
            }
            return dic;
        }

        public static Dictionary<string, string> AddParam(this Dictionary<string, string> dic, string field, string[] par)
        {
            if (par.Length >0)
            {
                dic.Add(field, string.Join(",", par));
            }
            return dic;
        }
    }
}
