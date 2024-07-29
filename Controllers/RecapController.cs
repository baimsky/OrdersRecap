using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OrdersRecap.Models;
using OrdersRecap.Services;
using System.Data;
using System.Text.RegularExpressions;

namespace OrdersRecap.Controllers
{
    public class RecapController : Controller
    {
        private readonly IExcelReader _excelReader;
        private readonly ILogger<RecapController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IStock _stockService;
        private readonly IMaster _masterService;

        public RecapController(
            IExcelReader excelReader,
            ILogger<RecapController> logger,
            IConfiguration configuration,
            IStock stockService,
            IMaster masterService)
        {
            _excelReader = excelReader;
            _logger = logger;
            _configuration = configuration;
            _stockService = stockService;
            _masterService = masterService;
        }

        public IActionResult Index() => View();

        public async Task<IActionResult> Shopee() //string sortOrder, string currentFilter, string searchString, int? pageNumber
        {
            //ViewData["CurrentSort"] = sortOrder;
            //ViewData["VariantParm"] = String.IsNullOrEmpty(sortOrder) ? "Variant" : "";
            //ViewData["SubVariantParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            //ViewData["QuantityParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            //if (searchString != null)
            //{
            //    pageNumber = 1;
            //}
            //else
            //{
            //    searchString = currentFilter;
            //}

            //ViewData["CurrentFilter"] = searchString;

            //List<SummaryRecord> summary = new List<SummaryRecord>();
            DataContainer dataContainer = new DataContainer();

            //if (!String.IsNullOrEmpty(searchString))
            //{
            //    summary = summary.Where(s => s.LastName.Contains(searchString)
            //                           || s.FirstMidName.Contains(searchString));
            //}

            //switch (sortOrder)
            //{
            //    case "name_desc":
            //        summary = summary.OrderByDescending(s => s.LastName);
            //        break;
            //    case "Date":
            //        summary = summary.OrderBy(s => s.EnrollmentDate);
            //        break;
            //    case "date_desc":
            //        summary = summary.OrderByDescending(s => s.EnrollmentDate);
            //        break;
            //    default:
            //        summary = summary.OrderBy(s => s.LastName);
            //        break;
            //}

            //int pageSize = 3;
            //return View(await PaginatedList<Student>.CreateAsync(students.AsNoTracking(), pageNumber ?? 1, pageSize));

            return View(dataContainer);
        }

        public async Task<IActionResult> MasterData()
        {
            var mastersData = await _masterService.GetMasterDataAsync();
            return View(mastersData);
        }

        [HttpPost]
        public async Task<IActionResult> Shopee(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty.");

            try
            {
                string filePath = await SaveFileAsync(file);
                var tempData = await _excelReader.ReadExcelFileAsync(filePath);
                var dataContainer = ProcessShopeeData(tempData);
                return View(dataContainer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Shopee data");
                return StatusCode(500, "An error occurred while processing the file.");
            }
        }

        private async Task<string> SaveFileAsync(IFormFile file)
        {
            string uploadsFolder = _configuration["UploadFilePath"];
            string filePath = Path.Combine(uploadsFolder, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }

        private DataContainer ProcessShopeeData(List<Record> tempData)
        {
            List<DataRecord> listData = new List<DataRecord>();

            foreach (var x in tempData)
            {
                string tmpVariant = x.tempProduct;
                int tmpQty = Convert.ToInt32(x.tempQty);

                DataRecord data = new DataRecord();
                data.originalVariant = tmpVariant;
                data.variant = tmpVariant.Contains(',') ? tmpVariant.Split(',')[0].Trim().Replace(" ", "") : tmpVariant;

                var regex = new Regex(@"([A-Za-z]+)(\d*)");
                var match = regex.Match(data.variant);
                data.variantName = match.Groups[1].Value;
                data.variantNumber = match.Groups[2].Value;

                data.subVariant = tmpVariant.Contains(',') ? tmpVariant.Split(',')[1] : "";
                data.quantity = tmpQty;
                listData.Add(data);
            }

            var dataContainer = new DataContainer();
            dataContainer.summaryRecords = listData
                .GroupBy(d => new { d.variant, d.subVariant })
                .Select(g => new SummaryRecord //(g, index)
                {
                    //No = index + 1,
                    Variant = g.Key.variant,
                    SubVariant = g.Key.subVariant,
                    TotalQuantity = g.Sum(d => d.quantity),
                    TotalPcs = g.Sum(d => (d.subVariant.Contains("Sidu") || d.subVariant.Contains("Bigboss") || d.variant.Contains("Sidu") || d.variant.Contains("Bigboss")) ? d.quantity * 6 : d.quantity * 1)
                })
                .OrderBy(s => s.SubVariant)
                .ThenBy(s => s.TotalPcs)
                .ThenBy(s => s.Variant)
                .ToList();

            dataContainer.SD = dataContainer.summaryRecords.Where(d => d.SubVariant.Contains("Sidu") || d.Variant.Contains("Sidu")).Sum(d => d.TotalQuantity);
            dataContainer.BB = dataContainer.summaryRecords.Where(d => d.SubVariant.Contains("Bigboss") || d.Variant.Contains("Bigboss")).Sum(d => d.TotalQuantity);
            dataContainer.SDpcs = (dataContainer.SD) * 6;
            dataContainer.BBpcs = (dataContainer.BB) * 6;

            return dataContainer;
        }

        public async Task<IActionResult> Stock()
        {
            var stocks = await _stockService.GetStocksAsync();
            return View(stocks);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(List<Stock> stocks)
        {
            await _stockService.SaveStocksAsync(stocks);
            return RedirectToAction("Stock");
        }
    }
}
