using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.Xunit.Performance.Api
{
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

    [Serializable]
    [XmlType("assembly")]
    public sealed class AssemblyModel
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("collection")]
        public List<TestModel> Collection { get; set; }
    }

    [Serializable]
    [XmlType("test")]
    public sealed class TestModel
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public string ClassName { get; set; }

        [XmlAttribute("method")]
        public string Method { get; set; }

        [XmlElement("performance")]
        public PerformanceModel Performance { get; set; }
    }

    public sealed class PerformanceModel : IXmlSerializable
    {
        public List<MetricModel> Metrics { get; set; }

        public List<IterationModel> IterationModels { get; set; }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

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
                    writer.WriteAttributeString(kvp.Key, kvp.Value.ToString());
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    public sealed class MetricModel
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Unit { get; set; }
    }

    public sealed class IterationModel
    {
        public Dictionary<string, double> Iteration { get; set; }
    }
}
