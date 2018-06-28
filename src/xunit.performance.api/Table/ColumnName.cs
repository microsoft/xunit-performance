namespace Microsoft.Xunit.Performance.Api.Table
{
    sealed class ColumnName
    {
        public ColumnName(DataTable table, string name)
        {
            Table = table;
            Name = name;
        }

        public string Name { get; private set; }

        internal DataTable Table { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is ColumnName name)
            {
                return ((Table == name.Table) &&
                        (Name == name.Name));
            }

            return false;
        }

        public override int GetHashCode() => Table.GetHashCode() ^ Name.GetHashCode();
    }
}