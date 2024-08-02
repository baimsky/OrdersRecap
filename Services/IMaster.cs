using OrdersRecap.Models;

namespace OrdersRecap.Services
{
    public interface IMaster
    {
        Task<Masters> GetMasterDataAsync();
    }
}
