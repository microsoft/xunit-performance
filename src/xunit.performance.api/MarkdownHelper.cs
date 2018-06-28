using MarkdownLog;
using Microsoft.Xunit.Performance.Api.Table;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

#if MarkdownLog
// Package: Microsoft.3rdpartytools.MarkdownLog
// Install-Package Microsoft.3rdpartytools.MarkdownLog -Version 0.10.0-alpha-experimental -Source https://dotnet.myget.org/F/dotnet-core/api/v3/index.json
#else

namespace MarkdownLog
{
    public static class StringExtensions
    {
        public static string Indent(this string text, int indentSize) => text.PrependAllLines(new string(' ', indentSize));

        public static string IndentAllExceptFirst(this string text, int indentSize) => text.PrependLines(new string(' ', indentSize), 1);

        public static string PrependAllLines(this string text, string prefix) => text.PrependLines(prefix, 0);

        static string PrependLines(this string text, string prefix, int numberToSkip = 0)
        {
            var source = text.SplitByLine();
            var second = source.Skip<string>(numberToSkip).Select<string, string>(i => prefix + i);
            return string.Join(Environment.NewLine, source.Take<string>(numberToSkip).Concat<string>(second));
        }

        public static IList<string> SplitByLine(this string text) => text.Split(new string[]
            {
        "\r\n",
        "\n\r",
        "\n",
        "\r"
            }, StringSplitOptions.None);

        public static string WrapAt(this string text, int maxCharsPerLine)
        {
            var strArray = text.Split(' ');
            var stringBuilder = new StringBuilder();
            int num = 0;
            foreach (string str in strArray)
            {
                if (num + str.Length < maxCharsPerLine)
                {
                    stringBuilder.Append(str);
                    if (str != ((IEnumerable<string>)strArray).Last<string>())
                        stringBuilder.Append(" ");
                    num += str.Length + 1;
                }
                else
                {
                    stringBuilder.Append(Environment.NewLine + str + " ");
                    num = str.Length + 1;
                }
            }
            return stringBuilder.ToString();
        }

        public static string EscapeMarkdownCharacters(this string text)
        {
            var input = text.Replace("\\", "\\\\").Replace("`", "\\`").Replace("*", "\\*").Replace("_", "\\_");
            return !input.StartsWith("#", StringComparison.Ordinal) ? Regex.Replace(input, "(?<Number>[0-9]+)\\. ", "${Number}\\. ") : "\\" + input;
        }

        public static string Align(this string text, TableColumnAlignment alignment, int width)
        {
            switch (alignment)
            {
                case TableColumnAlignment.Center:
                    var count1 = Math.Max(0, (width - text.Length) / 2);
                    var count2 = Math.Max(0, width - text.Length - count1);
                    return string.Format("{0}{1}{2}", new string(' ', count1), text, new string(' ', count2));

                case TableColumnAlignment.Right:
                    return text.PadLeft(width);

                default:
                    return text.PadRight(width);
            }
        }

        public static string EscapeCSharpString(this string text) => text.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t").Replace("\f", "\\f").Replace("\0", "\\0").Replace("\a", "\\a").Replace("\b", "\\b");
    }

    public class TableColumn
    {
        ITableCell _headerCell;

        public TableColumn()
        {
            _headerCell = new EmptyTableCell();
            Alignment = TableColumnAlignment.Unspecified;
        }

        public ITableCell HeaderCell
        {
            get => _headerCell;
            set => _headerCell = value ?? new EmptyTableCell();
        }

        public TableColumnAlignment Alignment { get; set; }
    }

    public class TableCell : ITableCell
    {
        string _text;

        public string Text
        {
            get => _text;
            set => _text = value ?? "";
        }

        public int RequiredWidth => GetEncodedText().Length;

        public string BuildCodeFormattedString(TableCellRenderSpecification spec) => GetEncodedText().Align(spec.Alignment, spec.MaximumWidth);

        string GetEncodedText() => _text.Trim().EscapeCSharpString();
    }

    public enum TableColumnAlignment
    {
        Unspecified,
        Left,
        Center,
        Right,
    }

    public interface ITableCell
    {
        int RequiredWidth { get; }

        string BuildCodeFormattedString(TableCellRenderSpecification spec);
    }

    public class TableCellRenderSpecification
    {
        internal TableCellRenderSpecification(TableColumnAlignment alignment, int maximumWidth)
        {
            Alignment = alignment;
            MaximumWidth = maximumWidth;
        }

        public TableColumnAlignment Alignment { get; private set; }

        public int MaximumWidth { get; private set; }
    }

    class EmptyTableCell : ITableCell
    {
        public int RequiredWidth => 0;

        public string BuildCodeFormattedString(TableCellRenderSpecification spec) => "";
    }

    public class TableRow
    {
        IEnumerable<ITableCell> _cells = Enumerable.Empty<ITableCell>();

