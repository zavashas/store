using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class DeliveryTimeSlot
{
    public int? IdDeliverySlot { get; set; }

    public string TimeRange { get; set; } = null!;

    public bool Deleted { get; set; }
}
