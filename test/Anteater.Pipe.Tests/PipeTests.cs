namespace Anteater.Pipe.Tests
{
    using System;
    using System.Threading.Tasks;
    using Anteater.Pipe.Commands;
    using Anteater.Pipe.Events;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
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
            var projectileMiddleWare = new Mock<IPipeMiddleware<IPipeProjectile>>();
            projectileMiddleWare
                .Setup(x => x.HandleAsync(It.IsAny<IPipeProjectile>(), It.IsAny<Func<Task<IPipeEcho>>>()))
                .Returns<IPipeProjectile, Func<Task<IPipeEcho>>>(async (p, next) => await next());
            _services.AddScoped(_ => projectileMiddleWare.Object);

            var commandMiddleWare = new Mock<IPipeMiddleware<ICommand>>();
            commandMiddleWare
                .Setup(x => x.HandleAsync(It.IsAny<ICommand>(), It.IsAny<Func<Task<IPipeEcho>>>()))
                .Returns<IPipeProjectile, Func<Task<IPipeEcho>>>(async (p, next) => await next());
            _services.AddScoped(_ => commandMiddleWare.Object);

            var testCommandMiddleWare = new Mock<IPipeMiddleware<TestCommand>>();
            testCommandMiddleWare
                .Setup(x => x.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<Func<Task<IPipeEcho>>>()))
                .Returns<IPipeProjectile, Func<Task<IPipeEcho>>>(async (p, next) => await next());
            _services.AddScoped(_ => testCommandMiddleWare.Object);

            var commandHandler = new Mock<PipeHandler<TestCommand>>();
            commandHandler
                .Setup(x => x.HandleAsync(It.IsAny<TestCommand>()))
                .Returns(Task.CompletedTask);
            _services.AddScoped<IPipeHandler<TestCommand>>(_ => commandHandler.Object);

            var serviceProvider = _services.BuildServiceProvider();

            var bus = serviceProvider.GetRequiredService<IPipe>();

            var command = new TestCommand();

            await bus.ExecuteAsync(command);

            projectileMiddleWare.Verify(x => x.HandleAsync(It.IsAny<ICommand>(), It.IsAny<Func<Task<IPipeEcho>>>()), Times.Once);
            commandMiddleWare.Verify(x => x.HandleAsync(It.IsAny<ICommand>(), It.IsAny<Func<Task<IPipeEcho>>>()), Times.Once);
            testCommandMiddleWare.Verify(x => x.HandleAsync(It.IsAny<ICommand>(), It.IsAny<Func<Task<IPipeEcho>>>()), Times.Once);
            commandHandler.Verify(x => x.HandleAsync(It.IsAny<TestCommand>()), Times.Once);
        }

        [Fact]
        public async Task ExecutingCommandWithResultTest()
        {
            var projectileMiddleWare = new Mock<IPipeMiddleware<IPipeProjectile>>();
            projectileMiddleWare
                .Setup(x => x.HandleAsync(It.IsAny<IPipeProjectile>(), It.IsAny<Func<Task<IPipeEcho>>>()))
                .Returns<IPipeProjectile, Func<Task<IPipeEcho>>>(async (p, next) => await next());
            _services.AddScoped(_ => projectileMiddleWare.Object);

            var commandMiddleWare = new Mock<IPipeMiddleware<ICommand>>();
            commandMiddleWare
                .Setup(x => x.HandleAsync(It.IsAny<ICommand>(), It.IsAny<Func<Task<IPipeEcho>>>()))
                .Returns<IPipeProjectile, Func<Task<IPipeEcho>>>(async (p, next) => await next());
            _services.AddScoped(_ => commandMiddleWare.Object);

            var testCommandMiddleWare = new Mock<IPipeMiddleware<TestCommandWithResult>>();
            testCommandMiddleWare
                .Setup(x => x.HandleAsync(It.IsAny<TestCommandWithResult>(), It.IsAny<Func<Task<IPipeEcho>>>()))
                .Returns<IPipeProjectile, Func<Task<IPipeEcho>>>(async (p, next) =>
                {
                    var responseValue = await next();
                    if (responseValue is IPipeEcho<int> value)
                    {
                        return new PipeEcho<int>(value.Result + 1);
                    }

                    return responseValue;
                });
            _services.AddScoped(_ => testCommandMiddleWare.Object);

            var commandHandler = new Mock<PipeHandler<TestCommandWithResult, int>>();
            commandHandler
                .Setup(x => x.HandleAsync(It.IsAny<TestCommandWithResult>()))
                .Returns<TestCommandWithResult>(c => Task.FromResult(c.Input + 1));
            _services.AddScoped<IPipeHandler<TestCommandWithResult>>(_ => commandHandler.Object);

            var serviceProvider = _services.BuildServiceProvider();

            var bus = serviceProvider.GetRequiredService<IPipe>();

            var command = new TestCommandWithResult
            {
                Input = 5
            };

            var result = await bus.ExecuteAsync(command);

            projectileMiddleWare.Verify(x => x.HandleAsync(It.IsAny<ICommand>(), It.IsAny<Func<Task<IPipeEcho>>>()), Times.Once);
            commandMiddleWare.Verify(x => x.HandleAsync(It.IsAny<ICommand>(), It.IsAny<Func<Task<IPipeEcho>>>()), Times.Once);
            testCommandMiddleWare.Verify(x => x.HandleAsync(It.IsAny<ICommand>(), It.IsAny<Func<Task<IPipeEcho>>>()), Times.Once);
            commandHandler.Verify(x => x.HandleAsync(It.IsAny<TestCommandWithResult>()), Times.Once);

            Assert.Equal(command.Input + 2, result);
        }

        [Fact]
        public async Task PublishEventTest()
        {
            var eventHandler = new Mock<PipeHandler<TestEvent>>();
            eventHandler.Setup(x => x.HandleAsync(It.IsAny<TestEvent>()));

            _services.AddScoped<IPipeHandler<TestEvent>>(_ => eventHandler.Object);

            var serviceProvider = _services.BuildServiceProvider();

            var @event = new TestEvent();

            var bus = serviceProvider.GetRequiredService<IEventPublisher>();

            bus.PublishAsync(@event);

            await Task.Delay(1000);

            eventHandler.Verify(x => x.HandleAsync(@event), Times.Once);
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
