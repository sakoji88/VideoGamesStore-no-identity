using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoGamesStore.Models;
using VideoGamesStore.Services;
using VideoGamesStore.ViewModels.Account;

namespace VideoGamesStore.Controllers;

public class AccountController : Controller
{
    private readonly VideoGamesStoreContext _context;
    private readonly IPasswordHasher _hasher;

    public AccountController(VideoGamesStoreContext context, IPasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == model.Login || u.Email == model.Login);

        if (user is null || !_hasher.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Неверные учетные данные.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Ваш аккаунт заблокирован.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.Name)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        TempData["Success"] = "Вы успешно вошли в аккаунт.";
        return RedirectToAction("Index", "Games");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
        {
            ModelState.AddModelError(nameof(model.Username), "Такой логин уже занят.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Такой email уже используется.");
        }

        if (!ModelState.IsValid) return View(model);

        var userRole = await _context.Roles.FirstAsync(r => r.Name == "User");
        _context.Users.Add(new User
        {
            Username = model.Username.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            PasswordHash = _hasher.HashPassword(model.Password),
            RoleId = userRole.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "Регистрация прошла успешно. Теперь войдите в аккаунт.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Вы вышли из аккаунта.";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
