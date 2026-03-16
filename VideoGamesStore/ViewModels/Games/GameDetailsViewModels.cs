using System.ComponentModel.DataAnnotations;
using VideoGamesStore.Models;

namespace VideoGamesStore.ViewModels.Games;

public class ReviewViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddReviewViewModel
{
    public int GameId { get; set; }

    [Display(Name = "Оценка")]
    [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5")]
    public int Rating { get; set; }

    [Display(Name = "Комментарий")]
    [StringLength(1000, ErrorMessage = "Комментарий не должен превышать 1000 символов")]
    public string? Comment { get; set; }
}

public class GameDetailsViewModel
{
    public Game Game { get; set; } = null!;
    public bool CanLeaveReview { get; set; }
    public bool HasPurchasedGame { get; set; }
    public bool HasReview { get; set; }
    public List<ReviewViewModel> Reviews { get; set; } = [];
    public AddReviewViewModel NewReview { get; set; } = new();
}
