using ApiTests.Fakes;
using Boltzenberg.Functions.Commands.Telegram;
using Boltzenberg.Functions.Domain.Telegram;
using Boltzenberg.Functions.Storage.Documents;
using Xunit;

namespace ApiTests.Commands
{
    public class ListCommandTests
    {
        private static GroceryListDocument MakeDoc(params string[] items)
            => new GroceryListDocument
            {
                AppId = GroceryListDocument.PartitionKey,
                id = "Test",
                _etag = "etag-1",
                Items = items.ToList()
            };

        private static CommandContext MakeContext(FakeGroceryListStore store)
            => new CommandContext
            {
                ChatId = 1001,
                FromUserId = 5241310949,
                RawText = "/list",
                CommandName = "/list",
                Arg = null,
                Log = NoOpLogBuffer.Create()
            };

        [Fact]
        public async Task Execute_ReturnsCurrentList()
        {
            var store = new FakeGroceryListStore(MakeDoc("Apples", "Bananas", "Cherries"));
            var cmd = new ListCommand(store);
            var ctx = MakeContext(store);

            var result = await cmd.ExecuteAsync(ctx);

            Assert.True(result.Success);
            Assert.Contains("Apples", result.Message);
            Assert.Contains("Bananas", result.Message);
            Assert.Contains("Cherries", result.Message);
        }

        [Fact]
        public async Task Execute_ReturnsErrorWhenStorageFails()
        {
            var store = new FakeGroceryListStore(failOnRead: true);
            var cmd = new ListCommand(store);
            var ctx = MakeContext(store);

            var result = await cmd.ExecuteAsync(ctx);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Execute_ReturnsEmptyListWhenNoItems()
        {
            var store = new FakeGroceryListStore(MakeDoc());
            var cmd = new ListCommand(store);
            var ctx = MakeContext(store);

            var result = await cmd.ExecuteAsync(ctx);

            Assert.True(result.Success);
            // Message should just contain the header emoji
            Assert.Contains("🟢", result.Message);
        }

        [Fact]
        public void Name_IsSlashList()
        {
            var store = new FakeGroceryListStore(MakeDoc());
            var cmd = new ListCommand(store);
            Assert.Equal("/list", cmd.Name);
        }

        [Fact]
        public void RequiresAuthorization_IsTrue()
        {
            var store = new FakeGroceryListStore(MakeDoc());
            var cmd = new ListCommand(store);
            Assert.True(cmd.RequiresAuthorization);
        }
    }
}
