using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance.Api.Table
{
    internal sealed class ColumnNameCollection : IEnumerable<ColumnName>
    {
        private DataTable _Table;
        private List<ColumnName> _ColumnNames;

        public ColumnNameCollection(DataTable table)
        {
            _Table = table;
            _ColumnNames = new List<ColumnName>();
        }

        public ColumnName Add(string name)
        {
            ColumnName column = new ColumnName(_Table, name);
            if (_ColumnNames.Contains(column))
            {
                throw new InvalidOperationException("Attempted to add a duplicate column.");
            }

            _ColumnNames.Add(column);

            return column;
        }

        public IReadOnlyCollection<ColumnName> Names
        {
            get { return _ColumnNames; }
        }

        public ColumnName this[string columnName]
        {
            get { return _ColumnNames.SingleOrDefault(c => c.Name == columnName); }
        }

        IEnumerator<ColumnName> IEnumerable<ColumnName>.GetEnumerator()
        {
            return _ColumnNames.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _ColumnNames.GetEnumerator();
        }
    }
}
