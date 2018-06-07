namespace Microsoft.Extensions.DependencyInjection
{
    using Anteater.Pipe;
    using Anteater.Pipe.Commands;
    using Anteater.Pipe.Events;

    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddAnteaterPipe(this IServiceCollection services)
        {
            services.AddScoped<ICommandExecutor, Pipe>();
            services.AddScoped<IEventPublisher, Pipe>();
            services.AddScoped<IPipe, Pipe>();

            return services;
        }
    }
}
