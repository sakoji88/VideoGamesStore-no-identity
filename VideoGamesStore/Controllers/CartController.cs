using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoGamesStore.Models;
using VideoGamesStore.ViewModels.Cart;

namespace VideoGamesStore.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly VideoGamesStoreContext _context;
    public CartController(VideoGamesStoreContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var order = await GetOrCreateCartAsync();
        var vm = new CartViewModel
        {
            OrderId = order.Id,
            Items = order.OrderItems.Select(i => new CartItemViewModel
            {
                OrderItemId = i.Id,
                GameId = i.GameId,
                Title = i.Game.Title,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                CoverImageUrl = i.Game.CoverImageUrl
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int gameId)
    {
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId && g.IsActive);
        if (game is null || game.Stock <= 0)
        {
            TempData["Error"] = "Игра недоступна для добавления в корзину.";
            return RedirectToAction("Index", "Games");
        }

        var order = await GetOrCreateCartAsync();
        var item = order.OrderItems.FirstOrDefault(x => x.GameId == gameId);
        if (item is null)
        {
            order.OrderItems.Add(new OrderItem { GameId = gameId, Quantity = 1, UnitPrice = game.Price });
        }
        else if (item.Quantity < game.Stock)
        {
            item.Quantity += 1;
        }

        order.TotalAmount = order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Товар добавлен в корзину.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int orderItemId, int quantity)
    {
        if (quantity < 1 || quantity > 99)
        {
            TempData["Error"] = "Некорректное количество.";
            return RedirectToAction(nameof(Index));
        }

        var userId = GetCurrentUserId();
        var item = await _context.OrderItems
            .Include(i => i.Game)
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == orderItemId && i.Order.UserId == userId && i.Order.Status == "Создан");

        if (item is null) return NotFound();
        if (quantity > item.Game.Stock)
        {
            TempData["Error"] = "Недостаточно товара на складе.";
            return RedirectToAction(nameof(Index));
        }

        item.Quantity = quantity;
        item.Order.TotalAmount = item.Order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Количество обновлено.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int orderItemId)
    {
        var userId = GetCurrentUserId();
        var item = await _context.OrderItems.Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == orderItemId && i.Order.UserId == userId && i.Order.Status == "Создан");
        if (item is null) return NotFound();

        _context.OrderItems.Remove(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Позиция удалена из корзины.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Order> GetOrCreateCartAsync()
    {
        var userId = GetCurrentUserId();
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Game)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Создан");

        if (order is not null) return order;

        order = new Order { UserId = userId, Status = "Создан", OrderDate = DateTime.UtcNow, TotalAmount = 0m };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Game)
            .FirstAsync(o => o.Id == order.Id);
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")!.Value);
}
