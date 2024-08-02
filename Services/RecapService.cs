using DocumentFormat.OpenXml.VariantTypes;
using OrdersRecap.Models;
using System.IO.Compression;

namespace OrdersRecap.Services
{
    public class RecapService : IRecap
    {
        private readonly string _catalogPath;
        private readonly string _recapPath;
        private readonly IMaster _masterService;

        public RecapService(IConfiguration configuration, IMaster masterService)
        {
            _catalogPath = configuration["CatalogPath"];
            _recapPath = configuration["RecapPath"];
            _masterService = masterService;
        }

        public async Task OrganizeFiles(DataContainer dataContainer)
        {
            var mastersData = await _masterService.GetMasterDataAsync();
            var mastersDictionary = mastersData.masters.ToDictionary(m => m.Code);

            string todayDate = DateTime.Now.ToString("yyyyMMdd");
            string recapDatePath = Path.Combine(_recapPath, todayDate);

            // Create the base directory for today's date
            Directory.CreateDirectory(recapDatePath);

            foreach (var record in dataContainer.summaryRecords)
            {
                string pcsFolder = $"{record.TotalPcs} PCS";

                string destPath;
                mastersDictionary.TryGetValue(record.Variant, out var masterData);
                Detail? detail = null;
                if (masterData != null && masterData.Details != null)
                {
                    detail = !string.IsNullOrEmpty(record.SubVariant)
                                ? masterData.Details.FirstOrDefault(d => d.SubVariant == record.SubVariant)
                                : masterData.Details.FirstOrDefault();
                }

                if (string.IsNullOrEmpty(record.SubVariant))
                {
                    // If SubVariant is empty, create directory structure: /X PCS/filename.jpg
                    destPath = Path.Combine(recapDatePath, pcsFolder);
                }
                else
                {
                    if (detail == null)
                    {
                        destPath = Path.Combine(recapDatePath, pcsFolder);
                    }
                    else
                    {
                        // If SubVariant is not empty, keep the original structure
                        string subVariantFolder = record.SubVariant;
                        destPath = Path.Combine(recapDatePath, pcsFolder, subVariantFolder);
                    }
                }

                // Create the directory structure if it doesn't exist
                Directory.CreateDirectory(destPath);

                // Find and copy the image file
                string sourceFileName = $"{record.Variant}.jpg";
                string sourcePath = Path.Combine(_catalogPath, sourceFileName);

                if (File.Exists(sourcePath))
                {
                    string destFilePath = Path.Combine(destPath, sourceFileName);
                    File.Copy(sourcePath, destFilePath, true);  // 'true' to overwrite if file exists
                }
                else
                {
                    // Log that the file wasn't found
                    Console.WriteLine($"Warning: File not found - {sourcePath}");
                }
            }

            // After creating all folders and copying files, create the zip file
            CreateZipFile(recapDatePath, todayDate);
        }

        private void CreateZipFile(string sourcePath, string todayDate)
        {
            string zipPath = Path.Combine(_recapPath, $"{todayDate}.zip");

            try
            {
                // Delete the zip file if it already exists
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                // Create the zip file
                ZipFile.CreateFromDirectory(sourcePath, zipPath);

                Console.WriteLine($"Successfully created zip file: {zipPath}");

                // Optionally, delete the original folder after zipping
                // Directory.Delete(sourcePath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating zip file: {ex.Message}");
            }
        }
    }
}
