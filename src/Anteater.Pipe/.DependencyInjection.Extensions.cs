namespace Microsoft.Extensions.DependencyInjection
{
    using Anteater.Pipe;

    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddAnteaterPipe(this IServiceCollection services)
        {
            services.AddSingleton<DefaultPipeHandlerResolver>();
            services.AddSingleton<IPipeHandlerResolver, DefaultPipeHandlerResolver>(sp => sp.GetRequiredService<DefaultPipeHandlerResolver>());
            services.AddSingleton<IPipeHandlerProvider, DefaultPipeHandlerResolver>(sp => sp.GetRequiredService<DefaultPipeHandlerResolver>());

            services.AddSingleton<Pipe>();
            services.AddSingleton<IPipe, Pipe>(sp => sp.GetRequiredService<Pipe>());
            services.AddSingleton<IEventPublisher, Pipe>(sp => sp.GetRequiredService<Pipe>());
            services.AddSingleton<ICommandExecutor, Pipe>(sp => sp.GetRequiredService<Pipe>());

            return services;
        }
    }
}
