namespace Boltzenberg.Functions.Domain.Telegram
{
    public class CommandResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = "";
        public string PhotoUrl { get; init; } = "";

        public static CommandResult OkMessage(string message) => new() { Success = true, Message = message };
        public static CommandResult OkPhoto(string photoUrl) => new() { Success = true, PhotoUrl = photoUrl };
        public static CommandResult Fail(string message) => new() { Success = false, Message = message };
    }
}
