    using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VideoGamesStore.Models;

public partial class Game
{
    [Key]
    public int Id { get; set; }

    public int? RawgId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Price { get; set; }

    public int Stock { get; set; }

    [Column(TypeName = "decimal(3, 1)")]
    public decimal? Rating { get; set; }

    [StringLength(500)]
    public string? CoverImageUrl { get; set; }

    [StringLength(20)]
    public string? AgeRating { get; set; }

    public int? PublisherId { get; set; }

    public int GenreId { get; set; }

    public bool IsActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("GenreId")]
    [InverseProperty("Games")]
    public virtual Genre Genre { get; set; } = null!;

    [InverseProperty("Game")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [ForeignKey("PublisherId")]
    [InverseProperty("Games")]
    public virtual Publisher? Publisher { get; set; }

    [InverseProperty("Game")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [ForeignKey("GameId")]
    [InverseProperty("Games")]
    public virtual ICollection<Platform> Platforms { get; set; } = new List<Platform>();
}
