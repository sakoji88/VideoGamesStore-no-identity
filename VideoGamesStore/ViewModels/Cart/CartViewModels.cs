using VideoGamesStore.Models;

namespace VideoGamesStore.ViewModels.Cart;

public class CartItemViewModel
{
    public int OrderItemId { get; set; }
    public int GameId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => UnitPrice * Quantity;
    public string? CoverImageUrl { get; set; }
}

public class CartViewModel
{
    public int OrderId { get; set; }
    public List<CartItemViewModel> Items { get; set; } = [];
    public decimal GrandTotal => Items.Sum(x => x.Total);
}
