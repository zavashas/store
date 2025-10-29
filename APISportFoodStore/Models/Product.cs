using System;
using System.Collections.Generic;

namespace APISportFoodStore.Models;

public partial class Product
{
    public int? IdProduct { get; set; }

    public string Name { get; set; } = null!;

    public string? Article { get; set; } = null!;

    public int CategoryId { get; set; }

    public int ManufacturerId { get; set; }

    public string Unit { get; set; } = null!;

    public decimal VolumeOrWeight { get; set; }

    public string Description { get; set; } = null!;

    public string? Image { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public bool IsAvailable { get; set; }

    public bool Deleted { get; set; }

    public decimal? CaloriesKcal { get; set; }

    public decimal? ProteinG { get; set; }

    public decimal? FatG { get; set; }

    public decimal? CarbsG { get; set; }
}
