using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using VideoGamesStore.Models;

namespace VideoGamesStore.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly VideoGamesStoreContext _context;

    public HomeController(ILogger<HomeController> logger, VideoGamesStoreContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var games = await _context.Games.Include(g => g.Genre).Where(g => g.IsActive).OrderByDescending(g => g.Rating).ThenBy(g => g.Title).ToListAsync();
        return View(games);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
