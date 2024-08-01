namespace OrdersRecap.Models
{
    public class Masters
    {
        public List<Master> masters { get; set; }
    }

    public class Master
    {
        public string Code { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public int BaseQuantity { get; set; }
        public string PaperType { get; set; }
        public List<Detail> Details { get; set; }
    }

    public class Detail
    {
        public string SubVariant { get; set; }
        public string PaperType { get; set; }
    }
}
