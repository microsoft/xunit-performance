using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xunit.Performance.Api.Table
{
    internal sealed class ColumnName
    {
        private DataTable _Table;
        private string _Name;

        public ColumnName(DataTable table, string name)
        {
            _Table = table;
            _Name = name;
        }

        public string Name
        {
            get { return _Name; }
        }

        internal DataTable Table
        {
            get { return _Table; }
        }

        public override bool Equals(object obj)
        {
            ColumnName name = obj as ColumnName;
            if (null != name)
            {
                return ((_Table == name._Table) &&
                        (_Name == name._Name));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _Table.GetHashCode() ^ _Name.GetHashCode();
        }
    }
}
