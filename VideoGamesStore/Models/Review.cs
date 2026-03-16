using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VideoGamesStore.Models;

public partial class Review
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int GameId { get; set; }

    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public bool IsVisible { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("GameId")]
    [InverseProperty("Reviews")]
    public virtual Game Game { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Reviews")]
    public virtual User User { get; set; } = null!;
}
