using Newtonsoft.Json;
using OrdersRecap.Models;

namespace OrdersRecap.Services
{
    public class StockService : IStock
    {
        private readonly IConfiguration _configuration;
        private readonly string _stockFilePath;

        public StockService(IConfiguration configuration)
        {
            _configuration = configuration;
            _stockFilePath = _configuration["StockFilePath"];
        }

        public async Task<List<Stock>> GetStocksAsync()
        {
            var jsonData = await File.ReadAllTextAsync(_stockFilePath);
            var stocksData = JsonConvert.DeserializeObject<Stocks>(jsonData);
            return (List<Stock>)(stocksData?.stocks ?? new List<Stock>());
        }

        public async Task SaveStocksAsync(List<Stock> stocks)
        {
            var stocksData = new Stocks { stocks = stocks };
            var jsonData = JsonConvert.SerializeObject(stocksData, Formatting.Indented);
            await File.WriteAllTextAsync(_stockFilePath, jsonData);
        }
    }
}
