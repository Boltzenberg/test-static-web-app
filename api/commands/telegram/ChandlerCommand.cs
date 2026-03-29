using Boltzenberg.Functions.Comms;
using Boltzenberg.Functions.Domain.Telegram;

namespace Boltzenberg.Functions.Commands.Telegram
{
    public class ChandlerCommand : ICommand
    {
        public string Name => "/chandler";
        public bool RequiresAuthorization => false;

        public async Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            string url = await ImgFlip.ChandlerizeUrlAsync(context.Arg ?? string.Empty, string.Empty, context.Log);
            return CommandResult.OkPhoto(url);
        }
    }
}
