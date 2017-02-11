using System.Collections.Generic;

namespace Microsoft.Xunit.Performance.Api
{
    internal sealed class AssemblyModel
    {
        public string Name { get; set; }

        public List<TestModel> Collection { get; set; }
    }

    internal sealed class TestModel
    {
        public string Name { get; set; }

        public string ClassName { get; set; }

        public string Method { get; set; }

        public PerformanceModel Performance { get; set; }
    }

    internal sealed class PerformanceModel
    {
        public List<MetricModel> Metrics { get; set; }

        public List<IterationModel> IterationModels { get; set; }
    }

    internal sealed class MetricModel
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Unit { get; set; }
    }

    internal sealed class IterationModel
    {
        public Dictionary<string, double> Iteration { get; set; }
    }
}
