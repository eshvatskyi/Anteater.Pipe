namespace Anteater.Pipe
{
    using System.Threading.Tasks;

    public interface IPipeHandler<in TProjectile>
        where TProjectile : IPipeProjectile
    {
    }

    internal interface IPipeAction
    {
        Task HandleAsync(IPipeProjectile projectile);
    }

    internal interface IPipeFunction<TResult>
    {
        Task<TResult> HandleAsync(IPipeProjectile projectile);
    }

    public abstract class PipeHandler<TProjectile> : IPipeHandler<TProjectile>, IPipeAction
        where TProjectile : IPipeProjectile
    {
        public abstract Task HandleAsync(TProjectile projectile);

        Task IPipeAction.HandleAsync(IPipeProjectile projectile) => HandleAsync((TProjectile)projectile);
    }

    public abstract class PipeHandler<TProjectile, TResult> : IPipeHandler<TProjectile>, IPipeFunction<TResult>
        where TProjectile : IPipeProjectile
    {
        public abstract Task<TResult> HandleAsync(TProjectile projectile);

        Task<TResult> IPipeFunction<TResult>.HandleAsync(IPipeProjectile projectile) => HandleAsync((TProjectile)projectile);
    }
}
