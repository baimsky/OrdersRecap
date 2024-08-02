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
        private readonly IRecap _recapService;

        public RecapController(
            IExcelReader excelReader,
            ILogger<RecapController> logger,
            IConfiguration configuration,
            IStock stockService,
            IMaster masterService,
            IRecap recapService)
        {
            _excelReader = excelReader;
            _logger = logger;
            _configuration = configuration;
            _stockService = stockService;
            _masterService = masterService;
            _recapService = recapService;
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

            //DataContainer dataContainer = new DataContainer();
            var dataContainer = await Task.FromResult(new DataContainer());

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
                var dataContainer = await ProcessShopeeDataAsync(tempData);
                ViewBag.DataList = dataContainer;
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

            ViewBag.Message = "Files are successfully uploaded";

            return filePath;
        }

        private async Task<DataContainer> ProcessShopeeDataAsync(List<Record> tempData)
        {
            var mastersData = await _masterService.GetMasterDataAsync();
            var mastersDictionary = mastersData.masters.ToDictionary(m => m.Code);

            /// check Dictionary if master.Code contains duplicate
            //var mastersDictionary = new Dictionary<string, Master>();
            //foreach (var master in mastersData.masters)
            //{
            //    if (!mastersDictionary.TryAdd(master.Code, master))
            //    {
            //        _logger.LogWarning($"Duplicate master code found: {master.Code}");
            //    }
            //}

            var listData = tempData.Select(x =>
            {
                var tmpVariant = x.tempProduct;
                var variantParts = tmpVariant.Split(',');
                var variant = variantParts[0].Trim().Replace(" ", "");
                var tmpSubVariant = variantParts.Length > 1 ? variantParts[1].Trim().Replace(" ", "") : "";
                var match = VariantRegex.Match(variant);

                if (!mastersDictionary.TryGetValue(variant, out var masterDatas))
                {
                    // Handle the case where the variant is not found in the master data
                    //return null;
                    _logger.LogWarning($"Variant not found in master data: {variant}");
                }

                // Try to get masterData, but proceed even if it's not found
                mastersDictionary.TryGetValue(variant, out var masterData);

                Detail? detail = null;
                if (masterData != null && masterData.Details != null)
                {
                    detail = !string.IsNullOrEmpty(tmpSubVariant)
                                ? masterData.Details.FirstOrDefault(d => d.SubVariant == tmpSubVariant)
                                : masterData.Details.FirstOrDefault();
                }

                return new DataRecord
                {
                    originalVariant = tmpVariant,
                    variant = variant,
                    variantName = match.Groups[1].Value,
                    variantNumber = match.Groups[2].Value,
                    subVariant = detail?.SubVariant ?? tmpSubVariant ?? "",
                    quantity = Convert.ToInt32(x.tempQty),
                    baseQuantity = masterData?.BaseQuantity ?? 1, 
                    paperType = detail?.PaperType ?? masterData?.PaperType ?? "Unknown"
                };
            })
            //.Where(d => d != null)
            .ToList();

            var dataContainer = new DataContainer
            {
                summaryRecords = listData
                    .GroupBy(d => new { d.variant, d.subVariant })
                    .Select(g => new SummaryRecord
                    {
                        Variant = g.Key.variant,
                        SubVariant = g.Key.subVariant,
                        TotalQuantity = g.Sum(d => d.quantity),
                        TotalPcs = g.Sum(d => d.quantity * d.baseQuantity),
                        PaperType = g.First().paperType
                    })
                    .OrderBy(s => s.SubVariant)
                    .ThenBy(s => s.TotalPcs)
                    .ThenBy(s => s.Variant)
                    .ToList()
            };

            dataContainer.SD = dataContainer.summaryRecords.Where(d => IsSidu(d.SubVariant) || IsSidu(d.Variant)).Sum(d => d.TotalQuantity);
            dataContainer.BB = dataContainer.summaryRecords.Where(d => IsBigboss(d.SubVariant) || IsBigboss(d.Variant)).Sum(d => d.TotalQuantity);
            dataContainer.SDpcs = dataContainer.summaryRecords.Where(d => IsSidu(d.SubVariant) || IsSidu(d.Variant)).Sum(d => d.TotalPcs);
            dataContainer.BBpcs = dataContainer.summaryRecords.Where(d => IsBigboss(d.SubVariant) || IsBigboss(d.Variant)).Sum(d => d.TotalPcs);

            return dataContainer;
        }

        private bool IsSiduOrBigboss(string value) => IsSidu(value) || IsBigboss(value);
        private bool IsSidu(string value) => value.Contains("Sidu", StringComparison.OrdinalIgnoreCase);
        private bool IsBigboss(string value) => value.Contains("Bigboss", StringComparison.OrdinalIgnoreCase);
        private static readonly Regex VariantRegex = new Regex(@"([A-Za-z]+)(\d*)", RegexOptions.Compiled);


        [HttpPost]
        public async Task<IActionResult> Recap(string DataListJson)
        {
            DataContainer? dataContainer = JsonConvert.DeserializeObject<DataContainer>(DataListJson);
            await _recapService.OrganizeFiles(dataContainer);
            ViewBag.DataList = dataContainer;
            return View("Shopee", dataContainer);
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
