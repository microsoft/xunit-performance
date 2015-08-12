using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Xunit.Performance
{
    class XunitPerformanceProject : XunitProject
    {
        private List<XunitProjectAssembly> _baselineAssemblies = new List<XunitProjectAssembly>();
        private string _baselineRunnerCommand;

        public IEnumerable<XunitProjectAssembly> BaselineAssemblies { get { return _baselineAssemblies; } }

        public void AddBaseline(XunitProjectAssembly assembly) { _baselineAssemblies.Add(assembly); }

        public string RunnerCommand { get; set; } = "xunit.console.exe";

        public string BaselineRunnerCommand
        {
            get { return _baselineRunnerCommand ?? RunnerCommand; }
            set { _baselineRunnerCommand = value; }
        }

        public string RunName { get; set; } = Environment.MachineName + "-" + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");

        public string EtlDirectory { get; set; } = ".";
    }
}
