#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jellyfin.Plugin.Reports.Api.Model;

namespace Jellyfin.Plugin.Reports.Api.Data
{
    /// <summary> A report export. </summary>
    public static class ReportExport
    {
        /// <summary> Export to CSV. </summary>
        /// <param name="reportResult"> The report result. </param>
        /// <returns> A string. </returns>
        public static string ExportToCsv(ReportResult reportResult)
        {
            static string EscapeText(string text)
            {
                string escapedText = text.Replace("\"", "\"\"", System.StringComparison.Ordinal);
                return text.IndexOfAny(new char[4] { '"', ',', '\n', '\r' }) == -1 ? escapedText : '"' + escapedText + '"';
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

            return returnValue.ToString();
        }


        /// <summary> Export to excel. </summary>
        /// <param name="reportResult"> The report result. </param>
        /// <returns> A string. </returns>
        public static string ExportToExcel(ReportResult reportResult)
        {
            const string Style = @"<style type='text/css'>
                            BODY {
                                    font-family: Arial;
                                    font-size: 12px;
                                }

                                TABLE {
                                    font-family: Arial;
                                    font-size: 12px;
                                }

                                A {
                                    font-family: Arial;
                                    color: #144A86;
                                    font-size: 12px;
                                    cursor: pointer;
                                    text-decoration: none;
                                    font-weight: bold;
                                }
                                DIV {
                                    font-family: Arial;
                                    font-size: 12px;
                                    margin-bottom: 0px;
                                }
                                P, LI, DIV {
                                    font-size: 12px;
                                    margin-bottom: 0px;
                                }

                                P, UL {
                                    font-size: 12px;
                                    margin-bottom: 6px;
                                    margin-top: 0px;
                                }

                                H1 {
                                    font-size: 18pt;
                                }

                                H2 {
                                    font-weight: bold;
                                    font-size: 14pt;
                                    COLOR: #C0C0C0;
                                }

                                H3 {
                                    font-weight: normal;
                                    font-size: 14pt;
                                    text-indent: +1em;
                                }

                                H4 {
                                    font-size: 10pt;
                                    font-weight: normal;
                                }

                                H5 {
                                    font-size: 10pt;
                                    font-weight: normal;
                                    background: #A9A9A9;
                                    COLOR: white;
                                    display: inline;
                                }

                                H6 {
                                    padding: 2 1 2 5;
                                    font-size: 11px;
                                    font-weight: bold;
                                    text-decoration: none;
                                    margin-bottom: 1px;
                                }

                                UL {
                                    line-height: 1.5em;
                                    list-style-type: disc;
                                }

                                OL {
                                    line-height: 1.5em;
                                }

                                LI {
                                    line-height: 1.5em;
                                }

                                A IMG {
                                    border: 0;
                                }

                                table.gridtable {
                                    color: #333333;
                                    border-width: 0.1pt;
                                    border-color: #666666;
                                    border-collapse: collapse;
                                }

                                table.gridtable th {
                                    border-width: 0.1pt;
                                    padding: 8px;
                                    border-style: solid;
                                    border-color: #666666;
                                    background-color: #dedede;
                                }
                                table.gridtable tr {
                                    background-color: #ffffff;
                                }
                                table.gridtable td {
                                    border-width: 0.1pt;
                                    padding: 8px;
                                    border-style: solid;
                                    border-color: #666666;
                                    background-color: #ffffff;
                                }
                        </style>";

            string Html = @"<!DOCTYPE html>
                            <html xmlns='http://www.w3.org/1999/xhtml'>
                            <head>
                            <meta http-equiv='X-UA-Compatible' content='IE=8, IE=9, IE=10' />
                            <meta charset='utf-8'>
                            <title>Jellyfin Reports Export</title>";
            Html += "\n" + Style + "\n";
            Html += "</head>\n";
            Html += "<body>\n";

            StringBuilder returnValue = new StringBuilder();
            returnValue.AppendLine("<table  class='gridtable'>");
            returnValue.AppendLine("<tr>");
            foreach (var x in reportResult.Headers)
            {
                returnValue.Append("<th>")
                    .Append(x.Name)
                    .AppendLine("</th>");
            }

            returnValue.AppendLine("</tr>");

            if (reportResult.IsGrouped)
            {
                foreach (ReportGroup group in reportResult.Groups)
                {
                    returnValue.AppendLine("<tr>");
                    returnValue.Append("<th scope='rowgroup' colspan='")
                        .Append(reportResult.Headers.Count)
                        .Append("'>")
                        .Append(string.IsNullOrEmpty(group.Name) ? "&nbsp;" : group.Name)
                        .AppendLine("</th>");
                    returnValue.AppendLine("</tr>");
                    ExportToExcelRows(returnValue, group.Rows);
                    returnValue.AppendLine("<tr>");
                    returnValue.Append("<th style='background-color: #ffffff;' scope='rowgroup' colspan='")
                        .Append(reportResult.Headers.Count)
                        .AppendLine("'>" + "&nbsp;" + "</th>");
                    returnValue.AppendLine("</tr>");
                }
            }
            else
            {
                ExportToExcelRows(returnValue, reportResult.Rows);
            }

            returnValue.AppendLine("</table>");

            Html += returnValue.ToString();
            Html += "</body>";
            Html += "</html>";
            return Html;
        }

        private static void ExportToExcelRows(
            StringBuilder returnValue,
            List<ReportRow> rows)
        {
            foreach (var row in rows)
            {
                returnValue.AppendLine("<tr>");
                foreach (var x in row.Columns)
                {
                    returnValue.Append("<td>")
                        .Append(x.Name)
                        .AppendLine("</td>");
                }

                returnValue.AppendLine("</tr>");
            }
        }
    }
}
