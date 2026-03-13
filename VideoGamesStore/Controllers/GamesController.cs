using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VideoGamesStore.Models;

namespace VideoGamesStore.Controllers
{
    public class GamesController : Controller
    {
        private readonly VideoGamesStoreContext _context;

        public GamesController(VideoGamesStoreContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? genreId, int? platformId, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentGenre"] = genreId;
            ViewData["CurrentPlatform"] = platformId;
            ViewData["TitleSort"] = string.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["PriceSort"] = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            ViewData["CurrentSort"] = sortOrder;

            var gamesQuery = _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Publisher)
                .Include(g => g.Platforms)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                gamesQuery = gamesQuery.Where(g => g.Title.Contains(searchString));
            }

            if (genreId.HasValue)
            {
                gamesQuery = gamesQuery.Where(g => g.GenreId == genreId.Value);
            }

            if (platformId.HasValue)
            {
                gamesQuery = gamesQuery.Where(g => g.Platforms.Any(p => p.Id == platformId.Value));
            }

            gamesQuery = sortOrder switch
            {
                "title_desc" => gamesQuery.OrderByDescending(g => g.Title),
                "price_asc" => gamesQuery.OrderBy(g => g.Price),
                "price_desc" => gamesQuery.OrderByDescending(g => g.Price),
                _ => gamesQuery.OrderBy(g => g.Title)
            };

            ViewBag.Genres = new SelectList(
     await _context.Genres.OrderBy(g => g.Name).ToListAsync(),
     "Id",
     "Name",
     genreId
 );

            ViewBag.Platforms = new SelectList(
                await _context.Platforms.OrderBy(p => p.Name).ToListAsync(),
                "Id",
                "Name",
                platformId
            );

            var games = await gamesQuery.ToListAsync();

            return View(games);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Publisher)
                .Include(g => g.Platforms)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            return View(game);

        }
        public IActionResult Create()
        {
            ViewBag.Genres = new SelectList(_context.Genres.OrderBy(g => g.Name), "Id", "Name");
            ViewBag.Publishers = new SelectList(_context.Publishers.OrderBy(p => p.Name), "Id", "Name");
            ViewBag.Platforms = new MultiSelectList(_context.Platforms.OrderBy(p => p.Name), "Id", "Name");

            return View(new Game());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGenre(string genreName)
        {
            if (!string.IsNullOrWhiteSpace(genreName))
            {
                bool exists = await _context.Genres
                    .AnyAsync(g => g.Name == genreName);

                if (!exists)
                {
                    _context.Genres.Add(new Genre
                    {
                        Name = genreName
                    });

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Create));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPublisher(string publisherName)
        {
            if (!string.IsNullOrWhiteSpace(publisherName))
            {
                bool exists = await _context.Publishers
                    .AnyAsync(p => p.Name == publisherName);

                if (!exists)
                {
                    _context.Publishers.Add(new Publisher
                    {
                        Name = publisherName
                    });

                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Create));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    string Title,
    string? Description,
    decimal Price,
    int Stock,
    int GenreId,
    int? PublisherId,
    string? CoverImageUrl,
    DateOnly? ReleaseDate,
    decimal? Rating,
    string? AgeRating,
    bool IsActive,
    int[] selectedPlatforms)
        {
            var game = new Game
            {
                Title = Title,
                Description = Description,
                Price = Price,
                Stock = Stock,
                GenreId = GenreId,
                PublisherId = PublisherId,
                CoverImageUrl = CoverImageUrl,
                ReleaseDate = ReleaseDate,
                Rating = Rating,
                AgeRating = AgeRating,
                IsActive = IsActive
            };

            if (selectedPlatforms != null)
            {
                foreach (var platformId in selectedPlatforms)
                {
                    var platform = await _context.Platforms.FindAsync(platformId);
                    if (platform != null)
                    {
                        game.Platforms.Add(platform);
                    }
                }
            }

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.Platforms)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            ViewBag.Genres = new SelectList(_context.Genres.OrderBy(g => g.Name), "Id", "Name", game.GenreId);
            ViewBag.Publishers = new SelectList(_context.Publishers.OrderBy(p => p.Name), "Id", "Name", game.PublisherId);
            ViewBag.Platforms = new MultiSelectList(
                _context.Platforms.OrderBy(p => p.Name),
                "Id",
                "Name",
                game.Platforms.Select(p => p.Id)
            );

            return View(game);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    int id,
    string Title,
    string? Description,
    decimal Price,
    int Stock,
    int GenreId,
    int? PublisherId,
    string? CoverImageUrl,
    DateOnly? ReleaseDate,
    decimal? Rating,
    string? AgeRating,
    bool IsActive,
    int[] selectedPlatforms)
        {
            var gameFromDb = await _context.Games
                .Include(g => g.Platforms)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gameFromDb == null)
            {
                return NotFound();
            }

            gameFromDb.Title = Title;
            gameFromDb.Description = Description;
            gameFromDb.Price = Price;
            gameFromDb.Stock = Stock;
            gameFromDb.GenreId = GenreId;
            gameFromDb.PublisherId = PublisherId;
            gameFromDb.CoverImageUrl = CoverImageUrl;
            gameFromDb.ReleaseDate = ReleaseDate;
            gameFromDb.Rating = Rating;
            gameFromDb.AgeRating = AgeRating;
            gameFromDb.IsActive = IsActive;

            gameFromDb.Platforms.Clear();

            if (selectedPlatforms != null)
            {
                foreach (var platformId in selectedPlatforms)
                {
                    var platform = await _context.Platforms.FindAsync(platformId);
                    if (platform != null)
                    {
                        gameFromDb.Platforms.Add(platform);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Publisher)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var game = await _context.Games
                .Include(g => g.Platforms)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game != null)
            {
                game.Platforms.Clear();
                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
    }