using MarkdownLog;
using Microsoft.Xunit.Performance.Api.Table;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Xunit.Performance.Api
{
    // TODO: This could be implemented as an extension to the MarkdownLog.Table class
    internal static class MarkdownHelper
    {
        public static void Write(string mdFileName, MarkdownLog.Table mdTable)
        {
            using (var stream = new FileStream(mdFileName, FileMode.Create))
            {
                using (var sw = new StreamWriter(stream))
                {
                    sw.Write(ToTrimmedTable(mdTable));
                }
            }
        }

        public static string ToTrimmedTable(MarkdownLog.Table mdTable)
        {
            using (var sr = new StringReader(mdTable.ToMarkdown()))
            {
                string line;
                var sb = new StringBuilder();

                while ((line = sr.ReadLine()) != null)
                {
                    line = line.TrimStart(' ');
                    line = !line.StartsWith(":") ? $" {line}" : line;
                    sb.AppendLine(line);
                }

                return sb.ToString();
            }
        }

        public static MarkdownLog.Table GenerateMarkdownTable(DataTable dt)
        {
            var rows = dt.ColumnNames.Count() > 0 ?
                dt.Rows.OrderBy(columns => columns[dt.ColumnNames.First()]) :
                dt.Rows;

            var cellValueFunctions = new Func<Row, object>[] {
                (row) => {
                    return row[dt.ColumnNames[dt.ColumnNames.First().Name]];
                },
                (row) => {
                    return row[dt.ColumnNames[TableHeader.Metric]];
                },
                (row) => {
                    return row[dt.ColumnNames[TableHeader.Unit]];
                },
                (row) => {
                    return row[dt.ColumnNames[TableHeader.Iterations]];
                },
                (row) => {
                    return ConvertToDoubleFormattedString(row[dt.ColumnNames[TableHeader.Average]]);
                },
                (row) => {
                    return ConvertToDoubleFormattedString(row[dt.ColumnNames[TableHeader.StandardDeviation]]);
                },
                (row) => {
                    return ConvertToDoubleFormattedString(row[dt.ColumnNames[TableHeader.Minimum]]);
                },
                (row) => {
                    return ConvertToDoubleFormattedString(row[dt.ColumnNames[TableHeader.Maximum]]);
                },
            };

            var mdTable = rows.ToMarkdownTable(cellValueFunctions);
            mdTable.Columns = from column in dt.ColumnNames
                              select new TableColumn
                              {
                                  HeaderCell = new TableCell() { Text = column.Name }
                              };
            mdTable.Columns = new TableColumn[]
            {
                new TableColumn(){ HeaderCell = new TableCell() { Text = dt.ColumnNames.First().Name }, Alignment = TableColumnAlignment.Left },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Metric }, Alignment = TableColumnAlignment.Left },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Unit }, Alignment = TableColumnAlignment.Center },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Iterations }, Alignment = TableColumnAlignment.Center },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Average }, Alignment = TableColumnAlignment.Right },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.StandardDeviation }, Alignment = TableColumnAlignment.Right },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Minimum }, Alignment = TableColumnAlignment.Right },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Maximum }, Alignment = TableColumnAlignment.Right },
            };
            return mdTable;
        }

        private static string ConvertToDoubleFormattedString(string data)
        {
            const string fixedFotmat = "F3";
            const string scientificNotationFormat = "E3";
            var d = Convert.ToDouble(data);
            var format = (d != 0 && (d > 99999 || Math.Abs(d) < 0.001)) ? scientificNotationFormat : fixedFotmat;
            return d.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
