namespace Anteater.Pipe
{
    using System;

    internal class Disposable : IDisposable
    {
        private readonly Action _action;

        private bool _disposed;

        private Disposable(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public static IDisposable Create(Action action)
        {
            return new Disposable(action);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _action();

                _disposed = true;
            }
        }
    }
}
