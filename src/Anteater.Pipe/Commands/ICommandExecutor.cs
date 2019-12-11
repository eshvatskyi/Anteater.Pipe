namespace Anteater.Pipe
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public interface ICommandExecutor
    {
        Task ExecuteAsync(ICommand command);

        Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command);

        IDisposable HandleAsync<TCommand>(Func<TCommand, Task> handle)
            where TCommand : class, ICommand;

        IDisposable HandleAsync<TCommand, TResult>(Func<TCommand, Task<TResult>> handle)
            where TCommand : class, ICommand<TResult>;
    }

    public partial interface IPipe : ICommandExecutor
    {
    }

    internal partial class Pipe
    {
        public async Task ExecuteAsync(ICommand command)
        {
            var handler = _pipeHandlerResolver.Resolve(command.GetType()).FirstOrDefault() as IPipeAction;

            Func<Task<IPipeEcho>> next = async () =>
            {
                await handler.HandleAsync(command).ConfigureAwait(false);

                return null;
            };

            foreach (var middleware in _pipeHandlerResolver.ResolveMiddlewares(command.GetType()))
            {
                var prev = next;
                next = async () => await middleware.HandleAsync(command, prev).ConfigureAwait(false);
            }

            await next().ConfigureAwait(false);
        }

        public async Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command)
        {
            var handler = _pipeHandlerResolver.Resolve(command.GetType()).FirstOrDefault() as IPipeFunction<TResult>;

            Func<Task<IPipeEcho>> next = async () =>
            {
                var res = await handler.HandleAsync(command).ConfigureAwait(false);

                return new PipeEcho<TResult>(res);
            };

            foreach (var middleware in _pipeHandlerResolver.ResolveMiddlewares(command.GetType()))
            {
                var prev = next;
                next = () => middleware.HandleAsync(command, prev);
            }

            var result = await next().ConfigureAwait(false);

            if (result is PipeEcho<TResult> typedResult)
            {
                return typedResult.Result;
            }

            throw new InvalidOperationException($"Unexpected result type: {result.GetType().FullName}, expecting: {typeof(TResult).FullName}");
        }

        public IDisposable HandleAsync<TCommand>(Func<TCommand, Task> handle)
            where TCommand : class, ICommand
        {
            var handler = new DelegateHandler<TCommand>(handle);

            _pipeHandlerResolver.Register(handler);

            return Disposable.Create(() => _pipeHandlerResolver.Remove(handler));
        }

        public IDisposable HandleAsync<TCommand, TResult>(Func<TCommand, Task<TResult>> handle)
            where TCommand : class, ICommand<TResult>
        {
            var handler = new DelegateHandler<TCommand, TResult>(handle);

            _pipeHandlerResolver.Register(handler);

            return Disposable.Create(() => _pipeHandlerResolver.Remove(handler));
        }
    }
}
