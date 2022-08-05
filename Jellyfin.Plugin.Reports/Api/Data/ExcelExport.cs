using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using Jellyfin.Plugin.Reports.Api.Model;

namespace Jellyfin.Plugin.Reports.Api.Data
{
    /// <summary> Build XML files and generate XLSX archives from them. </summary>
    public static class ExcelExport
    {
        /// <summary> Creates an XLSX file from <paramref name="reportResult"/> </summary>
        /// <param name="reportResult"> The results of a client query </param>
        /// <returns> A MemoryStream containing a XLSX file. </returns>
        public static MemoryStream GenerateXlsx(ReportResult reportResult)
        {
            // Constant XML files to place into the XLSX archive 
            const string rootRels = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
            const string rootContentTypes = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/></Types>";
            const string workbookRels = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/></Relationships>";
            const string workbookXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"ReportExport\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
            const string styleXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"9\"/><color rgb=\"FF333333\"/><name val=\"Arial\"/><family val=\"2\"/></font><font><b/><sz val=\"9\"/><color rgb=\"FF333333\"/><name val=\"Arial\"/><family val=\"2\"/></font></fonts><fills count=\"3\"><fill></fill><fill></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFDEDEDE\"/><bgColor indexed=\"64\"/></patternFill></fill></fills><borders count=\"2\"><border></border><border><left style=\"thin\"><color rgb=\"FF666666\"/></left><right style=\"thin\"><color rgb=\"FF666666\"/></right><top style=\"thin\"><color rgb=\"FF666666\"/></top><bottom style=\"thin\"><color rgb=\"FF666666\"/></bottom></border></borders><cellXfs count=\"3\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment wrapText=\"1\"/></xf></cellXfs></styleSheet>";

            static void AddStringToArchive(ZipArchive archive, string fileName, string content)
            {
                ZipArchiveEntry file = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                using (Stream entryStream = file.Open())
                using (StreamWriter streamWriter = new StreamWriter(entryStream))
                    streamWriter.Write(content);
            }
            static void AddXmlToArchive(ZipArchive archive, string fileName, ExcelXmlBuilder content)
            {
                ZipArchiveEntry file = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                using (Stream entryStream = file.Open())
                using (StreamWriter streamWriter = new StreamWriter(entryStream))
                    content.WriteXml(streamWriter);
            }

            ExcelSharedString sharedString = new ExcelSharedString();
            ExcelSheet sheetObj = new ExcelSheet(reportResult, sharedString);

            // Add reportResult to ExcelSheet object
            if (reportResult.IsGrouped)
            {
                reportResult.Groups.ForEach(group =>
                {
                    sheetObj.addGroupHeader(group.Name);
                    sheetObj.AddRows(group.Rows);
                });
            }
            else
            {
                sheetObj.AddRows(reportResult.Rows);
            }

            // Write XLSX file
            MemoryStream memoryStream = new MemoryStream();
            using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                AddStringToArchive(archive, "_rels/.rels", rootRels);
                AddStringToArchive(archive, "[Content_Types].xml", rootContentTypes);
                AddStringToArchive(archive, "xl/_rels/workbook.xml.rels", workbookRels);
                AddStringToArchive(archive, "xl/workbook.xml", workbookXml);
                AddStringToArchive(archive, "xl/styles.xml", styleXml);
                AddXmlToArchive(archive, "xl/sharedStrings.xml", sharedString);
                AddXmlToArchive(archive, "xl/worksheets/sheet1.xml", sheetObj);
            }
            memoryStream.Position = 0;
            return memoryStream;
        }


        /// <summary> Abstract superclass to enforce WriteXml method </summary>
        abstract private class ExcelXmlBuilder
        {
            /// <summary> Write ExcelXmlBuilder object contents to a StreamWriter in a XML format </summary>
            /// <param name="writer"> The StreamWriter to write the XML content to </param>
            abstract public void WriteXml(StreamWriter writer);
        }

        /// <summary> XML builder for SharedStrings.xml file </summary>
        private class ExcelSharedString : ExcelXmlBuilder
        {
            private int wordCount;
            private List<string> wordList = new();
            private const string sharedStringXmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"";

            /// <summary> Add a string to the workbook's shared string list (if not already in list), returning its index </summary>
            /// <param name="text"> The string to be added to the shared string list </param>
            /// <returns> Index of string <paramref name="text"/> in shared string list </returns>
            public int AddString(string text)
            {
                int strPos = wordList.IndexOf(text);
                wordCount++;
                if (strPos == -1)
                {
                    wordList.Add(text);
                    return wordList.Count - 1;
                }
                return strPos;
            }

            public override void WriteXml(StreamWriter writer)
            {
                writer.Write(sharedStringXmlHeader);
                writer.Write($"{wordCount}\" uniqueCount=\"{wordList.Count}\">");
                writer.Write(string.Join(null, wordList.Select(word => $"<si><t>{SecurityElement.Escape(word)}</t></si>")));
                writer.Write("</sst>");
            }
        }

