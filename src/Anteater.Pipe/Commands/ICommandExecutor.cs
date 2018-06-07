namespace Anteater.Pipe.Commands
{
    using System.Threading.Tasks;

    public interface ICommandExecutor
    {
        Task ExecuteAsync(ICommand command);

        Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command);
    }
}
