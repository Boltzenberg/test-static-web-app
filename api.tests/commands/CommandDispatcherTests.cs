using ApiTests.Fakes;
using Boltzenberg.Functions.Commands.Telegram;
using Boltzenberg.Functions.DataModels.Telegram;
using Boltzenberg.Functions.Domain.Telegram;
using Xunit;

namespace ApiTests.Commands
{
    /// <summary>
    /// Tests for CommandDispatcher routing and authorization.
    /// Telegram.SendAsync returns immediately when BOLTZENBERG_BOT_TOKEN is not set (test environment),
    /// so no real HTTP calls are made.
    /// </summary>
    public class CommandDispatcherTests
    {
        private static readonly long AuthorizedUserId = 5241310949L;
        private static readonly long UnauthorizedUserId = 9999999999L;

        private static TelegramUpdate MakeUpdate(string text, long fromId = 5241310949L, long chatId = 1001L)
        {
            return new TelegramUpdate
            {
                Message = new TelegramMessage
                {
                    MessageId = 1,
                    Text = text,
                    Chat = new TelegramChat { Id = chatId },
                    From = new TelegramUser { Id = fromId, Username = "testuser" }
                }
            };
        }

        [Fact]
        public async Task Dispatch_RoutesToCorrectCommand()
        {
            var fakeCmd = new FakeCommand("/ping", false, CommandResult.Ok("pong"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = MakeUpdate("/ping");
            var result = await dispatcher.DispatchAsync(update, log);

            Assert.Equal("ok", result);
            Assert.Equal(1, fakeCmd.ExecuteCount);
        }

        [Fact]
        public async Task Dispatch_ReturnsUnknownForUnrecognizedCommand()
        {
            var fakeCmd = new FakeCommand("/ping", false, CommandResult.Ok("pong"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = MakeUpdate("/unknown");
            var result = await dispatcher.DispatchAsync(update, log);

            Assert.Equal("unknown", result);
            Assert.Equal(0, fakeCmd.ExecuteCount);
        }

        [Fact]
        public async Task Dispatch_RejectsUnauthorizedUserForRestrictedCommand()
        {
            var fakeCmd = new FakeCommand("/secret", true, CommandResult.Ok("secret data"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = MakeUpdate("/secret", fromId: UnauthorizedUserId);
            var result = await dispatcher.DispatchAsync(update, log);

            Assert.Equal("unauthorized", result);
            Assert.Equal(0, fakeCmd.ExecuteCount);
        }

        [Fact]
        public async Task Dispatch_AllowsUnauthorizedUserForOpenCommand()
        {
            var fakeCmd = new FakeCommand("/ping", false, CommandResult.Ok("pong"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = MakeUpdate("/ping", fromId: UnauthorizedUserId);
            var result = await dispatcher.DispatchAsync(update, log);

            Assert.Equal("ok", result);
            Assert.Equal(1, fakeCmd.ExecuteCount);
        }

        [Fact]
        public async Task Dispatch_AllowsAuthorizedUserForRestrictedCommand()
        {
            var fakeCmd = new FakeCommand("/add", true, CommandResult.Ok("added"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = MakeUpdate("/add milk", fromId: AuthorizedUserId);
            var result = await dispatcher.DispatchAsync(update, log);

            Assert.Equal("ok", result);
            Assert.Equal(1, fakeCmd.ExecuteCount);
        }

        [Fact]
        public async Task Dispatch_PassesMultiWordRemainderAsSingleArg()
        {
            var fakeCmd = new FakeCommand("/add", true, CommandResult.Ok("ok"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = MakeUpdate("/add peanut butter");
            await dispatcher.DispatchAsync(update, log);

            Assert.NotNull(fakeCmd.LastContext);
            Assert.Equal("peanut butter", fakeCmd.LastContext!.Arg);
        }

        [Fact]
        public async Task Dispatch_ParsesSingleWordArgCorrectly()
        {
            var fakeCmd = new FakeCommand("/add", true, CommandResult.Ok("ok"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = MakeUpdate("/add milk");
            await dispatcher.DispatchAsync(update, log);

            Assert.NotNull(fakeCmd.LastContext);
            Assert.Equal("milk", fakeCmd.LastContext!.Arg);
        }

        [Fact]
        public async Task Dispatch_SetsNullArgForBareCommand()
        {
            var fakeCmd = new FakeCommand("/ping", false, CommandResult.Ok("pong"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = MakeUpdate("/ping");
            await dispatcher.DispatchAsync(update, log);

            Assert.NotNull(fakeCmd.LastContext);
            Assert.Null(fakeCmd.LastContext!.Arg);
        }

        [Fact]
        public async Task Dispatch_PassesCommandNameLowercased()
        {
            var fakeCmd = new FakeCommand("/ping", false, CommandResult.Ok("pong"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = MakeUpdate("/PING"); // uppercase
            var result = await dispatcher.DispatchAsync(update, log);

            Assert.Equal("ok", result);
        }

        [Fact]
        public async Task Dispatch_ReturnsErrorWhenCommandFails()
        {
            var fakeCmd = new FakeCommand("/add", true, CommandResult.Fail("something broke"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = MakeUpdate("/add milk");
            var result = await dispatcher.DispatchAsync(update, log);

            Assert.Equal("error", result);
        }

        [Fact]
        public async Task Dispatch_HandlesNullMessage()
        {
            var fakeCmd = new FakeCommand("/ping", false, CommandResult.Ok("pong"));
            var dispatcher = new CommandDispatcher(new[] { fakeCmd });
            var log = NoOpLogBuffer.Create();

            var update = new TelegramUpdate { Message = null };
            var result = await dispatcher.DispatchAsync(update, log);

            Assert.Equal("unknown", result);
        }
    }
}
