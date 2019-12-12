namespace Anteater.Pipe
{
    public interface ICommand : IPipeProjectile
    {
    }

    public interface ICommand<out TResult> : ICommand
    {
    }
}
