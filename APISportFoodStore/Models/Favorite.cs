using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class Favorite
{
    public int? IdFavorite { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }
}
