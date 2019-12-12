namespace Anteater.Pipe.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NSubstitute;
    using Xunit;

    public class EventTests : BasePipeTests
    {
        [Fact]
        public async Task Event_Publish_Successfully()
        {
            var handler = Substitute.For<PipeHandler<TestEvent>>();

            Services.AddScoped<IPipeHandler<TestEvent>>(_ => handler);

            var serviceProvider = Services.BuildServiceProvider();

            var @event = new TestEvent();

            var bus = serviceProvider.GetRequiredService<IEventPublisher>();

            bus.Publish(@event);

            await Task.Delay(1000);

            await handler.Received(1).HandleAsync(@event);
        }

        [Fact]
        public async Task Event_Subscribe_Successfully()
        {
            var @event = new TestEvent();

            var tcs = new TaskCompletionSource<IEvent>();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            var serviceProvider = Services.BuildServiceProvider();

            var bus = serviceProvider.GetRequiredService<IEventPublisher>();

            bus.Subscribe<TestEvent>(x =>
            {
                tcs.TrySetResult(x);
                return Task.CompletedTask;
            });

            using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                bus.Publish(@event);

                Assert.Equal(@event, await tcs.Task);
            }
        }

        public class TestEvent : IEvent
        {
        }
    }
}
