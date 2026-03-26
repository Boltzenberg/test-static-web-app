using ApiTests.Fakes;
using Boltzenberg.Functions.Commands.Telegram;
using Boltzenberg.Functions.Domain.Telegram;
using Boltzenberg.Functions.Storage.Documents;
using Xunit;

namespace ApiTests.Commands
{
    public class AddItemCommandTests
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
                RawText = arg != null ? $"/add {arg}" : "/add",
                CommandName = "/add",
                Arg = arg,
                Log = NoOpLogBuffer.Create()
            };

        [Fact]
        public async Task Execute_AddsItemToList()
        {
            var store = new FakeGroceryListStore(MakeDoc("Apples"));
            var cmd = new AddItemCommand(store);
            var ctx = MakeContext(store, "Bananas");

            var result = await cmd.ExecuteAsync(ctx);

            Assert.True(result.Success);
            var doc = store.GetCurrentDoc();
            Assert.NotNull(doc);
            Assert.Contains("Bananas", doc!.Items);
        }

        [Fact]
        public async Task Execute_ReturnsFormattedList()
        {
            var store = new FakeGroceryListStore(MakeDoc("Apples"));
            var cmd = new AddItemCommand(store);
            var ctx = MakeContext(store, "Bananas");


            var result = await cmd.ExecuteAsync(ctx);

            Assert.True(result.Success);
            Assert.Contains("Apples", result.Message);
            Assert.Contains("Bananas", result.Message);
        }

        [Fact]
        public async Task Execute_ReturnsErrorWhenStorageReadFails()
        {
            var store = new FakeGroceryListStore(failOnRead: true);
            var cmd = new AddItemCommand(store);
            var ctx = MakeContext(store, "Bananas");

            var result = await cmd.ExecuteAsync(ctx);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Execute_ReturnsErrorWhenStorageUpdateFails()
        {
            var store = new FakeGroceryListStore(MakeDoc("Apples"), failOnUpdate: true);
            var cmd = new AddItemCommand(store);
            var ctx = MakeContext(store, "Bananas");

            var result = await cmd.ExecuteAsync(ctx);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Execute_ReturnsErrorWhenNoArgsProvided()
        {
            var store = new FakeGroceryListStore(MakeDoc());
            var cmd = new AddItemCommand(store);
            var ctx = MakeContext(store); // no args

            var result = await cmd.ExecuteAsync(ctx);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Execute_TreatsMultiWordArgAsSingleItem()
        {
            var store = new FakeGroceryListStore(MakeDoc());
            var cmd = new AddItemCommand(store);
            // Dispatcher passes the full remainder as Args[0], so "peanut butter" is one item.
            var ctx = MakeContext(store, "peanut butter");

            var result = await cmd.ExecuteAsync(ctx);

            Assert.True(result.Success);
            var doc = store.GetCurrentDoc();
            Assert.NotNull(doc);
            Assert.Contains("peanut butter", doc!.Items);
            Assert.DoesNotContain("peanut", doc!.Items.Where(i => i == "peanut"));
            Assert.DoesNotContain("butter", doc!.Items.Where(i => i == "butter"));
        }

        [Fact]
        public async Task Execute_DeduplicatesItems()
        {
            var store = new FakeGroceryListStore(MakeDoc("Apples"));
            var cmd = new AddItemCommand(store);
            var ctx = MakeContext(store, "apples"); // lowercase duplicate

            var result = await cmd.ExecuteAsync(ctx);

            Assert.True(result.Success);
            var doc = store.GetCurrentDoc();
            Assert.NotNull(doc);
            Assert.Single(doc!.Items.Where(i => i.Equals("Apples", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