        public IEnumerable<ITableCell> Cells
        {
            get => _cells;
            set => _cells = value ?? Enumerable.Empty<ITableCell>();
        }
    }

    public class Table
    {
        readonly IEnumerable<Row> tableRows;
        readonly IEnumerable<Func<Row, object>> tableColumnFuncs;

        internal Table(IEnumerable<Row> tableRows, IEnumerable<Func<Row, object>> tableColumnFuncs)
        {
            this.tableRows = tableRows;
            this.tableColumnFuncs = tableColumnFuncs;
            var rows = new List<TableRow>();
            foreach (var row in tableRows)
            {
                var cells = new List<TableCell>();
                foreach (var func in tableColumnFuncs)
                {
                    var obj = func(row);
                    var cell = new TableCell() { Text = (obj is ColumnName name) ? name.Name : obj.ToString() };
                    cells.Add(cell);
                }
                rows.Add(new TableRow() { Cells = cells });
            }
            Rows = rows;
        }

        static readonly EmptyTableCell EmptyCell = new EmptyTableCell();

        IEnumerable<TableRow> _rows = new List<TableRow>();
        IEnumerable<TableColumn> _columns = new List<TableColumn>();

        IEnumerable<TableRow> Rows
        {
            get => _rows;
            set => _rows = value ?? Enumerable.Empty<TableRow>();
        }

        public IEnumerable<TableColumn> Columns
        {
            get => _columns;
            set => _columns = value ?? new List<TableColumn>();
        }

        public string ToMarkdown()
        {
            var markdownBuilder = new MarkdownBuilder(this);
            return markdownBuilder.Build();
        }

        class MarkdownBuilder
        {
            class Row
            {
                public IList<ITableCell> Cells { get; set; }
            }

            readonly List<Row> _rows;
            readonly List<TableColumn> _columns;
            readonly StringBuilder _builder = new StringBuilder();
            readonly IList<TableCellRenderSpecification> _columnRenderSpecs;

            internal MarkdownBuilder(Table table)
            {
                _columns = table.Columns.ToList();
                _rows = table.Rows.Select(row => new Row { Cells = row.Cells.ToList() }).ToList();

                var columnCount = Math.Max(_columns.Count, _rows.Any() ? _rows.Max(r => r.Cells.Count) : 0);
                _columnRenderSpecs = Enumerable.Range(0, columnCount).Select(BuildColumnSpecification).ToList();
            }

            TableCellRenderSpecification BuildColumnSpecification(int column) => new TableCellRenderSpecification(GetColumnAt(column).Alignment, GetMaximumCellWidth(column));

            internal string Build()
            {
                BuildHeaderRow();
                BuildDividerRow();

                foreach (var row in _rows)
                {
                    BuildBodyRow(row);
                }

                return _builder.ToString();
            }

            void BuildHeaderRow()
            {
                var headerCells = (from column in Enumerable.Range(0, _columnRenderSpecs.Count)
                                   let cell = GetColumnAt(column).HeaderCell
                                   let text = BuildCellMarkdownCode(column, cell)
                                   select text).ToList();

                _builder.Append("    ");
                _builder.AppendLine(" " + string.Join(" | ", headerCells));
            }

            void BuildDividerRow()
            {
                _builder.Append("    ");
                _builder.AppendLine(string.Join("|", _columnRenderSpecs.Select(BuildDividerCell)));
            }

            static string BuildDividerCell(TableCellRenderSpecification spec)
            {
                var dashes = new string('-', spec.MaximumWidth);

                switch (spec.Alignment)
                {
                    case TableColumnAlignment.Left:
                        return ":" + dashes + " ";

                    case TableColumnAlignment.Center:
                        return ":" + dashes + ":";

                    case TableColumnAlignment.Right:
                        return " " + dashes + ":";

                    default:
                        return " " + dashes + " ";
                }
            }

            void BuildBodyRow(Row row)
            {
                var rowCells = (from column in Enumerable.Range(0, _columnRenderSpecs.Count)
                                let cell = GetCellAt(row.Cells, column)
                                select BuildCellMarkdownCode(column, cell)).ToList();

                _builder.Append("    ");
                _builder.AppendLine(" " + string.Join(" | ", rowCells));
            }

            string BuildCellMarkdownCode(int column, ITableCell cell)
            {
                var columnSpec = _columnRenderSpecs[column];
                var maximumWidth = columnSpec.MaximumWidth;
                var cellText = cell.BuildCodeFormattedString(new TableCellRenderSpecification(columnSpec.Alignment, maximumWidth));
                var truncatedCellText = cellText.Length > maximumWidth ? cellText.Substring(0, maximumWidth) : cellText.PadRight(maximumWidth);

                return truncatedCellText;
            }

