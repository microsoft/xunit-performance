using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xunit.Performance.Api.Table
{
    sealed class DataTable
    {
        readonly List<Row> _Rows;

        public DataTable()
        {
            ColumnNames = new ColumnNameCollection(this);
            _Rows = new List<Row>();
        }

        public ColumnNameCollection ColumnNames { get; private set; }

        public IEnumerable<Row> Rows => _Rows;

        public static DataTable ReadFromCSV(string fullFilePath)
        {
            var table = new DataTable();

            using (CSVReader reader = new CSVReader(fullFilePath))
            {
                // Create columns.
                ColumnName[] columns = new ColumnName[reader.TitlePositions.Count];
                int columnIndex = 0;

                foreach (KeyValuePair<string, int> titleInfo in reader.TitlePositions.OrderBy(p => p.Value))
                {
                    columns[columnIndex++] = table.AddColumn(titleInfo.Key);
                }

                // Iterate through each row in the CSV file and add the data to the table.
                for (int rowIndex = 0; rowIndex < reader.Length(); rowIndex++)
                {
                    // Create a new row.
                    var row = table.AppendRow();
                    for (columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                    {
                        ColumnName columnName = columns[columnIndex];
                        row[columnName] = reader.GetValue(rowIndex, columnName.Name);
                    }
                }
            }

            return table;
        }

        public ColumnName AddColumn(string name) => ColumnNames.Add(name);

        public Row AppendRow()
        {
            var row = new Row(this);
            _Rows.Add(row);

            return row;
        }

        public void WriteToCSV(string fullFilePath, bool sort = true)
        {
            using (CSVFile outFile = new CSVFile(fullFilePath))
            {
                // Write the columns.
                var columnNames = ColumnNames.Select(c => c.Name).ToArray();
                outFile.WriteLine(columnNames);

                var rows = (sort && ColumnNames.Any()) ?
                    Rows.OrderBy(columns => columns[ColumnNames.First()]) : Rows;

                // Write out each row.
                foreach (var row in rows)
                {
                    string[] rowValues = new string[ColumnNames.Names.Count];
                    for (int i = 0; i < ColumnNames.Names.Count; i++)
                    {
                        rowValues[i] = row[ColumnNames.Names.ElementAtOrDefault(i)];
                    }

                    outFile.WriteLine(rowValues);
                }
            }
        }
    }
}