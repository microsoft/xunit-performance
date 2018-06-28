using System;
using System.Diagnostics;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Represents a scenario test that is about to be run
    /// </summary>
    public sealed class ScenarioTest : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="ScenarioTest"/> object
        /// </summary>
        /// <param name="configuration">The scenario configuration</param>
        public ScenarioTest(ScenarioTestConfiguration configuration)
        {
            _disposedValue = false;
            Process = new Process
            {
                StartInfo = configuration.StartInfo,
            };
        }

        /// <summary>
        /// The process that will be run for the test
        /// </summary>
        public Process Process { get; }

        #region IDisposable Support

        bool _disposedValue; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose() => Dispose(true);

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                    Process.Dispose();
                _disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}