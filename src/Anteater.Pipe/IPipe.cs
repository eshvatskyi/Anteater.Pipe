namespace Anteater.Pipe
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Anteater.Pipe.Commands;
    using Anteater.Pipe.Events;
    using Microsoft.Extensions.DependencyInjection;

    public interface IPipe : ICommandExecutor, IEventPublisher
    {
    }

    internal class Pipe : IPipe
    {
        private readonly IServiceProvider _services;

        public Pipe(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public async Task ExecuteAsync(ICommand command)
        {
            var type = typeof(IPipeHandler<>).MakeGenericType(command.GetType());
            var handler = (IPipeAction)_services.GetRequiredService(type);

            Func<Task<IPipeEcho>> next = async () =>
            {
                await handler.HandleAsync(command);
                return null;
            };

            foreach (var middleware in _services.GetMiddlewares(command.GetType()))
            {
                var prev = next;
                next = async () => await middleware.HandleAsync(command, prev);
            }

            await next();
        }

        public async Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command)
        {
            var type = typeof(IPipeHandler<>).MakeGenericType(command.GetType());
            var handler = (IPipeFunction<TResult>)_services.GetRequiredService(type);

            Func<Task<IPipeEcho>> next = async () =>
            {
                var res = await handler.HandleAsync(command);
                return new PipeEcho<TResult>(res);
            };

            foreach (var middleware in _services.GetMiddlewares(command.GetType()))
            {
                var prev = next;
                next = async () => await middleware.HandleAsync(command, prev);
            }

            var result = await next();

            if (result is PipeEcho<TResult> typedResult)
            {
                return typedResult.Result;
            }

            throw new InvalidOperationException($"Unexpected result type: {result.GetType().FullName}, expecting: {typeof(TResult).FullName}");
        }

        public void PublishAsync<TEvent>(TEvent @event)
            where TEvent : class, IEvent
        {
            var handlers = _services.GetServices<IPipeHandler<TEvent>>().OfType<IPipeAction>().ToList();

            Func<Task<IPipeEcho>> next = async () =>
            {
                await Task.WhenAll(handlers.Select(x => x.HandleAsync(@event)));
                return null;
            };

            foreach (var middleware in _services.GetMiddlewares<TEvent>())
            {
                var prev = next;
                next = async () => await middleware.HandleAsync(@event, prev);
            }

            Task.Run(() => next());
        }
    }
}
