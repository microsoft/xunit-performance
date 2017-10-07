using System;

namespace Microsoft.Xunit.Performance.Api
{
    /// <summary>
    /// Provides a mechanism for listening events.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value of the method that the runner delegate encapsulates.</typeparam>
    internal interface ICanListenEvents<TResult>
    {
        /// <summary>
        /// Performs application-defined tasks associated with profiling or tracing.
        /// </summary>
        /// <param name="runner">Operation to be profiled and/or traced</param>
        /// <returns>The return value of the method that the runner delegate encapsulates.</returns>
        TResult Record(Func<TResult> runner);
    }
}
