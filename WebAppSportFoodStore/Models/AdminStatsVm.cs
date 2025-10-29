namespace WebAppSportFoodStore.Models
{
    public class AdminStatsVm
    {
        public string PeriodTitle { get; set; }

        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int UniqueCustomers { get; set; }

        public List<string> OrdersLabels { get; set; } = new();
        public List<int> OrdersCounts { get; set; } = new();

        public List<string> RevenueLabels { get; set; } = new();
        public List<decimal> RevenueValues { get; set; } = new();

        public List<string> TopProductsLabels { get; set; } = new();
        public List<int> TopProductsQty { get; set; } = new();

        public string GroupBy { get; set; } = "day";
        public int TopN { get; set; } = 10;
    }
}
