using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VideoGamesStore.Models;

[Index("Name", Name = "UQ__Publishe__737584F683145023", IsUnique = true)]
public partial class Publisher
{
    [Key]
    public int Id { get; set; }

    [StringLength(150)]
    public string Name { get; set; } = null!;

    [InverseProperty("Publisher")]
    public virtual ICollection<Game> Games { get; set; } = new List<Game>();
}
