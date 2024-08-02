using Newtonsoft.Json;
using OrdersRecap.Models;

namespace OrdersRecap.Services
{
    public class MasterService : IMaster
    {
        private readonly IConfiguration _configuration;
        private readonly string _masterFilePath;

        public MasterService(IConfiguration configuration)
        {
            _configuration = configuration;
            _masterFilePath = _configuration["MasterFilePath"];
        }

        public async Task<Masters> GetMasterDataAsync()
        {
            var jsonData = await File.ReadAllTextAsync(_masterFilePath);
            var mastersData = JsonConvert.DeserializeObject<Masters>(jsonData);
            mastersData.masters = mastersData.masters.OrderBy(x => x.Code).ToList();
            return mastersData;
        }
    }
}
