namespace Anteater.Pipe.Commands
{
    public interface ICommand : IPipeProjectile
    {
    }

    public interface ICommand<out TResult> : ICommand
    {
    }
}
