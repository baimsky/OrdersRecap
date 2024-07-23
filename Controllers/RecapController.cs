using DocumentFormat.OpenXml.Vml;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OrdersRecap.Models;
using OrdersRecap.Services;
using System.Data;
using System.Numerics;
using System.Text.RegularExpressions;

namespace OrdersRecap.Controllers
{
    public class RecapController : Controller
    {
        private readonly ExcelReader _excelReader;
        private readonly ILogger<RecapController> _logger;

        public RecapController(ILogger<RecapController> logger)
        {
            _excelReader = new ExcelReader();
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

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

        public IActionResult MasterData()
        {
            using (StreamReader r = new StreamReader("JSON/master.json"))
            {
                string json = r.ReadToEnd();
                var mastersData = JsonConvert.DeserializeObject<Masters>(json);
                mastersData.masters.OrderBy(x => x.Code);
                return View(mastersData);
            }
        }

        public Masters getMasterData()
        {
            Masters masters = new Masters();
            using (StreamReader r = new StreamReader("JSON/master.json"))
            {
                string json = r.ReadToEnd();
                masters = JsonConvert.DeserializeObject<Masters>(json);
                masters.masters.OrderBy(x => x.Code);
                return masters;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Shopee(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty.");

            string filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Attachment", file.FileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            ViewBag.Message = "Files are successfully uploaded";

            List<DataRecord> listData = new List<DataRecord>();
            List<Record> tempData = _excelReader.ReadExcelFile(filePath);

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

            return View(dataContainer);
        }
    }
}
