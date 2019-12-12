namespace Anteater.Pipe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;

    internal interface IPipeHandlerResolver
    {
        void Register(IPipeHandler handler);

        void Remove(IPipeHandler handler);

        IEnumerable<IPipeHandler> Resolve(Type pipeProjectileType);

        IEnumerable<IPipeMiddleware> ResolveMiddlewares(Type pipeProjectileType);
    }

    public interface IPipeHandlerProvider
    {
        IDisposable UseServiceProvider(IServiceProvider serviceProvider);
    }

    internal class DefaultPipeHandlerResolver : IPipeHandlerResolver, IPipeHandlerProvider
    {
        private readonly IServiceProvider _defaultServiceProvider;

        private readonly AsyncLocal<IServiceProvider> _currentServiceProvider = new AsyncLocal<IServiceProvider>();

        private readonly List<IPipeHandler> _handlers = new List<IPipeHandler>();

        public DefaultPipeHandlerResolver(IServiceProvider serviceProvider)
        {
            _defaultServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Register(IPipeHandler handler)
        {
            _handlers.Add(handler);
        }

        public void Remove(IPipeHandler handler)
        {
            _handlers.Remove(handler);
        }

        public IEnumerable<IPipeHandler> Resolve(Type pipeProjectileType)
        {
            var serviceProvider = _currentServiceProvider.Value ?? _defaultServiceProvider;

            var type = typeof(IPipeHandler<>).MakeGenericType(pipeProjectileType);

            return _handlers.Where(x => type.IsInstanceOfType(x))
                .Concat(serviceProvider.GetServices(type).Cast<IPipeHandler>());
        }

        public IEnumerable<IPipeMiddleware> ResolveMiddlewares(Type pipeProjectileType)
        {
            var serviceProvider = _currentServiceProvider.Value ?? _defaultServiceProvider;

            return serviceProvider.GetMiddlewares(pipeProjectileType);
        }

        public IDisposable UseServiceProvider(IServiceProvider serviceProvider)
        {
            _currentServiceProvider.Value = serviceProvider;

            return Disposable.Create(() => _currentServiceProvider.Value = null);
        }
    }
}
