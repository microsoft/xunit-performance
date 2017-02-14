using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xunit.Performance.Api.Table
{
    internal sealed class DataTable
    {
        private ColumnNameCollection _ColumnNames;
        private List<Row> _Rows;

        public DataTable()
        {
            _ColumnNames = new ColumnNameCollection(this);
            _Rows = new List<Row>();
        }

        public ColumnNameCollection ColumnNames
        {
            get { return _ColumnNames; }
        }

        public ColumnName AddColumn(string name)
        {
            return _ColumnNames.Add(name);
        }

        public Row AppendRow()
        {
            Row row = new Row(this);
            _Rows.Add(row);

            return row;
        }

        public IEnumerable<Row> Rows
        {
            get { return _Rows; }
        }

        public void WriteToCSV(string fullFilePath, bool sort = true)
        {
            using (CSVFile outFile = new CSVFile(fullFilePath))
            {
                // Write the columns.
                string[] columnNames = _ColumnNames.Select(c => c.Name).ToArray();
                outFile.WriteLine(columnNames);

                var rows = (sort == true && _ColumnNames.Count() > 0) ?
                    Rows.OrderBy(columns => columns[_ColumnNames.First()]) : Rows;

                // Write out each row.
                foreach (var row in rows)
                {
                    string[] rowValues = new string[_ColumnNames.Names.Count];
                    for (int i = 0; i < _ColumnNames.Names.Count; i++)
                    {
                        rowValues[i] = row[_ColumnNames.Names.ElementAtOrDefault(i)];
                    }

                    outFile.WriteLine(rowValues);
                }
            }
        }

        public static DataTable ReadFromCSV(string fullFilePath)
        {
            DataTable table = new DataTable();

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
                    Row row = table.AppendRow();
                    for (columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                    {
                        ColumnName columnName = columns[columnIndex];
                        row[columnName] = reader.GetValue(rowIndex, columnName.Name);
                    }
                }
            }

            return table;
        }
    }
}
