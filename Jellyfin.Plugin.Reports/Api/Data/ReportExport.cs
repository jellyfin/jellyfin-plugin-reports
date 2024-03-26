#nullable disable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Jellyfin.Plugin.Reports.Api.Model;
using ClosedXML.Excel;

namespace Jellyfin.Plugin.Reports.Api.Data
{
    /// <summary> A report export. </summary>
    public static class ReportExport
    {
        /// <summary> Export to CSV. </summary>
        /// <param name="reportResult"> The report result. </param>
        /// <returns> A MemoryStream containing a CSV file. </returns>
        public static MemoryStream ExportToCsv(ReportResult reportResult)
        {
            static string EscapeText(string text)
            {
                string escapedText = text.Replace("\"", "\"\"", System.StringComparison.Ordinal);
                return text.IndexOfAny(['"', ',', '\n', '\r']) == -1 ? escapedText : $"\"{escapedText}\"";
            }
            static void AppendRows(StreamWriter writer, List<ReportRow> rows)
            {
                foreach (ReportRow row in rows)
                {
                    writer.WriteLine(string.Join(',', row.Columns.Select(s => EscapeText(s.Name))));
                }
            }

            MemoryStream memoryStream = new MemoryStream();
            using (StreamWriter writer = new StreamWriter(memoryStream, leaveOpen:true))
            {
                writer.WriteLine(string.Join(',', reportResult.Headers.Select(s => EscapeText(s.Name))));

                if (reportResult.IsGrouped)
                {
                    foreach (ReportGroup group in reportResult.Groups)
                    {
                        AppendRows(writer, group.Rows);
                    }
                }
                else
                {
                    AppendRows(writer, reportResult.Rows);
                }
            }
            memoryStream.Position = 0;
            return memoryStream;
        }


        /// <summary> Export to HTML. </summary>
        /// <param name="reportResult"> The report result. </param>
        /// <returns> A MemoryStream containing a HTML file. </returns>
        public static MemoryStream ExportToHtml(ReportResult reportResult)
        {
            static void ExportToHtmlRows(StreamWriter writer, List<ReportRow> rows)
            {
                foreach (ReportRow row in rows)
                {
                    writer.Write("<tr>");
                    foreach (ReportItem x in row.Columns)
                    {
                        writer.Write($"<td>{WebUtility.HtmlEncode(x.Name)}</td>");
                    }
                    writer.Write("</tr>");
                }
            }

            const string Html = @"<!DOCTYPE html>
                <html xmlns='http://www.w3.org/1999/xhtml'>
                <head>
                    <meta charset='utf-8'>
                    <title>Jellyfin Reports Export</title>
                    <style type='text/css'>
                        body {
                            font-family: Arial;
                            font-size: 12px;
                        }
                        table.gridtable {
                            color: #333333;
                            border-width: 0.1pt;
                            border-color: #666666;
                            border-collapse: collapse;
                        }
                        table.gridtable th, table.gridtable td {
                            border-width: 0.1pt;
                            padding: 8px;
                            border-style: solid;
                            border-color: #666666;
                        }
                        table.gridtable th {
                            background-color: #dedede;
                        }
                        table.gridtable td {
                            background-color: #ffffff;
                        }
                    </style>
                </head>
                <body>";

            MemoryStream memoryStream = new MemoryStream();
            using (StreamWriter writer = new StreamWriter(memoryStream, leaveOpen: true))
            {
                writer.Write(Html);
                writer.Write("<table  class='gridtable'><tr>");
                foreach (ReportHeader x in reportResult.Headers)
                {
                    writer.Write($"<th>{WebUtility.HtmlEncode(x.Name)}</th>");
                }
                writer.Write("</tr>");

                if (reportResult.IsGrouped)
                {
                    foreach (ReportGroup group in reportResult.Groups)
                    {
                        string groupName = string.IsNullOrEmpty(group.Name) ? "&nbsp;" : WebUtility.HtmlEncode(group.Name);
                        writer.Write($"<tr><th colspan='{reportResult.Headers.Count}'>{groupName}</th></tr>");
                        ExportToHtmlRows(writer, group.Rows);
                        writer.Write($"<tr><td colspan='{reportResult.Headers.Count}'>&nbsp;</td></tr>");
                    }
                }
                else
                {
                    ExportToHtmlRows(writer, reportResult.Rows);
                }
                writer.Write("</table></body></html>");
            }
            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary> Export to Excel. </summary>
        /// <param name="reportResult"> The report result. </param>
        /// <returns> A MemoryStream containing a XLSX file. </returns>
        public static MemoryStream ExportToExcel(ReportResult reportResult)
        {
            static void AddHeaderStyle(IXLRange range)
            {
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                range.Style.Font.Bold = true;
                range.Style.Fill.BackgroundColor = XLColor.FromArgb(222, 222, 222);
            }

            static void AddReportRows(IXLWorksheet worksheet, List<ReportRow> reportRows, ref int nextRow)
            {
                IEnumerable<string[]> rows = reportRows.Select(r => r.Columns.Select(s => s.Name).ToArray());
                worksheet.Cell(nextRow, 1).InsertData(rows);
                nextRow += rows.Count();
            }

            using var workbook = new XLWorkbook(XLEventTracking.Disabled);
            IXLWorksheet worksheet = workbook.Worksheets.Add("ReportExport");

            // Add report rows
            int nextRow = 1;
            IEnumerable<string> headers = reportResult.Headers.Select(s => s.Name);
            IXLRange headerRange = worksheet.Cell(nextRow++, 1).InsertData(headers, true);
            AddHeaderStyle(headerRange);
            if (reportResult.IsGrouped)
            {
                foreach (ReportGroup group in reportResult.Groups)
                {
                    int groupHeaderRow = nextRow++;
                    worksheet.Cell(groupHeaderRow, 1).Value = group.Name;
                    AddHeaderStyle(worksheet.Cell(groupHeaderRow, 1).AsRange());
                    worksheet.Range(groupHeaderRow, 1, groupHeaderRow, reportResult.Headers.Count).Merge();
                    AddReportRows(worksheet, group.Rows, ref nextRow);
                    worksheet.Rows(groupHeaderRow + 1, nextRow - 1).Group();
                }
            }
            else
            {
                AddReportRows(worksheet, reportResult.Rows, ref nextRow);
            }

            // Sheet properties
            worksheet.Style.Font.FontColor = XLColor.FromArgb(51, 51, 51);
            worksheet.Style.Font.FontName = "Arial";
            worksheet.Style.Font.FontSize = 9;
            worksheet.ShowGridLines = false;
            worksheet.SheetView.FreezeRows(1);
            worksheet.Outline.SummaryVLocation = XLOutlineSummaryVLocation.Top;
            worksheet.RangeUsed().Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.RangeUsed().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            //worksheet.ColumnsUsed().AdjustToContents(10.0, 50.0);

            // Workbook properties
            workbook.Properties.Author = "Jellyfin";
            workbook.Properties.Title = "ReportExport";
            string pluginVer = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            workbook.Properties.Comments = $"Produced by Jellyfin Reports Plugin {pluginVer}";

            // Save workbook to stream and return
            MemoryStream memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
