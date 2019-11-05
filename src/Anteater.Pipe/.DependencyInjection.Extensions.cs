namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using Anteater.Pipe;
    using Anteater.Pipe.Commands;
    using Anteater.Pipe.Events;

    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddAnteaterPipe(this IServiceCollection services, Func<IServiceProvider, IServiceProvider> serviceProviderFactory = null)
        {
            Pipe CreatePipe(IServiceProvider serviceProvider)
            {
                return new Pipe(() => (serviceProviderFactory ?? ((sp) => sp))(serviceProvider));
            }

            services.AddSingleton<ICommandExecutor, Pipe>(CreatePipe);
            services.AddSingleton<IEventPublisher, Pipe>(CreatePipe);
            services.AddSingleton<IPipe, Pipe>(CreatePipe);

            return services;
        }
    }
}
