using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class Manufacturer
{
    public int? IdManufacturer { get; set; }

    public string Name { get; set; } = null!;

    public bool Deleted { get; set; }
}
