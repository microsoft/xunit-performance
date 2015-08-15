using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Xunit.Performance
{
    internal class BenchmarkTraitDiscoverer : ITraitDiscoverer
    {
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            return new[] { new KeyValuePair<string, string>("Benchmark", "true") };
        } 
    }
}
