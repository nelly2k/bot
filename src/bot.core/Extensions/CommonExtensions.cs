using System;
using System.Linq;
using bot.model;
using Microsoft.Practices.Unity;

namespace bot.core.Extensions
{
    public static class CommonExtensions
    {

        public static void RegisterAssembleyWith<T>(this UnityContainer container)
        {
            var baseService = typeof(IService);

            var services = typeof(T).Assembly.GetTypes().Where(x => baseService.IsAssignableFrom(x));

            foreach (var interf in services.Where(x => x.IsInterface))
            {
                var implementation = services.FirstOrDefault(x => interf.IsAssignableFrom(x) && !x.IsInterface);
                if (implementation == null)
                {
                    continue;
                }

                container.RegisterType(interf, implementation);
            }
        }

        public static void RegisterDateTime(this UnityContainer container)
        {
            container.RegisterType<IDateTime, DateTimeService>();
        }
    }
}