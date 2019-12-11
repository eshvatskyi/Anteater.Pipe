namespace Anteater.Pipe
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public interface IEventPublisher
    {
        void Publish<TEvent>(TEvent @event)
            where TEvent : class, IEvent;

        IDisposable Subscribe<TEvent>(Func<TEvent, Task> handle)
            where TEvent : class, IEvent;
    }

    public partial interface IPipe : IEventPublisher
    {
    }

    internal partial class Pipe
    {
        public void Publish<TEvent>(TEvent @event)
            where TEvent : class, IEvent
        {
            var handlers = _pipeHandlerResolver.Resolve(@event.GetType()).OfType<IPipeAction>().ToArray();

            Func<Task<IPipeEcho>> next = async () =>
            {
                await Task.WhenAll(handlers.Select(x => x.HandleAsync(@event))).ConfigureAwait(false);

                return null;
            };

            foreach (var middleware in _pipeHandlerResolver.ResolveMiddlewares(@event.GetType()))
            {
                var prev = next;
                next = async () => await middleware.HandleAsync(@event, prev).ConfigureAwait(false);
            }

            _ = next();
        }

        public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handle)
            where TEvent : class, IEvent
        {
            var handler = new DelegateHandler<TEvent>(handle);

            _pipeHandlerResolver.Register(handler);

            return Disposable.Create(() => _pipeHandlerResolver.Remove(handler));
        }
    }
}
