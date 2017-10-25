using System;
using System.Diagnostics;

namespace Microsoft.Xunit.Performance.Api
{
    public sealed class ScenarioTest : IDisposable
    {
        public ScenarioTest(ScenarioTestConfiguration configuration)
        {
            _disposedValue = false;
            Process = new Process {
                StartInfo = configuration.StartInfo,
            };
        }

        public Process Process { get; }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                    Process.Dispose();
                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
