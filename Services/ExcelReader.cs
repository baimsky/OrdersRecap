using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using OrdersRecap.Models;
using System.Data;

namespace OrdersRecap.Services
{
    public class ExcelReader
    {
        public List<Record> ReadExcelFile(string filePath)
        {
            List<Record> records = new List<Record>();
            Record record = new Record();

            //List<string> columnOValues = new List<string>();
            //List<string> columnRValues = new List<string>();

            using (SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, false))
            {
                WorkbookPart workbookPart = document.WorkbookPart;
                Sheet sheet = workbookPart.Workbook.Sheets.FirstOrDefault() as Sheet;
                WorksheetPart worksheetPart = workbookPart.GetPartById(sheet.Id.Value) as WorksheetPart;
                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                int columnOIndex = 14; // Column O is the 15th column (zero-based index 14)
                int columnRIndex = 17; // Column R is the 18th column (zero-based index 17)

                foreach (Row row in sheetData.Elements<Row>().Skip(1)) // Skip the header row
                {
                    Cell cellO = row.Elements<Cell>().ElementAtOrDefault(columnOIndex);
                    Cell cellR = row.Elements<Cell>().ElementAtOrDefault(columnRIndex);

                    string valueO = GetCellValue(document, cellO);
                    string valueR = GetCellValue(document, cellR);

                    records.Add(new Record
                    {
                        tempProduct = valueO,
                        tempQty = valueR
                    });

                    //columnOValues.Add(valueO);
                    //columnRValues.Add(valueR);
                }
            }

            return records;
        }

        private string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            if (cell == null)
                return string.Empty;

            string value = cell.InnerText;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                SharedStringTablePart stringTable = document.WorkbookPart.SharedStringTablePart;
                value = stringTable.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
            }

            return value;
        }

        public static void ExportToExcel(List<SummaryRecord> summaryRecords, string filePath)
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                // Create the workbook and worksheet
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                // Add Sheets to the workbook
                Sheets sheets = document.WorkbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet()
                {
                    Id = document.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "SummaryRecords"
                };
                sheets.Append(sheet);

                // Add data to the worksheet
                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                // Add header row
                Row headerRow = new Row();
                headerRow.Append(
                    new Cell() { CellValue = new CellValue("No"), DataType = CellValues.String },
                    new Cell() { CellValue = new CellValue("Variant Name"), DataType = CellValues.String },
                    new Cell() { CellValue = new CellValue("Variant Number"), DataType = CellValues.String },
                    new Cell() { CellValue = new CellValue("SubVariant"), DataType = CellValues.String },
                    new Cell() { CellValue = new CellValue("Total Quantity"), DataType = CellValues.String }
                );
                sheetData.Append(headerRow);

                // Add data rows
                foreach (var record in summaryRecords)
                {
                    Row dataRow = new Row();
                    dataRow.Append(
                        new Cell() { CellValue = new CellValue(record.No.ToString()), DataType = CellValues.Number },
                        new Cell() { CellValue = new CellValue(record.VariantName), DataType = CellValues.String },
                        new Cell() { CellValue = new CellValue(record.VariantNumber), DataType = CellValues.String },
                        new Cell() { CellValue = new CellValue(record.SubVariant), DataType = CellValues.String },
                        new Cell() { CellValue = new CellValue(record.TotalQuantity.ToString()), DataType = CellValues.Number }
                    );
                    sheetData.Append(dataRow);
                }
            }
        }
    }
}
