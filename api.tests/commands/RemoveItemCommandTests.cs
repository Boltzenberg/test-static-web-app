using ApiTests.Fakes;
using Boltzenberg.Functions.Commands.Telegram;
using Boltzenberg.Functions.Domain.Telegram;
using Boltzenberg.Functions.Storage.Documents;
using Xunit;

namespace ApiTests.Commands
{
    public class RemoveItemCommandTests
    {
        private static GroceryListDocument MakeDoc(params string[] items)
            => new GroceryListDocument
            {
                AppId = GroceryListDocument.PartitionKey,
                id = "Test",
                _etag = "etag-1",
                Items = items.ToList()
            };

        private static CommandContext MakeContext(FakeGroceryListStore store, string? arg = null)
            => new CommandContext
            {
                ChatId = 1001,
                FromUserId = 5241310949,
                RawText = arg != null ? $"/remove {arg}" : "/remove",
                CommandName = "/remove",
                Arg = arg,
                Log = NoOpLogBuffer.Create()
            };

        [Fact]
        public async Task Execute_RemovesItemFromList()
        {
            var store = new FakeGroceryListStore(MakeDoc("Apples", "Bananas", "Cherries"));
            var cmd = new RemoveItemCommand(store);
            var ctx = MakeContext(store, "Bananas");

            var result = await cmd.ExecuteAsync(ctx);

            Assert.True(result.Success);
            var doc = store.GetCurrentDoc();
            Assert.NotNull(doc);
            Assert.DoesNotContain("Bananas", doc!.Items);
            Assert.Contains("Apples", doc.Items);
        }

        [Fact]
        public async Task Execute_IsCaseInsensitiveOnRemove()
        {
            var store = new FakeGroceryListStore(MakeDoc("Apples", "Bananas"));
            var cmd = new RemoveItemCommand(store);
            var ctx = MakeContext(store, "bananas");

            var result = await cmd.ExecuteAsync(ctx);

            Assert.True(result.Success);
            var doc = store.GetCurrentDoc();
            Assert.NotNull(doc);
            Assert.DoesNotContain("Bananas", doc!.Items);
        }

        [Fact]
        public async Task Execute_ReturnsErrorWhenStorageReadFails()
        {
            var store = new FakeGroceryListStore(failOnRead: true);
            var cmd = new RemoveItemCommand(store);
            var ctx = MakeContext(store, "Bananas");

            var result = await cmd.ExecuteAsync(ctx);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Execute_ReturnsErrorWhenStorageUpdateFails()
        {
            var store = new FakeGroceryListStore(MakeDoc("Apples", "Bananas"), failOnUpdate: true);
            var cmd = new RemoveItemCommand(store);
            var ctx = MakeContext(store, "Bananas");

            var result = await cmd.ExecuteAsync(ctx);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Execute_ReturnsFormattedList()
        {
            var store = new FakeGroceryListStore(MakeDoc("Apples", "Bananas", "Cherries"));
            var cmd = new RemoveItemCommand(store);
            var ctx = MakeContext(store, "Bananas");

            var result = await cmd.ExecuteAsync(ctx);

            Assert.True(result.Success);
            Assert.Contains("Apples", result.Message);
            Assert.Contains("Cherries", result.Message);
            Assert.DoesNotContain("Bananas", result.Message);
        }
    }
}
