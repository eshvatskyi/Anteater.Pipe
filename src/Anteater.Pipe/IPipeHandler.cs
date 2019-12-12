namespace Anteater.Pipe
{
    using System;
    using System.Threading.Tasks;

    public interface IPipeHandler
    {
    }

    public interface IPipeHandler<in TProjectile> : IPipeHandler
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

        Task IPipeAction.HandleAsync(IPipeProjectile projectile)
        {
            return HandleAsync((TProjectile)projectile);
        }
    }

    internal class DelegateHandler<TProjectile> : PipeHandler<TProjectile>
        where TProjectile : IPipeProjectile
    {
        private readonly Func<TProjectile, Task> _handle;

        public DelegateHandler(Func<TProjectile, Task> handle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public override Task HandleAsync(TProjectile projectile)
        {
            return _handle(projectile);
        }
    }

    public abstract class PipeHandler<TProjectile, TResult> : IPipeHandler<TProjectile>, IPipeFunction<TResult>
        where TProjectile : IPipeProjectile
    {
        public abstract Task<TResult> HandleAsync(TProjectile projectile);

        Task<TResult> IPipeFunction<TResult>.HandleAsync(IPipeProjectile projectile)
        {
            return HandleAsync((TProjectile)projectile);
        }
    }

    internal class DelegateHandler<TProjectile, TResult> : PipeHandler<TProjectile, TResult>
        where TProjectile : IPipeProjectile
    {
        private readonly Func<TProjectile, Task<TResult>> _handle;

        public DelegateHandler(Func<TProjectile, Task<TResult>> handle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public override Task<TResult> HandleAsync(TProjectile projectile)
        {
            return _handle(projectile);
        }
    }
}
