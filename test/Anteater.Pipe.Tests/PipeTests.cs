namespace Anteater.Pipe.Tests
{
    using System;
    using System.Threading.Tasks;
    using Anteater.Pipe.Commands;
    using Anteater.Pipe.Events;
    using Microsoft.Extensions.DependencyInjection;
    using NSubstitute;
    using Xunit;

    public class PipeTests
    {
        private readonly IServiceCollection _services = new ServiceCollection();

        public PipeTests()
        {
            _services.AddAnteaterPipe();
        }

        [Fact]
        public async Task ExecutingCommandTest()
        {
            var projectileMiddleWare = Substitute.For<IPipeMiddleware<IPipeProjectile>>();
            projectileMiddleWare.HandleAsync(Arg.Any<IPipeProjectile>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c => await c.Arg<Func<Task<IPipeEcho>>>()());
            _services.AddScoped(_ => projectileMiddleWare);

            var commandMiddleWare = Substitute.For<IPipeMiddleware<ICommand>>();
            commandMiddleWare.HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c => await c.Arg<Func<Task<IPipeEcho>>>()());
            _services.AddScoped(_ => commandMiddleWare);

            var testCommandMiddleWare = Substitute.For<IPipeMiddleware<TestCommand>>();
            testCommandMiddleWare.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c => await c.Arg<Func<Task<IPipeEcho>>>()());
            _services.AddScoped(_ => testCommandMiddleWare);

            var commandHandler = Substitute.For<PipeHandler<TestCommand>>();
            commandHandler.HandleAsync(Arg.Any<TestCommand>())
                .Returns(Task.CompletedTask);
            _services.AddScoped<IPipeHandler<TestCommand>>(_ => commandHandler);

            var serviceProvider = _services.BuildServiceProvider();

            var bus = serviceProvider.GetRequiredService<IPipe>();

            var command = new TestCommand();

            await bus.ExecuteAsync(command);

            await projectileMiddleWare.Received(1).HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>());
            await commandMiddleWare.Received(1).HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>());
            await testCommandMiddleWare.Received(1).HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>());
            await commandHandler.Received(1).HandleAsync(Arg.Any<TestCommand>());
        }

        [Fact]
        public async Task ExecutingCommandWithResultTest()
        {
            var projectileMiddleWare = Substitute.For<IPipeMiddleware<IPipeProjectile>>();
            projectileMiddleWare.HandleAsync(Arg.Any<IPipeProjectile>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c => await c.Arg<Func<Task<IPipeEcho>>>()());
            _services.AddScoped(_ => projectileMiddleWare);

            var commandMiddleWare = Substitute.For<IPipeMiddleware<ICommand>>();
            commandMiddleWare.HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c => await c.Arg<Func<Task<IPipeEcho>>>()());
            _services.AddScoped(_ => commandMiddleWare);

            var testCommandMiddleWare = Substitute.For<IPipeMiddleware<TestCommandWithResult>>();
            testCommandMiddleWare.HandleAsync(Arg.Any<TestCommandWithResult>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c =>
                {
                    var responseValue = await c.Arg<Func<Task<IPipeEcho>>>()();
                    if (responseValue is IPipeEcho<int> value)
                    {
                        return new PipeEcho<int>(value.Result + 1);
                    }

                    return responseValue;
                });
            _services.AddScoped(_ => testCommandMiddleWare);

            var commandHandler = Substitute.For<PipeHandler<TestCommandWithResult, int>>();
            commandHandler.HandleAsync(Arg.Any<TestCommandWithResult>())
                .Returns(c => Task.FromResult(c.Arg<TestCommandWithResult>().Input + 1));
            _services.AddScoped<IPipeHandler<TestCommandWithResult>>(_ => commandHandler);

            var serviceProvider = _services.BuildServiceProvider();

            var bus = serviceProvider.GetRequiredService<IPipe>();

            var command = new TestCommandWithResult
            {
                Input = 5
            };

            var result = await bus.ExecuteAsync(command);

            await projectileMiddleWare.Received(1).HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>());
            await commandMiddleWare.Received(1).HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>());
            await testCommandMiddleWare.Received(1).HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>());
            await commandHandler.Received(1).HandleAsync(Arg.Any<TestCommandWithResult>());

            Assert.Equal(command.Input + 2, result);
        }

        [Fact]
        public async Task PublishEventTest()
        {
            var eventHandler = Substitute.For<PipeHandler<TestEvent>>();

            _services.AddScoped<IPipeHandler<TestEvent>>(_ => eventHandler);

            var serviceProvider = _services.BuildServiceProvider();

            var @event = new TestEvent();

            var bus = serviceProvider.GetRequiredService<IEventPublisher>();

            bus.PublishAsync(@event);

            await Task.Delay(1000);

            await eventHandler.Received(1).HandleAsync(@event);
        }

        public class TestCommand : ICommand
        {
        }

        public class TestCommandWithResult : ICommand<int>
        {
            public int Input { get; set; }
        }

        public class TestCommandWithResultHandler : PipeHandler<TestCommandWithResult, int>
        {
            public override Task<int> HandleAsync(TestCommandWithResult projectile)
            {
                throw new NotImplementedException();
            }
        }

        public class TestEvent : IEvent
        {
        }
    }
}
