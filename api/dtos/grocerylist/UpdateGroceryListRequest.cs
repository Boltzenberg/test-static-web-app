namespace Boltzenberg.Functions.Dtos.GroceryList
{
    public record UpdateGroceryListRequest(
        List<string> ToAdd,
        List<string> ToRemove
    );
}
