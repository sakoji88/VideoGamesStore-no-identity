namespace VideoGamesStore.ViewModels.Admin;

public class OrderItemAdminViewModel
{
    public string GameTitle { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => Quantity * UnitPrice;
}

public class OrderAdminViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderItemAdminViewModel> Items { get; set; } = new List<OrderItemAdminViewModel>();
}
