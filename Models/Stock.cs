namespace OrdersRecap.Models
{
    public class Stock
    {
        public string? variant { get; set; }
        public string? subVariant { get; set; }
        public int stock { get; set; }
    }

    public class Stocks
    {
        public IList<Stock> stocks { get; set; }
    }
}
