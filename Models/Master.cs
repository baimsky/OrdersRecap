namespace OrdersRecap.Models
{
    public class Master
    {
        public string? Code { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
    }

    public class Masters
    {
        public IList<Master> masters { get; set; }
    }
}
