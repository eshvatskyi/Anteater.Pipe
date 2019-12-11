namespace Anteater.Pipe.Tests
{
    using Microsoft.Extensions.DependencyInjection;

    public abstract class BasePipeTests
    {
        protected IServiceCollection Services { get; } = new ServiceCollection();

        public BasePipeTests()
        {
            Services.AddAnteaterPipe();
        }
    }
}
