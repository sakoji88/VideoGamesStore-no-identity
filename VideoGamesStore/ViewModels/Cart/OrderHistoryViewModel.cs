namespace VideoGamesStore.ViewModels.Cart;

public class OrderHistoryItemViewModel
{
    public string GameTitle { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => Quantity * UnitPrice;
}

public class OrderHistoryViewModel
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderHistoryItemViewModel> Items { get; set; } = [];
}
