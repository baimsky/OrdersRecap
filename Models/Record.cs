namespace OrdersRecap.Models
{
    public class Record
    {
        public string? tempProduct { get; set; }
        public string? tempQty { get; set; }
    }

    public class DataRecord
    {
        public int no { get; set; }
        public string? originalVariant { get; set; }
        public string? variant { get; set; }
        public string? variantName { get; set; }
        public string? variantNumber { get; set; }
        public string? subVariant { get; set; }
        public int quantity { get; set; }
    }

    public class SummaryRecord
    {
        public int No { get; set; }
        public string? OriginalVariant { get; set; }
        public string? Variant { get; set; }
        public string? VariantName { get; set; }
        public string? VariantNumber { get; set; }
        public string? SubVariant { get; set; }
        public int TotalQuantity { get; set; }
    }

    public class Sampul
    {
        public int SD { get; set; }
        public int BB { get; set; }
    }

    public class DataContainer
    {
        public int SD { get; set; }
        public int BB { get; set; }
        public IList<DataRecord> dataRecords { get; set; }
        public IList<SummaryRecord> summaryRecords { get; set; }

        public DataContainer()
        {
            dataRecords = new List<DataRecord>();
            summaryRecords = new List<SummaryRecord>();
        }
    }
}
