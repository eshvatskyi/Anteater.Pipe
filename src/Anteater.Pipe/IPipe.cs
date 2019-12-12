namespace Anteater.Pipe
{
    using System;

    public partial interface IPipe
    {
    }

    internal partial class Pipe : IPipe
    {
        private readonly IPipeHandlerResolver _pipeHandlerResolver;

        public Pipe(IPipeHandlerResolver pipeHandlerResolver)
        {
            _pipeHandlerResolver = pipeHandlerResolver ?? throw new ArgumentNullException(nameof(pipeHandlerResolver));
        }
    }
}