        /// <summary> XML builder for sheet XML files (e.g. sheet1.xml) </summary>
        private class ExcelSheet : ExcelXmlBuilder
        {
            private int numCols;
            private int rowCount;
            private bool isGrouped;
            private int[] colWidths;
            private List<int> groupHeaderRows = new();
            private ExcelSharedString sharedString;
            private StringBuilder sheetXml;
            private const int minColWidth = 10;
            private const int maxColWidth = 50;
            private const string sheetXmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetPr><outlinePr summaryBelow=\"0\"/></sheetPr><sheetViews><sheetView showGridLines=\"0\" tabSelected=\"1\" workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><sheetFormatPr defaultRowHeight=\"15\" outlineLevelRow=\"1\"/>";
            private enum RowType
            {
                sheetHeader,
                groupHeader,
                standard
            }
    
            public ExcelSheet(ReportResult reportResult, ExcelSharedString sharedString)
            {
                this.sharedString = sharedString;
                isGrouped = reportResult.IsGrouped;
                numCols = reportResult.Headers.Count;
                colWidths = Enumerable.Repeat(minColWidth, numCols).ToArray();
                sheetXml = new StringBuilder();
                AddRow(reportResult.Headers.Select(s => s.Name).ToArray(), RowType.sheetHeader);
            }

            /// <summary> Add rows to the Excel sheet </summary>
            /// <param name="rows"> A list of ReportRows to be added </param>
            public void AddRows(List<ReportRow> rows)
            {
                rows.ForEach(row =>
                {
                    AddRow(row.Columns.Select(s => s.Name).ToArray(), RowType.standard);
                });
            }

            /// <summary> Add a group header row to the Excel sheet </summary>
            /// <param name="header"> The title to use for the header row </param>
            public void addGroupHeader(string header)
            {
                string[] groupHeaderRow = new string[numCols];
                groupHeaderRow[0] = header;
                AddRow(groupHeaderRow, RowType.groupHeader);
                groupHeaderRows.Add(rowCount);
            }

            public override void WriteXml(StreamWriter writer)
            {
                writer.Write(sheetXmlHeader);
                writer.Write("<cols>");
                writer.Write(string.Join(null, colWidths.Select((width,idx) => $"<col min=\"{idx+1}\" max=\"{idx+1}\" width=\"{width}\" style=\"0\"/>")));
                writer.Write($"</cols><sheetData>");
                writer.Write(sheetXml);
                writer.Write("</sheetData>");
                if (isGrouped)
                {
                    string lastCol = ColIdxToColRef(numCols - 1);
                    writer.Write($"<mergeCells count=\"{groupHeaderRows.Count}\">");
                    writer.Write(string.Join(null, groupHeaderRows.Select(groupRow => $"<mergeCell ref=\"A{groupRow}:{lastCol}{groupRow}\"/>")));
                    writer.Write("</mergeCells>");
                }
                writer.Write("</worksheet>");
            }

            /// <summary> Add a row to the Excel sheet </summary>
            /// <param name="rowVals"> String array of values to fill row with. One per cell, starting with column A </param>
            /// <param name="rowType"> Type of row to add. Whether it's a title, group header, or standard row </param>
            private void AddRow(string[] rowVals, RowType rowType)
            {
                int rowStyle = rowType.Equals(RowType.standard) ? 2 : 1;
                sheetXml.Append(CultureInfo.InvariantCulture, $"<row r=\"{++rowCount}\" spans=\"1:{numCols}\" s=\"{rowStyle}\"");
                if (rowType.Equals(RowType.groupHeader))
                {
                    sheetXml.Append(" collapsed=\"1\"");
                }
                else if (rowType.Equals(RowType.standard) && isGrouped)
                {
                    sheetXml.Append(" hidden=\"1\" outlineLevel=\"1\"");
                }
                sheetXml.Append('>');
                for (int colIdx = 0; colIdx < rowVals.Length; colIdx++)
                {
                    string cellStr = rowVals[colIdx];
                    sheetXml.Append(CultureInfo.InvariantCulture, $"<c r=\"{ColIdxToColRef(colIdx)}{rowCount}\" s=\"{rowStyle}");
                    if (string.IsNullOrWhiteSpace(cellStr))
                    {
                        sheetXml.Append("\"/>");
                    }
                    else
                    {
                        sheetXml.Append(CultureInfo.InvariantCulture, $"\" t=\"s\"><v>{sharedString.AddString(cellStr)}</v></c>");
                        if (!rowType.Equals(RowType.groupHeader))
                        {
                            int colWidth = (int)Math.Ceiling(cellStr.Length * (rowType.Equals(RowType.standard) ? 1 : 1.2));
                            colWidths[colIdx] = Math.Max(colWidths[colIdx], Math.Min(colWidth, maxColWidth));
                        }
                    }
                }
                sheetXml.Append("</row>");
            }

            /// <summary>
            /// Converts a column index to a Excel Column Ref.
            /// <example> For example <code>ColIdxToColRef(12)</code>returns "L" </example>
            /// <example> and <code>ColIdxToColRef(30)</code> returns "AD". </example>
            /// </summary>
            /// <param name="colIdx"> The column index to be converted, should use zero-based indexing </param>
            /// <returns> Excel Column referecnce in terms of base-26 alphabetic string </returns>
            private static string ColIdxToColRef(int colIdx)
            {
                string colRef = "";
                colIdx++;
                while (colIdx > 0)
                {
                    int rem = (colIdx - 1) % 26;
                    colRef = Convert.ToChar('A' + rem) + colRef;
                    colIdx = (colIdx - rem) / 26;
                }
                return colRef;
            }
        }


    }
}
