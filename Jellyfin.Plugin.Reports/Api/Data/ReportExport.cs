#nullable disable

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
            static void AppendRows(StringBuilder builder, List<ReportRow> rows)
            {
                foreach (ReportRow row in rows)
                {
                    builder.AppendJoin(',', row.Columns.Select(s => EscapeText(s.Name))).AppendLine();
                }
            }

            StringBuilder returnValue = new StringBuilder();
            returnValue.AppendJoin(',', reportResult.Headers.Select(s => EscapeText(s.Name))).AppendLine();

            if (reportResult.IsGrouped)
            {
                foreach (ReportGroup group in reportResult.Groups)
                {
                    AppendRows(returnValue, group.Rows);
                }
            }
            else
            {
                AppendRows(returnValue, reportResult.Rows);
            }

            MemoryStream memoryStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(memoryStream);
            writer.Write(returnValue);
            writer.Flush();
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

            StringBuilder returnValue = new StringBuilder(Html);
            returnValue.Append("<table  class='gridtable'><tr>");
            foreach (ReportHeader x in reportResult.Headers)
            {
                returnValue.Append(CultureInfo.InvariantCulture, $"<th>{WebUtility.HtmlEncode(x.Name)}</th>");
            }
            returnValue.Append("</tr>");

            if (reportResult.IsGrouped)
            {
                foreach (ReportGroup group in reportResult.Groups)
                {
                    string groupName = string.IsNullOrEmpty(group.Name) ? "&nbsp;" : WebUtility.HtmlEncode(group.Name);
                    returnValue.Append(CultureInfo.InvariantCulture, $"<tr><th colspan='{reportResult.Headers.Count}'>{groupName}</th></tr>");
                    ExportToHtmlRows(returnValue, group.Rows);
                    returnValue.Append(CultureInfo.InvariantCulture, $"<tr><td colspan='{reportResult.Headers.Count}'>&nbsp;</td></tr>");
                }
            }
            else
            {
                ExportToHtmlRows(returnValue, reportResult.Rows);
            }
            returnValue.Append("</table></body></html>");

            MemoryStream memoryStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(memoryStream);
            writer.Write(returnValue);
            writer.Flush();
            memoryStream.Position = 0;
            return memoryStream;
        }

        private static void ExportToHtmlRows(StringBuilder returnValue, List<ReportRow> rows)
        {
            foreach (ReportRow row in rows)
            {
                returnValue.Append("<tr>");
                foreach (ReportItem x in row.Columns)
                {
                    returnValue.Append(CultureInfo.InvariantCulture, $"<td>{WebUtility.HtmlEncode(x.Name)}</td>");
                }
                returnValue.Append("</tr>");
            }
        }
    }
}
