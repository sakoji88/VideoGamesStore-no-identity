using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VideoGamesStore.Models;

[Index("Name", Name = "UQ__Platform__737584F6D58D84B8", IsUnique = true)]
public partial class Platform
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [ForeignKey("PlatformId")]
    [InverseProperty("Platforms")]
    public virtual ICollection<Game> Games { get; set; } = new List<Game>();
}
