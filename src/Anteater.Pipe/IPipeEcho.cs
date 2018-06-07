namespace Anteater.Pipe
{
    public interface IPipeEcho
    {
    }

    public interface IPipeEcho<out TResult> : IPipeEcho
    {
        TResult Result { get; }
    }

    internal class PipeEcho<TResult> : IPipeEcho<TResult>
    {
        public PipeEcho(TResult result)
        {
            Result = result;
        }

        public TResult Result { get; }
    }
}
