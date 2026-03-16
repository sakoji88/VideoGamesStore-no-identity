using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VideoGamesStore.Models;
using VideoGamesStore.ViewModels.Games;

namespace VideoGamesStore.Controllers
{
    public class GamesController : Controller
    {
        private const int PageSize = 9;
        private readonly VideoGamesStoreContext _context;

        public GamesController(VideoGamesStoreContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? genreId, int? platformId, string sortOrder, int page = 1)
        {
            var query = _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Publisher)
                .Include(g => g.Platforms)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                query = query.Where(g => g.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(g => g.Title.Contains(searchString));
            }

            if (genreId.HasValue)
            {
                query = query.Where(g => g.GenreId == genreId.Value);
            }

            if (platformId.HasValue)
            {
                query = query.Where(g => g.Platforms.Any(p => p.Id == platformId.Value));
            }

            switch (sortOrder)
            {
                case "title_desc":
                    query = query.OrderByDescending(g => g.Title);
                    break;
                case "price_asc":
                    query = query.OrderBy(g => g.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(g => g.Price);
                    break;
                default:
                    query = query.OrderBy(g => g.Title);
                    break;
            }

            var safePage = Math.Max(page, 1);
            var totalItems = await query.CountAsync();

            var model = new GamesIndexViewModel();
            model.Items = await query.Skip((safePage - 1) * PageSize).Take(PageSize).ToListAsync();
            model.Genres = new SelectList(await _context.Genres.OrderBy(g => g.Name).ToListAsync(), "Id", "Name", genreId);
            model.Platforms = new SelectList(await _context.Platforms.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", platformId);
            model.SearchString = searchString;
            model.GenreId = genreId;
            model.PlatformId = platformId;
            model.SortOrder = sortOrder;
            model.Page = safePage;
            model.TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Publisher)
                .Include(g => g.Platforms)
                .FirstOrDefaultAsync(g => g.Id == id.Value);

            if (game == null || (!game.IsActive && !User.IsInRole("Admin")))
            {
                return NotFound();
            }

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.GameId == game.Id && (r.IsVisible || User.IsInRole("Admin")))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var model = new GameDetailsViewModel();
            model.Game = game;
            model.Reviews = reviews.Select(r => new ReviewViewModel
            {
                Id = r.Id,
                Username = r.User.Username,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();
            model.NewReview = new AddReviewViewModel { GameId = game.Id };

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = GetCurrentUserId();
                model.HasPurchasedGame = await _context.OrderItems.AnyAsync(i =>
                    i.GameId == game.Id && i.Order.UserId == userId && i.Order.Status == "Оформлен");
                model.HasReview = await _context.Reviews.AnyAsync(r => r.GameId == game.Id && r.UserId == userId);
                model.CanLeaveReview = model.HasPurchasedGame && !model.HasReview;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(AddReviewViewModel model)
        {
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == model.GameId);
            if (game == null || (!game.IsActive && !User.IsInRole("Admin")))
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var hasPurchased = await _context.OrderItems.AnyAsync(i =>
                i.GameId == model.GameId && i.Order.UserId == userId && i.Order.Status == "Оформлен");

            if (!hasPurchased)
            {
                TempData["Error"] = "Оставлять отзывы могут только пользователи, купившие игру.";
                return RedirectToAction("Details", new { id = model.GameId });
            }

            var alreadyReviewed = await _context.Reviews.AnyAsync(r => r.GameId == model.GameId && r.UserId == userId);
            if (alreadyReviewed)
            {
                TempData["Error"] = "Вы уже оставили отзыв на эту игру.";
                return RedirectToAction("Details", new { id = model.GameId });
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Проверьте корректность оценки и текста отзыва.";
                return RedirectToAction("Details", new { id = model.GameId });
            }

            _context.Reviews.Add(new Review
            {
                GameId = model.GameId,
                UserId = userId,
                Rating = model.Rating,
                Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsVisible = false
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Отзыв отправлен на модерацию и появится после подтверждения администратором.";
            return RedirectToAction("Details", new { id = model.GameId });
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            FillMeta(null, null, null);
            return View(new Game { IsActive = true });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGenre(string genreName)
        {
            if (!string.IsNullOrWhiteSpace(genreName))
            {
                var normalized = genreName.Trim();
                var exists = await _context.Genres.AnyAsync(g => g.Name == normalized);
                if (!exists)
                {
                    _context.Genres.Add(new Genre { Name = normalized });
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Create");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPublisher(string publisherName)
        {
            if (!string.IsNullOrWhiteSpace(publisherName))
            {
                var normalized = publisherName.Trim();
                var exists = await _context.Publishers.AnyAsync(p => p.Name == normalized);
                if (!exists)
                {
                    _context.Publishers.Add(new Publisher { Name = normalized });
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Create");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Game game, int[] selectedPlatforms)
        {
            if (selectedPlatforms == null)
            {
                selectedPlatforms = Array.Empty<int>();
            }

            if (!ModelState.IsValid)
            {
                FillMeta(game.GenreId, game.PublisherId, selectedPlatforms);
                return View(game);
            }

            game.Platforms = await _context.Platforms.Where(p => selectedPlatforms.Contains(p.Id)).ToListAsync();
            game.CreatedAt = DateTime.UtcNow;

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Игра добавлена.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var game = await _context.Games.Include(g => g.Platforms).FirstOrDefaultAsync(g => g.Id == id);
            if (game == null)
            {
                return NotFound();
            }

            FillMeta(game.GenreId, game.PublisherId, game.Platforms.Select(p => p.Id));
            return View(game);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Game model, int[] selectedPlatforms)
        {
            if (selectedPlatforms == null)
            {
                selectedPlatforms = Array.Empty<int>();
            }

            var game = await _context.Games.Include(g => g.Platforms).FirstOrDefaultAsync(g => g.Id == id);
            if (game == null)
            {
                return NotFound();
            }

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
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ActionName("Delete")]
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

            return RedirectToAction("Index");
        }

        private void FillMeta(int? genreId, int? publisherId, IEnumerable<int> selectedPlatforms)
        {
            ViewBag.Genres = new SelectList(_context.Genres.OrderBy(g => g.Name), "Id", "Name", genreId);
            ViewBag.Publishers = new SelectList(_context.Publishers.OrderBy(p => p.Name), "Id", "Name", publisherId);
            ViewBag.Platforms = new MultiSelectList(_context.Platforms.OrderBy(p => p.Name), "Id", "Name", selectedPlatforms);
        }

        private int GetCurrentUserId()
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int userId;
            return int.TryParse(claimValue, out userId) ? userId : 0;
        }
    }
}
