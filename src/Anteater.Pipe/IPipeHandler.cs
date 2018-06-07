namespace Anteater.Pipe
{
    using System.Threading.Tasks;

    public interface IPipeHandler<in TProjectile>
        where TProjectile : IPipeProjectile
    {
        Task HandleAsync(TProjectile projectile);
    }

    public interface IPipeHandler<in TProjectile, TResult>
        where TProjectile : IPipeProjectile
    {
        Task<TResult> HandleAsync(TProjectile projectile);
    }
}
