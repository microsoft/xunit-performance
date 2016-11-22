using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api.Table
{
    public sealed class Row
    {
        private DataTable _Table;
        private Dictionary<ColumnName, string> _Cells = new Dictionary<ColumnName, string>();

        public Row(DataTable table)
        {
            _Table = table;
        }

        public string this[ColumnName columnName]
        {
            get
            {
                if (_Table != columnName.Table)
                {
                    throw new ArgumentException("The specified column is not part of the current table.", "columnName");
                }

                string value;
                _Cells.TryGetValue(columnName, out value);
                return value;
            }
            set
            {
                if (_Table != columnName.Table)
                {
                    throw new ArgumentException("The specified column is not part of the current table.", "columnName");
                }

                _Cells[columnName] = value;
            }
        }
    }
}
