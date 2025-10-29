using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class Order
{
    public int? IdOrder { get; set; }

    public int UserId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public DateOnly DeliveryDate { get; set; }

    public int DeliverySlotId { get; set; }

    public int? OrderStatusId { get; set; }
}
