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
