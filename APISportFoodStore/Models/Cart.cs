using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class Cart
{
    public int? IdCart { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }
}
