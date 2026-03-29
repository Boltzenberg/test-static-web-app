using Boltzenberg.Functions.DataModels.Telegram;
using Boltzenberg.Functions.Domain.Telegram;
using Boltzenberg.Functions.Logging;
using TelegramComms = Boltzenberg.Functions.Comms.Telegram;

namespace Boltzenberg.Functions.Commands.Telegram
{
    public class CommandDispatcher
    {
        private readonly IReadOnlyList<ICommand> _commands;
        private static readonly HashSet<long> _authorizedUserIds = new() { 5241310949, 5411752675 };

        public CommandDispatcher(IEnumerable<ICommand> commands)
        {
            _commands = commands.ToList();
        }

        public async Task<string> DispatchAsync(TelegramUpdate update, LogBuffer log)
        {
            var msg = update.Message;
            if (msg == null)
            {
                return "unknown";
            }

            var chatId = msg.Chat?.Id ?? 0;
            var fromId = msg.From?.Id ?? 0;
            var text = msg.Text?.Trim() ?? string.Empty;

            var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts.Length > 0 ? parts[0].ToLower() : string.Empty;
            var arg = parts.Length > 1 ? parts[1].Trim() : null;

            log.Info("Received command '{0}' from user {1}", commandName, fromId);

            var command = _commands.FirstOrDefault(c => c.Name == commandName);
            if (command == null)
            {
                log.Info("Unknown command: {0}", commandName);
                await TelegramComms.SendAsync(chatId.ToString(), "🤖 Unknown command");
                return "unknown";
            }

            if (command.RequiresAuthorization && !_authorizedUserIds.Contains(fromId))
            {
                log.Error("Unauthorized user {0} attempted '{1}'", fromId, commandName);
                await TelegramComms.SendAsync(chatId.ToString(), "❌ Unauthorized");
                return "unauthorized";
            }

            var context = new CommandContext
            {
                ChatId = chatId,
                FromUserId = fromId,
                Username = msg.From?.Username,
                RawText = text,
                CommandName = commandName,
                Arg = arg,
                Log = log
            };

            var result = await command.ExecuteAsync(context);
            if (!result.Success)
            {
                await TelegramComms.SendAsync(chatId.ToString(), result.Message);
                return "error";
            }
            else
            {
                if (!string.IsNullOrEmpty(result.Message))
                {
                    await TelegramComms.SendAsync(chatId.ToString(), result.Message);
                }
            
                if (!string.IsNullOrEmpty(result.PhotoUrl))
                {
                    await TelegramComms.SendPhotoAsync(chatId.ToString(), result.PhotoUrl);
                }

                return "ok";
            }
        }
    }
}
