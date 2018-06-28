using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xunit.Performance.Api.Table
{
    sealed class ColumnNameCollection : IEnumerable<ColumnName>
    {
        readonly List<ColumnName> _ColumnNames;
        readonly DataTable _Table;

        public ColumnNameCollection(DataTable table)
        {
            _Table = table;
            _ColumnNames = new List<ColumnName>();
        }

        public IReadOnlyCollection<ColumnName> Names => _ColumnNames;

        public ColumnName this[string columnName] => _ColumnNames.SingleOrDefault(c => c.Name == columnName);

        public ColumnName Add(string name)
        {
            var column = new ColumnName(_Table, name);
            if (_ColumnNames.Contains(column))
            {
                throw new InvalidOperationException("Attempted to add a duplicate column.");
            }

            _ColumnNames.Add(column);

            return column;
        }

        IEnumerator<ColumnName> IEnumerable<ColumnName>.GetEnumerator() => _ColumnNames.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _ColumnNames.GetEnumerator();
    }
}