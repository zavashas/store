using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class ProductImage
{
    public int? IdProductImage { get; set; }

    public int ProductId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsMain { get; set; }

    public int SortOrder { get; set; }

    public string? AltText { get; set; }

    public bool Deleted { get; set; }

    public DateTime CreatedAt { get; set; }
}
