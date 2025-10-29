using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class UserAddress
{
    public int? IdAddress { get; set; }

    public int UserId { get; set; }

    public string City { get; set; } = null!;

    public string Street { get; set; } = null!;

    public string House { get; set; } = null!;

    public string? Apartament { get; set; }

    public string? CourierComment { get; set; }

    public bool Deleted { get; set; }
}
