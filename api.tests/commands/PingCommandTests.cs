using ApiTests.Fakes;
using Boltzenberg.Functions.Commands.Telegram;
using Boltzenberg.Functions.Domain.Telegram;
using Xunit;

namespace ApiTests.Commands
{
    public class PingCommandTests
    {
        private static CommandContext MakeContext()
            => new CommandContext
            {
                ChatId = 1001,
                FromUserId = 5241310949,
                RawText = "/ping",
                CommandName = "/ping",
                Arg = null,
                Log = NoOpLogBuffer.Create()
            };

        [Fact]
        public async Task Execute_ReturnsPong()
        {
            var cmd = new PingCommand();
            var result = await cmd.ExecuteAsync(MakeContext());

            Assert.True(result.Success);
            Assert.Contains("Pong", result.Message);
        }

        [Fact]
        public void Name_IsSlashPing()
        {
            var cmd = new PingCommand();
            Assert.Equal("/ping", cmd.Name);
        }

        [Fact]
        public void RequiresAuthorization_IsFalse()
        {
            var cmd = new PingCommand();
            Assert.False(cmd.RequiresAuthorization);
        }
    }
}
