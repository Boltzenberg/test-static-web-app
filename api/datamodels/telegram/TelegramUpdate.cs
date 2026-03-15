namespace Boltzenberg.Functions.DataModels.Telegram
{
    public class TelegramUpdate
    {
        public TelegramMessage? Message { get; set; }
    }

    public class TelegramMessage
    {
        public long MessageId { get; set; }
        public TelegramChat? Chat { get; set; }
        public TelegramUser? From { get; set; }
        public string? Text { get; set; }
    }

    public class TelegramChat
    {
        public long Id { get; set; }
    }

    public class TelegramUser
    {
        public long Id { get; set; }
        public string? Username { get; set; }
    }
}