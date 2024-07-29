using OrdersRecap.Models;

namespace OrdersRecap.Services
{
    public interface IStock
    {
        Task<List<Stock>> GetStocksAsync();
        Task SaveStocksAsync(List<Stock> stocks);
    }
}
