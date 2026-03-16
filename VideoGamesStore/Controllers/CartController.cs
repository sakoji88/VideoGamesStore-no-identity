using System.Security.Claims;
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


    public async Task<IActionResult> Orders()
    {
        var userId = GetCurrentUserId();
        var orders = await _context.Orders
            .Where(o => o.UserId == userId && o.Status == "Оформлен")
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Game)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderHistoryViewModel
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                Items = o.OrderItems.Select(i => new OrderHistoryItemViewModel
                {
                    GameTitle = i.Game.Title,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        return View(orders);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int gameId)
    {
        var success = await AddItemToCartAsync(gameId);
        if (!success)
        {
            TempData["Error"] = "Игра недоступна для добавления в корзину.";
            return RedirectToAction("Index", "Games");
        }

        TempData["Success"] = "Товар добавлен в корзину.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyNow(int gameId)
    {
        var success = await AddItemToCartAsync(gameId);
        if (!success)
        {
            TempData["Error"] = "Игра недоступна для покупки.";
            return RedirectToAction("Index", "Games");
        }

        TempData["Success"] = "Товар добавлен в корзину. Перейдите к оформлению.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int orderItemId, int quantity)
    {
        if (quantity is < 1 or > 99)
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
        item.Order.TotalAmount = await CalculateOrderTotalAsync(item.OrderId);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Количество обновлено.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int orderItemId)
    {
        var userId = GetCurrentUserId();
        var item = await _context.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == orderItemId && i.Order.UserId == userId && i.Order.Status == "Создан");

        if (item is null) return NotFound();

        var orderId = item.OrderId;
        _context.OrderItems.Remove(item);
        await _context.SaveChangesAsync();

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is not null)
        {
            order.TotalAmount = await CalculateOrderTotalAsync(orderId);
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Позиция удалена из корзины.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int orderId)
    {
        var userId = GetCurrentUserId();
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Game)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId && o.Status == "Создан");

        if (order is null || !order.OrderItems.Any())
        {
            TempData["Error"] = "Корзина пуста или недоступна.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var item in order.OrderItems)
        {
            if (item.Quantity > item.Game.Stock)
            {
                TempData["Error"] = $"Недостаточно остатка для игры {item.Game.Title}.";
                return RedirectToAction(nameof(Index));
            }
        }

        foreach (var item in order.OrderItems)
        {
            item.Game.Stock -= item.Quantity;
        }

        order.TotalAmount = await CalculateOrderTotalAsync(order.Id);
        order.Status = "Оформлен";
        await _context.SaveChangesAsync();

        TempData["Success"] = "Покупка успешно оформлена!";
        return RedirectToAction("Index", "Games");
    }

    private async Task<bool> AddItemToCartAsync(int gameId)
    {
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId && g.IsActive);
        if (game is null || game.Stock <= 0)
        {
            return false;
        }

        var order = await GetOrCreateCartAsync();
        var item = order.OrderItems.FirstOrDefault(x => x.GameId == gameId);

        if (item is null)
        {
            order.OrderItems.Add(new OrderItem { GameId = gameId, Quantity = 1, UnitPrice = game.Price });
        }
        else
        {
            if (item.Quantity >= game.Stock)
            {
                TempData["Error"] = "Достигнуто максимальное количество по остатку.";
                return true;
            }

            item.Quantity += 1;
        }

        order.TotalAmount = await CalculateOrderTotalAsync(order.Id);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<decimal> CalculateOrderTotalAsync(int orderId)
    {
        var total = await _context.OrderItems
            .Where(i => i.OrderId == orderId)
            .Select(i => (decimal?)(i.UnitPrice * i.Quantity))
            .SumAsync();

        return total ?? 0m;
    }

    private async Task<Order> GetOrCreateCartAsync()
    {
        var userId = GetCurrentUserId();
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Game)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Создан");

        if (order is not null)
        {
            return order;
        }

        order = new Order { UserId = userId, Status = "Создан", OrderDate = DateTime.UtcNow, TotalAmount = 0m };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Game)
            .FirstAsync(o => o.Id == order.Id);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var userId) ? userId : 0;
    }
}
