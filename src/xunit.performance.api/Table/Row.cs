using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api.Table
{
    sealed class Row
    {
        readonly Dictionary<ColumnName, string> _Cells = new Dictionary<ColumnName, string>();
        readonly DataTable _Table;

        public Row(DataTable table) => _Table = table;

        public string this[ColumnName columnName]
        {
            get
            {
                if (_Table != columnName.Table)
                {
                    throw new ArgumentException("The specified column is not part of the current table.", nameof(columnName));
                }

                _Cells.TryGetValue(columnName, out string value);
                return value;
            }
            set
            {
                if (_Table != columnName.Table)
                {
                    throw new ArgumentException("The specified column is not part of the current table.", nameof(columnName));
                }

                _Cells[columnName] = value;
            }
        }
    }
}