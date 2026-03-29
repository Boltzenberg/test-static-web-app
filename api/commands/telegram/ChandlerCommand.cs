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
            string arg = context.Arg ?? string.Empty;
            if (string.IsNullOrEmpty(arg))
            {
                return CommandResult.Fail("You need to specify a caption for the meme!");
            }

            int delimiterIndex = arg.IndexOf('|');
            string top = (delimiterIndex == -1) ? arg : arg.Substring(0, delimiterIndex - 1).Trim();
            string bottom = (delimiterIndex == -1) ? string.Empty : arg.Substring(delimiterIndex + 1).Trim();
            string url = await ImgFlip.ChandlerizeUrlAsync(top, bottom, context.Log);
            return CommandResult.OkPhoto(url);
        }
    }
}
