using OrdersRecap.Models;

namespace OrdersRecap.Services
{
    public interface IExcelReader
    {
        Task<List<Record>> ReadExcelFileAsync(string filePath);
    }
}
