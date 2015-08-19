namespace Microsoft.Xunit.Performance.Sdk
{
    public class PerformanceMetricValue
    {
        public string Name { get; private set; }
        public string Unit { get; private set; }
        public double Value { get; private set; }

        public PerformanceMetricValue(string name, string units, double value)
        {
            Name = name;
            Unit = units;
            Value = value;
        }
    }
}
