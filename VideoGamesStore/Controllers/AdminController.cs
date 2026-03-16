using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoGamesStore.Models;
using VideoGamesStore.ViewModels.Admin;

namespace VideoGamesStore.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly VideoGamesStoreContext _context;
    public AdminController(VideoGamesStoreContext context) => _context = context;

    public async Task<IActionResult> Users(string? search)
    {
        var query = _context.Users.Include(u => u.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
        }

        var model = await query.OrderBy(u => u.Username)
            .Select(u => new UserManagementViewModel
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role.Name,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToListAsync();

        ViewBag.Search = search;
        return View(model);
    }

    public async Task<IActionResult> Orders(string? search)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Game)
            .Where(o => o.Status == "Оформлен")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => o.User.Username.Contains(search) || o.User.Email.Contains(search));
        }

        var model = await query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderAdminViewModel
            {
                Id = o.Id,
                Username = o.User.Username,
                Email = o.User.Email,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                Items = o.OrderItems.Select(i => new OrderItemAdminViewModel
                {
                    GameTitle = i.Game.Title,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            }).ToListAsync();

        ViewBag.Search = search;
        return View(model);
    }

    public async Task<IActionResult> Reviews(string? search)
    {
        var query = _context.Reviews
            .Include(r => r.Game)
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.Game.Title.Contains(search) || r.User.Username.Contains(search));
        }

        var model = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewModerationViewModel
            {
                Id = r.Id,
                GameId = r.GameId,
                GameTitle = r.Game.Title,
                Username = r.User.Username,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsVisible = r.IsVisible
            })
            .ToListAsync();

        ViewBag.Search = search;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleReviewVisibility(int id)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
        if (review is null) return NotFound();

        review.IsVisible = !review.IsVisible;
        await _context.SaveChangesAsync();
        TempData["Success"] = review.IsVisible ? "Отзыв опубликован." : "Отзыв скрыт.";
        return RedirectToAction(nameof(Reviews));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBan(int id)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var currentUserId = int.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")!.Value);
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Нельзя заблокировать самого себя.";
            return RedirectToAction(nameof(Users));
        }

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = user.IsActive ? "Пользователь разблокирован." : "Пользователь заблокирован.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRole(int id)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var adminRole = await _context.Roles.FirstAsync(r => r.Name == "Admin");
        var userRole = await _context.Roles.FirstAsync(r => r.Name == "User");

        var demotingAdmin = user.RoleId == adminRole.Id;
        if (demotingAdmin)
        {
            var adminsCount = await _context.Users.CountAsync(u => u.RoleId == adminRole.Id);
            if (adminsCount <= 1)
            {
                TempData["Error"] = "Нельзя понизить последнего администратора.";
                return RedirectToAction(nameof(Users));
            }
        }

        user.RoleId = user.RoleId == adminRole.Id ? userRole.Id : adminRole.Id;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Роль пользователя обновлена.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        if (user.Role.Name == "Admin")
        {
            var adminsCount = await _context.Users.CountAsync(u => u.Role.Name == "Admin");
            if (adminsCount <= 1)
            {
                TempData["Error"] = "Нельзя удалить последнего администратора.";
                return RedirectToAction(nameof(Users));
            }
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Пользователь удален.";
        return RedirectToAction(nameof(Users));
    }
}
