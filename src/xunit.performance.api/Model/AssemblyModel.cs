using Microsoft.Xunit.Performance.Api.Table;
using Microsoft.Xunit.Performance.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.Xunit.Performance.Api
{
    [Serializable]
    [XmlType("assembly")]
    public sealed class AssemblyModel
    {
        [XmlArray("collection")]
        public List<TestModel> Collection { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        internal static AssemblyModel Create(
            string assemblyFileName,
            CSVMetricReader reader,
            XUnitPerformanceMetricData xUnitPerformanceMetricData)
        {
            var assemblyModel = new AssemblyModel
            {
                Name = Path.GetFileName(assemblyFileName),
                Collection = new List<TestModel>()
            };

            foreach (var (perfTestMsg, metric, values) in GetCollectedData(assemblyFileName, reader, xUnitPerformanceMetricData))
            {
                var testModel = assemblyModel.Collection.FirstOrDefault(test => test.Name == perfTestMsg.TestCase.DisplayName);
                if (testModel == null)
                {
                    testModel = new TestModel
                    {
                        Name = perfTestMsg.TestCase.DisplayName,
                        Method = perfTestMsg.TestCase.TestMethod.Method.Name,
                        ClassName = perfTestMsg.TestCase.TestMethod.TestClass.Class.Name,
                        Performance = new PerformanceModel
                        {
                            Metrics = new List<MetricModel>(),
                            IterationModels = new List<IterationModel>()
                        },
                    };
                }

                var testMetric = testModel.Performance.Metrics.FirstOrDefault(m => m.DisplayName == metric);
                if (testMetric == null)
                {
                    testModel.Performance.Metrics.Add(new MetricModel
                    {
                        DisplayName = metric,
                        Name = metric,
                        Unit = metric == "Duration" ? PerformanceMetricUnits.Milliseconds : "unknown", // We are guessing here.
                    });
                }

                foreach (var value in values)
                {
                    var iterationModel = new IterationModel { Iteration = new Dictionary<string, double>() };
                    iterationModel.Iteration.Add(metric, value);
                    if (iterationModel.Iteration.Count > 0)
                        testModel.Performance.IterationModels.Add(iterationModel);
                }

                assemblyModel.Collection.Add(testModel);
            }

            return assemblyModel;
        }

        internal DataTable GetStatistics()
        {
            var dt = new DataTable();
            var col0_testName = dt.AddColumn(Name);
            var col1_metric = dt.AddColumn(TableHeader.Metric);
            var col2_unit = dt.AddColumn(TableHeader.Unit);
            var col3_iterations = dt.AddColumn(TableHeader.Iterations);
            var col4_average = dt.AddColumn(TableHeader.Average);
            var col5_stdevs = dt.AddColumn(TableHeader.StandardDeviation);
            var col6_min = dt.AddColumn(TableHeader.Minimum);
            var col7_max = dt.AddColumn(TableHeader.Maximum);

            foreach (var testModel in Collection)
            {
                foreach (var metric in testModel.Performance.Metrics)
                {
                    var values = testModel.Performance.IterationModels
                        .Where(iter => iter.Iteration.ContainsKey(metric.Name))
                        .Select(iter => iter.Iteration[metric.Name]);

                    if (!values.Any()) // Cannot compute statistics when there are not results (e.g. user only ran a subset of all tests).
                        continue;

                    // Skip the warmup run.
                    if (values.Count() > 1)
                        values = values.Skip(1);

                    var avg = values.Average();
                    var stdev_s = Math.Sqrt(values.Sum(x => Math.Pow(x - avg, 2)) / (values.Count() - 1));
                    var max = values.Max();
                    var min = values.Min();

                    var newRow = dt.AppendRow();
                    newRow[col0_testName] = testModel.Name;
                    newRow[col1_metric] = metric.DisplayName;
                    newRow[col2_unit] = metric.Unit;

                    newRow[col3_iterations] = values.Count().ToString();
                    newRow[col4_average] = avg.ToString();
                    newRow[col5_stdevs] = stdev_s.ToString();
                    newRow[col6_min] = min.ToString();
                    newRow[col7_max] = max.ToString();
                }
            }

            return dt;
        }

        static IEnumerable<(PerformanceTestMessage performanceTestMessage, string metric, IEnumerable<double> values)> GetCollectedData(
            string assemblyFileName,
            CSVMetricReader reader,
            XUnitPerformanceMetricData xUnitPerformanceMetricData)
        {
            assemblyFileName = assemblyFileName ?? "";
            var testsFoundInAssembly = xUnitPerformanceMetricData.PerformanceTestMessages;
            foreach (var testFoundInAssembly in testsFoundInAssembly)
            {
                foreach (var (testCaseName, metric, values) in GetMeasurements(reader))
                {
                    if (values == null)
                        continue;

                    if (testCaseName == testFoundInAssembly.TestCase.DisplayName)
                    {
                        yield return (testFoundInAssembly, metric, values);
                    }
                }
            }
        }

        static IEnumerable<(string testCaseName, string metric, IEnumerable<double> values)> GetMeasurements(CSVMetricReader reader)
        {
            foreach (var testCaseName in reader.TestCases)
            {
                var iterations = reader.GetValues(testCaseName);
                var measurements = new Dictionary<string, List<double>>();

                foreach (var dict in iterations)
                {
                    foreach (var pair in dict)
                    {
                        if (!measurements.ContainsKey(pair.Key))
                            measurements[pair.Key] = new List<double>();
                        measurements[pair.Key].Add(pair.Value);
                    }
                }

                foreach (var measurement in measurements)
                    yield return (testCaseName, measurement.Key, measurement.Value);
            }
        }
    }

    [Serializable]
    [XmlRoot("assemblies")]
    public sealed class AssemblyModelCollection : List<AssemblyModel>
    {
        public void Serialize(string xmlFileName)
        {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");
            using (var stream = File.Create(xmlFileName))
            {
                using (var sw = new StreamWriter(stream))
                {
                    new XmlSerializer(typeof(AssemblyModelCollection))
                        .Serialize(sw, this, namespaces);
                }
            }
        }
    }

    public sealed class IterationModel
    {
        public Dictionary<string, double> Iteration { get; set; }
    }

    public sealed class MetricModel
    {
        string _name;

        public string DisplayName { get; set; }

        // TODO: This should be internal 'MetricModel.Name' is only used to generate the 'XmlElement.Name' & 'XmlAttribute.Name'.
        public string Name
        {
            get => _name;
            set => _name = XmlConvert.EncodeName(value);
        }

        public string Unit { get; set; }
    }

    public sealed class PerformanceModel : IXmlSerializable
    {
        public List<IterationModel> IterationModels { get; set; }
        public List<MetricModel> Metrics { get; set; }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader) => throw new NotImplementedException();

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("metrics");
            foreach (var metric in Metrics)
            {
                writer.WriteStartElement(metric.Name);
                writer.WriteAttributeString("displayName", metric.DisplayName);
                writer.WriteAttributeString("unit", metric.Unit);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("iterations");
            var index = 0;
            foreach (var iterationModel in IterationModels)
            {
                writer.WriteStartElement("iteration");
                writer.WriteAttributeString("index", index.ToString());
                ++index;
                foreach (var kvp in iterationModel.Iteration)
                {
                    writer.WriteAttributeString(kvp.Key, kvp.Value.ToString(CultureInfo.InvariantCulture));
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    [Serializable]
    [XmlRoot("ScenarioBenchmark")]
    public sealed class ScenarioBenchmark
    {
        public ScenarioBenchmark(string name) : this()
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"{nameof(name)} cannot be null, empty or white space.");
            Name = name;
        }

        ScenarioBenchmark()
        {
            Namespace = "";
            Tests = new List<ScenarioTestModel>();
        }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Namespace")]
        public string Namespace { get; set; }

        [XmlArray("Tests")]
        public List<ScenarioTestModel> Tests { get; set; }

        public void Serialize(string xmlFileName)
        {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");
            using (var stream = File.Create(xmlFileName))
            {
                using (var sw = new StreamWriter(stream))
                {
                    new XmlSerializer(typeof(ScenarioBenchmark))
                        .Serialize(sw, this, namespaces);
                }
            }
        }

        internal static DataTable GetEmptyTable(string scenarioName = null)
        {
            var dt = new DataTable();
            if (scenarioName == null)
            {
                dt.AddColumn(TableHeader.ScenarioName);
                dt.AddColumn(TableHeader.TestName);
            }
            else
            {
                dt.AddColumn(scenarioName);
            }
            dt.AddColumn(TableHeader.Metric);
            dt.AddColumn(TableHeader.Unit);
            dt.AddColumn(TableHeader.Iterations);
            dt.AddColumn(TableHeader.Average);
            dt.AddColumn(TableHeader.StandardDeviation);
            dt.AddColumn(TableHeader.Minimum);
            dt.AddColumn(TableHeader.Maximum);

            return dt;
        }

        internal void AddRowsToTable(DataTable dt, IEnumerable<ScenarioTestResultRow> rows, bool includeScenarioNameColumn = false)
        {
            foreach (var row in rows)
            {
                var newRow = dt.AppendRow();
                if (includeScenarioNameColumn)
                {
                    newRow[dt.ColumnNames[TableHeader.ScenarioName]] = row.ScenarioName;
                    newRow[dt.ColumnNames[TableHeader.TestName]] = row.TestName;
                }
                else
                {
                    newRow[dt.ColumnNames[row.ScenarioName]] = row.TestName;
                }
                newRow[dt.ColumnNames[TableHeader.Metric]] = row.MetricName;
                newRow[dt.ColumnNames[TableHeader.Unit]] = row.MetricUnit;

                newRow[dt.ColumnNames[TableHeader.Iterations]] = row.Iterations.ToString();
                newRow[dt.ColumnNames[TableHeader.Average]] = row.Average.ToString();
                newRow[dt.ColumnNames[TableHeader.StandardDeviation]] = row.StandardDeviation.ToString();
                newRow[dt.ColumnNames[TableHeader.Minimum]] = row.Minimum.ToString();
                newRow[dt.ColumnNames[TableHeader.Maximum]] = row.Maximum.ToString();
            }
        }

        internal List<ScenarioTestResultRow> GetStatistics()
        {
            var ret = new List<ScenarioTestResultRow>();

            foreach (var test in Tests)
            {
                foreach (var metric in test.Performance.Metrics)
                {
                    var values = test.Performance.IterationModels
                        .Where(iter => iter.Iteration.ContainsKey(metric.Name))
                        .Select(iter => iter.Iteration[metric.Name]);

                    if (!values.Any()) // Cannot compute statistics when there are not results (e.g. user only ran a subset of all tests).
                        continue;

                    // Skip the warmup run.
                    if (values.Count() > 1)
                        values = values.Skip(1);

                    var avg = values.Average();
                    var stdev_s = Math.Sqrt(values.Sum(x => Math.Pow(x - avg, 2)) / (values.Count() - 1));
                    var max = values.Max();
                    var min = values.Min();

                    var row = new ScenarioTestResultRow
                    {
                        ScenarioName = Name,
                        TestName = string.IsNullOrEmpty(test.Namespace) ?
                        $"{test.Name}" : $"{test.Namespace}{test.Separator}{test.Name}",
                        MetricName = metric.DisplayName,
                        MetricUnit = metric.Unit,

                        Iterations = values.Count(),
                        Average = avg,
                        StandardDeviation = stdev_s,
                        Minimum = min,
                        Maximum = max
                    };

                    ret.Add(row);
                }
            }

            return ret;
        }

        internal DataTable GetStatisticsTable(bool includeScenarioNameColumn = false)
        {
            var dt = GetEmptyTable(includeScenarioNameColumn ? null : Name);

            AddRowsToTable(dt, GetStatistics(), includeScenarioNameColumn);

            return dt;
        }
    }

    [Serializable]
    [XmlType("Test")]
    public sealed class ScenarioTestModel
    {
        string _namespace;

        string _separator;

        public ScenarioTestModel(string name) : this() => Name = name;

        ScenarioTestModel()
        {
            _namespace = "";
            _separator = "/";
            Performance = new PerformanceModel
            {
                Metrics = new List<MetricModel>(),
                IterationModels = new List<IterationModel>()
            };
        }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Namespace")]
        public string Namespace { get => _namespace; set => _namespace = value ?? ""; }

        [XmlElement("Performance")]
        public PerformanceModel Performance { get; set; }

        public string Separator { get => _separator; set => _separator = value ?? "/"; }
    }

    [Serializable]
    [XmlType("test")]
    public sealed class TestModel
    {
        [XmlAttribute("type")]
        public string ClassName { get; set; }

        [XmlAttribute("method")]
        public string Method { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("performance")]
        public PerformanceModel Performance { get; set; }
    }

    sealed class ScenarioTestResultRow
    {
        public double Average { get; set; }
        public int Iterations { get; set; }
        public double Maximum { get; set; }
        public string MetricName { get; set; }
        public string MetricUnit { get; set; }
        public double Minimum { get; set; }
        public string ScenarioName { get; set; }
        public double StandardDeviation { get; set; }
        public string TestName { get; set; }
    }
}