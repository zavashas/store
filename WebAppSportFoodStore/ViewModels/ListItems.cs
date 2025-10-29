namespace WebAppSportFoodStore.ViewModels
{
    public sealed class ProductListItemVM
    {
        public int IdProduct { get; set; }
        public string Name { get; set; } = "";
        public string CategoryName { get; set; } = "-";
        public string ManufacturerName { get; set; } = "-";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsAvailable { get; set; }
    }

    public sealed class UserListItemVM
    {
        public int IdUser { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string RoleName { get; set; } = "-";
    }

    public sealed class CategoryListItemVM
    {
        public int IdCategory { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class ManufacturerListItemVM
    {
        public int IdManufacturer { get; set; }
        public string Name { get; set; } = "";
    }
}
