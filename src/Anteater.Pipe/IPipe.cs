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
        private readonly Func<IServiceProvider> _serviceProviderFactory;

        public Pipe(Func<IServiceProvider> serviceProviderFactory)
        {
            _serviceProviderFactory = serviceProviderFactory ?? throw new ArgumentNullException(nameof(serviceProviderFactory));
        }

        public async Task ExecuteAsync(ICommand command)
        {
            var services = GetServiceProvider();

            var type = typeof(IPipeHandler<>).MakeGenericType(command.GetType());
            var handler = (IPipeAction)services.GetRequiredService(type);

            Func<Task<IPipeEcho>> next = async () =>
            {
                await handler.HandleAsync(command).ConfigureAwait(false);
                return null;
            };

            foreach (var middleware in services.GetMiddlewares(command.GetType()))
            {
                var prev = next;
                next = () => middleware.HandleAsync(command, prev);
            }

            await next().ConfigureAwait(false);
        }

        public async Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command)
        {
            var services = GetServiceProvider();

            var type = typeof(IPipeHandler<>).MakeGenericType(command.GetType());
            var handler = (IPipeFunction<TResult>)services.GetRequiredService(type);

            Func<Task<IPipeEcho>> next = async () =>
            {
                var res = await handler.HandleAsync(command).ConfigureAwait(false);
                return new PipeEcho<TResult>(res);
            };

            foreach (var middleware in services.GetMiddlewares(command.GetType()))
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

        public void PublishAsync<TEvent>(TEvent @event)
            where TEvent : class, IEvent
        {
            var services = GetServiceProvider();

            var handlers = services.GetServices<IPipeHandler<TEvent>>().OfType<IPipeAction>().ToList();

            Func<Task<IPipeEcho>> next = async () =>
            {
                await Task.WhenAll(handlers.Select(x => x.HandleAsync(@event))).ConfigureAwait(false);
                return null;
            };

            foreach (var middleware in services.GetMiddlewares<TEvent>())
            {
                var prev = next;
                next = () => middleware.HandleAsync(@event, prev);
            }

            _ = next();
        }

        private IServiceProvider GetServiceProvider()
        {
            return _serviceProviderFactory.Invoke();
        }
    }
}
