using Boltzenberg.Functions.Domain;
using Xunit;

namespace ApiTests.Domain
{
    public class GroceryListTests
    {
        private static GroceryList MakeList(params string[] items)
            => new GroceryList { ListId = "Test", Items = items.ToList() };

        [Fact]
        public void Apply_AddsItems()
        {
            var list = MakeList("Apples");
            var result = list.Apply(new[] { "Bananas", "Cherries" }, Array.Empty<string>());
            Assert.Contains("Bananas", result.Items);
            Assert.Contains("Cherries", result.Items);
            Assert.Contains("Apples", result.Items);
        }

        [Fact]
        public void Apply_RemovesItems()
        {
            var list = MakeList("Apples", "Bananas", "Cherries");
            var result = list.Apply(Array.Empty<string>(), new[] { "Bananas" });
            Assert.DoesNotContain("Bananas", result.Items);
            Assert.Contains("Apples", result.Items);
            Assert.Contains("Cherries", result.Items);
        }

        [Fact]
        public void Apply_AddAndRemoveInOneCall()
        {
            var list = MakeList("Apples", "Bananas");
            var result = list.Apply(new[] { "Cherries" }, new[] { "Bananas" });
            Assert.Contains("Apples", result.Items);
            Assert.Contains("Cherries", result.Items);
            Assert.DoesNotContain("Bananas", result.Items);
        }

        [Fact]
        public void Apply_IsCaseInsensitiveOnRemove()
        {
            var list = MakeList("Apples", "Bananas");
            var result = list.Apply(Array.Empty<string>(), new[] { "bananas" });
            Assert.DoesNotContain("Bananas", result.Items);
            Assert.Contains("Apples", result.Items);
        }

        [Fact]
        public void Apply_DeduplicatesOnAdd()
        {
            var list = MakeList("Apples");
            var result = list.Apply(new[] { "Apples", "Bananas" }, Array.Empty<string>());
            Assert.Single(result.Items.Where(i => i.Equals("Apples", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void Apply_SortsResult()
        {
            var list = MakeList();
            var result = list.Apply(new[] { "Zucchini", "Apples", "Mangoes" }, Array.Empty<string>());
            var items = result.Items.ToList();
            Assert.Equal("Apples", items[0]);
            Assert.Equal("Mangoes", items[1]);
            Assert.Equal("Zucchini", items[2]);
        }

        [Fact]
        public void Apply_PreservesListId()
        {
            var list = MakeList("Apples");
            var result = list.Apply(new[] { "Bananas" }, Array.Empty<string>());
            Assert.Equal("Test", result.ListId);
        }

        [Fact]
        public void Apply_ReturnsNewInstance()
        {
            var list = MakeList("Apples");
            var result = list.Apply(new[] { "Bananas" }, Array.Empty<string>());
            Assert.NotSame(list, result);
        }
    }
}
