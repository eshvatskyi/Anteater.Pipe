namespace Anteater.Pipe.Events
{
    public interface IEventPublisher
    {
        void PublishAsync<TEvent>(TEvent @event)
            where TEvent : class, IEvent;
    }
}
