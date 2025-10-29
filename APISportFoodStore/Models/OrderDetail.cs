using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class OrderDetail
{
    public int? IdOrderDetail { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }
}
