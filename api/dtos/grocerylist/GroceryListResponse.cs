namespace Boltzenberg.Functions.Dtos.GroceryList
{
    public record GroceryListResponse(
        string ListId,
        List<string> Items
    );
}
