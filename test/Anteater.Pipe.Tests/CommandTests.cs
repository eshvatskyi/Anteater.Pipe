namespace Anteater.Pipe.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NSubstitute;
    using Xunit;

    public class CommandTests : BasePipeTests
    {
        [Fact]
        public async Task Command_WithMiddleware_Executed_Successfully()
        {
            var projectileMiddleWare = Substitute.For<IPipeMiddleware<IPipeProjectile>>();
            projectileMiddleWare
                .HandleAsync(Arg.Any<IPipeProjectile>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c => await c.Arg<Func<Task<IPipeEcho>>>()());
            Services.AddScoped(_ => projectileMiddleWare);

            var commandMiddleWare = Substitute.For<IPipeMiddleware<ICommand>>();
            commandMiddleWare
                .HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c => await c.Arg<Func<Task<IPipeEcho>>>()());
            Services.AddScoped(_ => commandMiddleWare);

            var testCommandMiddleWare = Substitute.For<IPipeMiddleware<TestCommand>>();
            testCommandMiddleWare
                .HandleAsync(Arg.Any<TestCommand>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c => await c.Arg<Func<Task<IPipeEcho>>>()());
            Services.AddScoped(_ => testCommandMiddleWare);

            var commandHandler = Substitute.For<PipeHandler<TestCommand>>();
            commandHandler
                .HandleAsync(Arg.Any<TestCommand>())
                .Returns(Task.CompletedTask);
            Services.AddScoped<IPipeHandler<TestCommand>>(_ => commandHandler);

            var serviceProvider = Services.BuildServiceProvider();

            var bus = serviceProvider.GetRequiredService<IPipe>();

            var command = new TestCommand();

            await bus.ExecuteAsync(command);

            await projectileMiddleWare.Received(1).HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>());
            await commandMiddleWare.Received(1).HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>());
            await testCommandMiddleWare.Received(1).HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>());
            await commandHandler.Received(1).HandleAsync(Arg.Any<TestCommand>());
        }

        [Fact]
        public async Task Command_WithMiddleware_ExecutedAndReturn_Successfully()
        {
            var projectileMiddleWare = Substitute.For<IPipeMiddleware<IPipeProjectile>>();
            projectileMiddleWare
                .HandleAsync(Arg.Any<IPipeProjectile>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c => await c.Arg<Func<Task<IPipeEcho>>>()());
            Services.AddScoped(_ => projectileMiddleWare);

            var commandMiddleWare = Substitute.For<IPipeMiddleware<ICommand>>();
            commandMiddleWare
                .HandleAsync(Arg.Any<ICommand>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c => await c.Arg<Func<Task<IPipeEcho>>>()());
            Services.AddScoped(_ => commandMiddleWare);

            var testCommandMiddleWare = Substitute.For<IPipeMiddleware<TestCommandWithResult>>();
            testCommandMiddleWare
                .HandleAsync(Arg.Any<TestCommandWithResult>(), Arg.Any<Func<Task<IPipeEcho>>>())
                .Returns(async c =>
                {
                    var responseValue = await c.Arg<Func<Task<IPipeEcho>>>()();
                    if (responseValue is IPipeEcho<int> value)
                    {
                        return new PipeEcho<int>(value.Result + 1);
                    }

                    return responseValue;
                });
            Services.AddScoped(_ => testCommandMiddleWare);

            var commandHandler = Substitute.For<PipeHandler<TestCommandWithResult, int>>();
            commandHandler
                .HandleAsync(Arg.Any<TestCommandWithResult>())
                .Returns(c => Task.FromResult(c.Arg<TestCommandWithResult>().Input + 1));
            Services.AddScoped<IPipeHandler<TestCommandWithResult>>(_ => commandHandler);

            var serviceProvider = Services.BuildServiceProvider();

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
        public async Task Command_HandledInline_Successfully()
        {
            var tcs = new TaskCompletionSource<ICommand>();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            var serviceProvider = Services.BuildServiceProvider();

            var bus = serviceProvider.GetRequiredService<IPipe>();

            var command = new TestCommand();

            bus.HandleAsync<TestCommand>(x =>
            {
                tcs.TrySetResult(x);
                return Task.CompletedTask;
            });

            using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                await bus.ExecuteAsync(command);

                Assert.Equal(command, await tcs.Task);
            }
        }

        [Fact]
        public async Task Command_HandledInlineAndReturn_Successfully()
        {
            var tcs = new TaskCompletionSource<ICommand>();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            var serviceProvider = Services.BuildServiceProvider();

            var bus = serviceProvider.GetRequiredService<IPipe>();

            var command = new TestCommandWithResult
            {
                Input = 5
            };

            bus.HandleAsync<TestCommandWithResult, int>(x =>
            {
                tcs.TrySetResult(x);
                return Task.FromResult(command.Input + 1);
            });

            using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                await bus.ExecuteAsync(command);

                Assert.Equal(command, await tcs.Task);
            }

            var result = await bus.ExecuteAsync(command);

            Assert.Equal(command, await tcs.Task);
            Assert.Equal(command.Input + 1, result);
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
    }
}
