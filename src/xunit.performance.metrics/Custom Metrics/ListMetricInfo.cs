using System;
using System.Collections.Generic;

namespace Microsoft.Xunit.Performance
{
    public class ListMetricInfo
    {
        Metrics Name = new Metrics();
        Metrics Size = new Metrics();
        Metrics Count = new Metrics();

        Dictionary<string, SizeCount> _Items = new Dictionary<string, SizeCount>();

        public Dictionary<string, SizeCount> Items { get { return _Items; } }

        public IEnumerable<Metrics> MetricList { get { return new Metrics[] { Name, Size, Count }; } }

        public ListMetricInfo()
        {
            initializeMetrics();
        }

        public void addItem(string itemName, long size)
        {
            SizeCount item;
            if (!Items.TryGetValue(itemName, out item))
            {
                item = new SizeCount();
                Items[itemName] = item;
            }

            item.Size += size;
            item.Count++;
        }

        void initializeMetrics()
        {
            Name.Name = "Name";
            Name.Unit = "FileName";
            Name.Type = typeof(string);
            Size.Name = "Size";
            Size.Unit = "Bytes";
            Size.Type = typeof(Int32);
            Count.Name = "Count";
            Count.Unit = "Count";
            Count.Type = typeof(Int32);
        }

        public class Metrics
        {
            public string Name { get; set; }
            public string Unit { get; set; }
            public Type Type { get; set; }
        }

        public class SizeCount
        {
            public long Size { get; set; }
            public long Count { get; set; }

            public SizeCount()
            {
                Size = 0;
                Count = 0;
            }
        }
    }
}
