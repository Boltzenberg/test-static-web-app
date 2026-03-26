using Boltzenberg.Functions.Logging;

namespace Boltzenberg.Functions.Domain.Telegram
{
    public class CommandContext
    {
        public long ChatId { get; init; }
        public long FromUserId { get; init; }
        public string? Username { get; init; }
        public string RawText { get; init; } = "";
        public string CommandName { get; init; } = "";
        public string? Arg { get; init; }
        public LogBuffer Log { get; init; } = null!;
    }
}
