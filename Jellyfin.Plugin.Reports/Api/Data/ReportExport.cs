#nullable disable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Jellyfin.Plugin.Reports.Api.Model;

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
                return text.IndexOfAny(new char[4] { '"', ',', '\n', '\r' }) == -1 ? escapedText : $"\"{escapedText}\"";
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
        /// <returns> A  MemoryStream containing a HTML file. </returns>
        public static MemoryStream ExportToHtml(ReportResult reportResult)
        {
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

        private static void ExportToHtmlRows(StreamWriter writer, List<ReportRow> rows)
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
    }
}
