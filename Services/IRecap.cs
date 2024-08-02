using OrdersRecap.Models;

namespace OrdersRecap.Services
{
    public interface IRecap
    {
        Task OrganizeFiles(DataContainer dataContainer);
    }
}
