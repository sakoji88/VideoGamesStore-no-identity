using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VideoGamesStore.Models;

namespace VideoGamesStore.Services;

public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;
    public ActiveUserMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context, VideoGamesStoreContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var idValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idValue, out var userId))
            {
                var isActive = await db.Users.Where(u => u.Id == userId).Select(u => u.IsActive).FirstOrDefaultAsync();
                if (!isActive)
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }
        }

        await _next(context);
    }
}
