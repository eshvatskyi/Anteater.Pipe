namespace Anteater.Pipe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;

    internal static class Extensions
    {
        public static IEnumerable<Type> AllProjectileTypes(this Type origin)
        {
            var type = typeof(IPipeProjectile);

            return Enumerable.Concat(
                origin.GetInterfaces().SelectMany(x => x.AllProjectileTypes().Append(x)),
                new[] { origin.BaseType }.Where(x => x != null).SelectMany(x => x.AllProjectileTypes()).Append(origin.BaseType))
                .OfType<Type>()
                .Where(x => type.IsAssignableFrom(x))
                .Append(origin);
        }

        public static IEnumerable<IPipeMiddleware> GetMiddlewares(this IServiceProvider provider, Type projectileType)
        {
            var type = typeof(IPipeMiddleware<>);

            return projectileType.AllProjectileTypes().Distinct()
                .SelectMany(x => provider.GetServices(type.MakeGenericType(x)))
                .OfType<IPipeMiddleware>()
                .GroupBy(x => x.GetType())
                .Select(x => x.First());
        }

        public static IEnumerable<IPipeMiddleware> GetMiddlewares<TProjectile>(this IServiceProvider provider)
            where TProjectile : IPipeProjectile
        {
            return provider.GetMiddlewares(typeof(TProjectile));
        }
    }
}
