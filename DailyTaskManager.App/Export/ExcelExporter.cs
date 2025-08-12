
using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Export
{
    public class ExcelExporter
    {
        public void ExportToExcel(IEnumerable<DailyTask> tasks)
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Report");

            ws.Cell(1, 1).Value = "Red br";
            ws.Cell(1, 2).Value = "Kratak opis aktivnosti";
            ws.Cell(1, 3).Value = "Datum";
            ws.Cell(1, 4).Value = "Broj sati";

            int row = 2;
            int total = 0;
            int index = 1;

            foreach (var task in tasks)
            {
                ws.Cell(row, 1).Value = index++;
                ws.Cell(row, 2).Value = task.Description;
                ws.Cell(row, 3).Value = task.Date.ToString("d. M. yyyy.");
                ws.Cell(row, 4).Value = task.Hours;
                total += task.Hours;
                row++;
            }

            ws.Cell(row, 3).Value = "Ukupno sati";
            ws.Cell(row, 4).Value = total;

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"TaskReport_{DateTime.Now:yyyy_MM}.xlsx");
            wb.SaveAs(path);
        }
    }
}
