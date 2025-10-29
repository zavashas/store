namespace APISportFoodStore.Models
{
    public class ProductDto
    {
        public int IdProduct { get; set; }
        public string Name { get; set; } = null!;
        public string Article { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? Image { get; set; }
        public decimal Price { get; set; }
        public decimal VolumeOrWeight { get; set; }
        public string Unit { get; set; } = null!;
        public int ManufacturerID { get; set; }
        public string ManufacturerName { get; set; } = null!;
        public int CategoryId { get; set; }

        // КБЖУ
        public decimal? CaloriesKcal { get; set; }
        public decimal? ProteinG { get; set; }
        public decimal? FatG { get; set; }
        public decimal? CarbsG { get; set; }
    }
}
