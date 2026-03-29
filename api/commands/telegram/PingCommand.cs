using Boltzenberg.Functions.Domain.Telegram;

namespace Boltzenberg.Functions.Commands.Telegram
{
    public class PingCommand : ICommand
    {
        public string Name => "/ping";
        public bool RequiresAuthorization => false;

        public Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            return Task.FromResult(CommandResult.OkMessage("🟢 Pong"));
        }
    }
}
