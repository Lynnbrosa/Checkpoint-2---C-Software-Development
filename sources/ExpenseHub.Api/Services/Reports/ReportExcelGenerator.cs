using System;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services.Reports;

internal static class ReportExcelGenerator
{
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static ReportExcelFile Generate(ReportJob job, ReportSummary summary)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(summary);

        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add("Relatório");

        sheet.Cell(1, 1).Value = "ReportId";
        sheet.Cell(1, 2).Value = job.Id.ToString();

        sheet.Cell(2, 1).Value = "Scope";
        sheet.Cell(2, 2).Value = job.Scope.ToString();

        sheet.Cell(3, 1).Value = "FromDate";
        sheet.Cell(3, 2).Value = job.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        sheet.Cell(4, 1).Value = "ToDate";
        sheet.Cell(4, 2).Value = job.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        sheet.Cell(5, 1).Value = "GeneratedAt";
        sheet.Cell(5, 2).Value = summary.GeneratedAt.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        sheet.Cell(7, 1).Value = "Status";
        sheet.Cell(7, 2).Value = "Count";
        sheet.Cell(7, 3).Value = "Amount";

        int row = 8;
        foreach (ReportStatusSummary item in summary.ByStatus)
        {
            sheet.Cell(row, 1).Value = item.Status.ToString();
            sheet.Cell(row, 2).Value = item.Count;
            sheet.Cell(row, 3).Value = item.Amount;
            sheet.Cell(row, 3).Style.NumberFormat.Format = "0.00";
            row++;
        }

        sheet.Cell(row, 1).Value = "Total";
        sheet.Cell(row, 2).Value = summary.TotalCount;
        sheet.Cell(row, 3).Value = summary.TotalAmount;
        sheet.Cell(row, 3).Style.NumberFormat.Format = "0.00";

        using MemoryStream stream = new();
        workbook.SaveAs(stream);

        return new ReportExcelFile
        {
            FileName = $"report-{job.Id}.xlsx",
            ContentType = ExcelContentType,
            Content = stream.ToArray(),
        };
    }
}

