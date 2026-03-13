using Microsoft.AspNetCore.Mvc.Rendering;
using VideoGamesStore.Models;

namespace VideoGamesStore.ViewModels.Games;

public class GamesIndexViewModel
{
    public IEnumerable<Game> Items { get; set; } = [];
    public SelectList Genres { get; set; } = null!;
    public SelectList Platforms { get; set; } = null!;
    public string? SearchString { get; set; }
    public int? GenreId { get; set; }
    public int? PlatformId { get; set; }
    public string? SortOrder { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
}
