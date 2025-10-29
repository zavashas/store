using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class ReviewImage
{
    public int? IdReviewImage { get; set; }

    public int ReviewId { get; set; }

    public string ImageUrl { get; set; } = null!;
}
