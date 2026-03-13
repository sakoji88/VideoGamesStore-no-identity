using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VideoGamesStore.Models;
using VideoGamesStore.ViewModels.Games;

namespace VideoGamesStore.Controllers;

public class GamesController : Controller
{
    private readonly VideoGamesStoreContext _context;
    private const int PageSize = 9;
    public GamesController(VideoGamesStoreContext context) => _context = context;

    public async Task<IActionResult> Index(string? searchString, int? genreId, int? platformId, string? sortOrder, int page = 1)
    {
        var gamesQuery = _context.Games.Include(g => g.Genre).Include(g => g.Publisher).Include(g => g.Platforms).AsQueryable();
        if (!User.IsInRole("Admin")) gamesQuery = gamesQuery.Where(g => g.IsActive);
        if (!string.IsNullOrWhiteSpace(searchString)) gamesQuery = gamesQuery.Where(g => g.Title.Contains(searchString));
        if (genreId.HasValue) gamesQuery = gamesQuery.Where(g => g.GenreId == genreId.Value);
        if (platformId.HasValue) gamesQuery = gamesQuery.Where(g => g.Platforms.Any(p => p.Id == platformId.Value));

        gamesQuery = sortOrder switch
        {
            "title_desc" => gamesQuery.OrderByDescending(g => g.Title),
            "price_asc" => gamesQuery.OrderBy(g => g.Price),
            "price_desc" => gamesQuery.OrderByDescending(g => g.Price),
            _ => gamesQuery.OrderBy(g => g.Title)
        };

        var total = await gamesQuery.CountAsync();
        var vm = new GamesIndexViewModel
        {
            Items = await gamesQuery.Skip((Math.Max(page, 1) - 1) * PageSize).Take(PageSize).ToListAsync(),
            Genres = new SelectList(await _context.Genres.OrderBy(g => g.Name).ToListAsync(), "Id", "Name", genreId),
            Platforms = new SelectList(await _context.Platforms.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", platformId),
            SearchString = searchString,
            GenreId = genreId,
            PlatformId = platformId,
            SortOrder = sortOrder,
            Page = Math.Max(page, 1),
            TotalPages = (int)Math.Ceiling(total / (double)PageSize)
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var game = await _context.Games.Include(g => g.Genre).Include(g => g.Publisher).Include(g => g.Platforms).FirstOrDefaultAsync(g => g.Id == id);
        if (game is null || (!game.IsActive && !User.IsInRole("Admin"))) return NotFound();
        return View(game);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        FillMeta();
        return View(new Game { IsActive = true });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGenre(string genreName)
    {
        if (!string.IsNullOrWhiteSpace(genreName) && !await _context.Genres.AnyAsync(g => g.Name == genreName))
        {
            _context.Genres.Add(new Genre { Name = genreName.Trim() });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Create));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPublisher(string publisherName)
    {
        if (!string.IsNullOrWhiteSpace(publisherName) && !await _context.Publishers.AnyAsync(p => p.Name == publisherName))
        {
            _context.Publishers.Add(new Publisher { Name = publisherName.Trim() });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Create));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Game game, int[] selectedPlatforms)
    {
        if (!ModelState.IsValid)
        {
            FillMeta(game.GenreId, game.PublisherId, selectedPlatforms);
            return View(game);
        }
        game.Platforms = await _context.Platforms.Where(p => selectedPlatforms.Contains(p.Id)).ToListAsync();
        _context.Games.Add(game);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Игра добавлена.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var game = await _context.Games.Include(g => g.Platforms).FirstOrDefaultAsync(g => g.Id == id);
        if (game is null) return NotFound();
        FillMeta(game.GenreId, game.PublisherId, game.Platforms.Select(p => p.Id));
        return View(game);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Game model, int[] selectedPlatforms)
    {
        var game = await _context.Games.Include(g => g.Platforms).FirstOrDefaultAsync(g => g.Id == id);
        if (game is null) return NotFound();
        if (!ModelState.IsValid)
        {
            FillMeta(model.GenreId, model.PublisherId, selectedPlatforms);
            return View(model);
        }

        game.Title = model.Title;
        game.Description = model.Description;
        game.Price = model.Price;
        game.Stock = model.Stock;
        game.GenreId = model.GenreId;
        game.PublisherId = model.PublisherId;
        game.CoverImageUrl = model.CoverImageUrl;
        game.ReleaseDate = model.ReleaseDate;
        game.Rating = model.Rating;
        game.AgeRating = model.AgeRating;
        game.IsActive = model.IsActive;
        game.Platforms = await _context.Platforms.Where(p => selectedPlatforms.Contains(p.Id)).ToListAsync();
        await _context.SaveChangesAsync();

        TempData["Success"] = "Игра обновлена.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var game = await _context.Games.Include(g => g.Genre).FirstOrDefaultAsync(g => g.Id == id);
        return game is null ? NotFound() : View(game);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var game = await _context.Games.Include(g => g.Platforms).FirstOrDefaultAsync(g => g.Id == id);
        if (game is not null)
        {
            game.Platforms.Clear();
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private void FillMeta(int? genreId = null, int? publisherId = null, IEnumerable<int>? selectedPlatforms = null)
    {
        ViewBag.Genres = new SelectList(_context.Genres.OrderBy(g => g.Name), "Id", "Name", genreId);
        ViewBag.Publishers = new SelectList(_context.Publishers.OrderBy(p => p.Name), "Id", "Name", publisherId);
        ViewBag.Platforms = new MultiSelectList(_context.Platforms.OrderBy(p => p.Name), "Id", "Name", selectedPlatforms);
    }
}
