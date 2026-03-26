namespace Boltzenberg.Functions.Domain.Telegram
{
    public interface ICommand
    {
        string Name { get; }
        bool RequiresAuthorization { get; }
        Task<CommandResult> ExecuteAsync(CommandContext context);
    }
}