            int GetMaximumCellWidth(int column)
            {
                var headerCells = new[] { GetColumnAt(column).HeaderCell };
                var bodyCells = _rows.Select(row => GetCellAt(row.Cells, column));
                var columnCells = headerCells.Concat(bodyCells);
                return columnCells.Max(i => i.RequiredWidth);
            }

            TableColumn GetColumnAt(int index) => index < _columns.Count
                    ? _columns[index]
                    : CreateDefaultHeaderCell(index);

            static TableColumn CreateDefaultHeaderCell(int columnIndex) =>
                // GitHub Flavoured Markdown requires a header cell. If header text isn't provided
                // use an Excel-like naming scheme (e.g. A, B, C, .., AA, AB, etc)

                new TableColumn { HeaderCell = new TableCell { Text = columnIndex.ToColumnTitle() } };

            static ITableCell GetCellAt(IList<ITableCell> cells, int index) => index < cells.Count ? cells[index] : EmptyCell;
        }
    }

    public static class NumberExtensions
    {
        public static string ToColumnTitle(this int columnIndex)
        {
            int dividend = columnIndex + 1;
            string columnName = string.Empty;

            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }
    }
}

#endif

namespace Microsoft.Xunit.Performance.Api
{
    // TODO: This could be implemented as an extension to the MarkdownLog.Table class
    static class MarkdownHelper
    {
        public static MarkdownLog.Table ToMarkdownTable(this IEnumerable<Table.Row> rows, IEnumerable<Func<Table.Row, object>> funcs) => new MarkdownLog.Table(rows, funcs);

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
                    line = !line.StartsWith(":", StringComparison.Ordinal) ? $" {line}" : line;
                    sb.AppendLine(line);
                }

                return sb.ToString();
            }
        }

        public static MarkdownLog.Table GenerateMarkdownTable(DataTable dt)
        {
            bool includesScenarioNameColumn = (dt.ColumnNames.Any() && dt.ColumnNames.First().Name == TableHeader.ScenarioName);
            IEnumerable<Row> rows;
            if (includesScenarioNameColumn)
            {
                rows = dt.Rows.OrderBy(columns => columns[dt.ColumnNames[TableHeader.ScenarioName]])
                              .ThenBy(columns => columns[dt.ColumnNames[TableHeader.TestName]]);
            }
            else if (dt.ColumnNames.Any())
            {
                rows = dt.Rows.OrderBy(columns => columns[dt.ColumnNames.First()]);
            }
            else
            {
                rows = dt.Rows;
            }

            var cellValueFunctions = new List<Func<Row, object>>();

            if (includesScenarioNameColumn)
            {
                cellValueFunctions.Add((row) =>
                {
                    return row[dt.ColumnNames[TableHeader.ScenarioName]];
                });
                cellValueFunctions.Add((row) =>
                {
                    return row[dt.ColumnNames[TableHeader.TestName]];
                });
            }
            else
            {
                cellValueFunctions.Add((row) =>
                {
                    return row[dt.ColumnNames[dt.ColumnNames.First().Name]];
                });
            }

            cellValueFunctions.AddRange(new Func<Row, object>[]
            {
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
            });

            var mdTable = rows.ToMarkdownTable(cellValueFunctions.ToArray());

            var tableColumns = new List<TableColumn>();
            if (includesScenarioNameColumn)
            {
                tableColumns.Add(new TableColumn() { HeaderCell = new TableCell() { Text = TableHeader.ScenarioName }, Alignment = TableColumnAlignment.Left });
                tableColumns.Add(new TableColumn() { HeaderCell = new TableCell() { Text = TableHeader.TestName }, Alignment = TableColumnAlignment.Left });
            }
            else
            {
                tableColumns.Add(new TableColumn() { HeaderCell = new TableCell() { Text = dt.ColumnNames.First().Name }, Alignment = TableColumnAlignment.Left });
            }

            tableColumns.AddRange(new TableColumn[]
            {
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Metric }, Alignment = TableColumnAlignment.Left },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Unit }, Alignment = TableColumnAlignment.Center },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Iterations }, Alignment = TableColumnAlignment.Center },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Average }, Alignment = TableColumnAlignment.Right },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.StandardDeviation }, Alignment = TableColumnAlignment.Right },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Minimum }, Alignment = TableColumnAlignment.Right },
                new TableColumn(){ HeaderCell = new TableCell() { Text = TableHeader.Maximum }, Alignment = TableColumnAlignment.Right },
            });

            mdTable.Columns = tableColumns;
            return mdTable;
        }

        static string ConvertToDoubleFormattedString(string data)
        {
            const string fixedFormat = "F3";
            const string scientificNotationFormat = "E3";
            var d = Convert.ToDouble(data);
            var format = (d != 0 && (d > 999999 || Math.Abs(d) < 0.001)) ? scientificNotationFormat : fixedFormat;
            return d.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}