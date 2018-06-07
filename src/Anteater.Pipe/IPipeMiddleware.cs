namespace Anteater.Pipe
{
    using System;
    using System.Threading.Tasks;

    public interface IPipeMiddleware
    {
        Task<IPipeEcho> HandleAsync(IPipeProjectile projectile, Func<Task<IPipeEcho>> next);
    }

    public interface IPipeMiddleware<in TProjectile> : IPipeMiddleware
        where TProjectile : IPipeProjectile
    {
    }
}
