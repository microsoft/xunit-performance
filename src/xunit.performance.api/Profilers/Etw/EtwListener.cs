// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Microsoft.Xunit.Performance.Api.PerformanceLogger;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Implements a mechanism for listening ETW events.
    /// </summary>
    /// <typeparam name="TResult">The type of the return value of the method that the runner delegate encapsulates.</typeparam>
    internal sealed class EtwListener<TResult> : ICanListenEvents<TResult>
    {
        /// <summary>
        /// Initializes a new instance of the EtwListener class.
        /// </summary>
        /// <param name="userSessionData">ETW session data.</param>
        /// <param name="userProviders">A collection of user providers to be enabled.</param>
        /// <param name="kernelProviders">A collection of kernel providers to be enabled.</param>
        public EtwListener(
            EtwSessionData userSessionData,
            IReadOnlyCollection<EtwUserProvider> userProviders,
            IReadOnlyCollection<EtwKernelProvider> kernelProviders)
        {
            UserSessionData = userSessionData ?? throw new ArgumentNullException(nameof(userSessionData));
            UserProviders = userProviders ?? throw new ArgumentNullException(nameof(userProviders));
            KernelProviders = kernelProviders ?? throw new ArgumentNullException(nameof(kernelProviders));
        }

        /// <summary>
        /// ETW session data.
        /// </summary>
        public EtwSessionData UserSessionData { get; }

        /// <summary>
        /// A collection of user providers to be enabled during ETW recording.
        /// </summary>
        public IReadOnlyCollection<EtwUserProvider> UserProviders { get; }

        /// <summary>
        /// A collection of kernel providers to be enabled during ETW recording.
        /// </summary>
        public IReadOnlyCollection<EtwKernelProvider> KernelProviders { get; }

        /// <summary>
        /// Performs tasks associated with listening ETW events on Windows.
        /// </summary>
        /// <param name="runner">Operation to be profiled/traced.</param>
        /// <returns>The return value of the method that the runner delegate encapsulates.</returns>
        public TResult Record(Func<TResult> runner)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));

            var fi = new FileInfo(UserSessionData.FileName);
            var kernelFileName = Path.Combine($"{fi.DirectoryName}", $"{Path.GetFileNameWithoutExtension(fi.Name)}.kernel.etl");
            var kernelProvider = KernelProviders.Aggregate(EtwKernelProvider.Default,
                (current, item) => new EtwKernelProvider {
                    Flags = current.Flags | item.Flags,
                    StackCapture = current.StackCapture | item.StackCapture,
                });
            var needKernelSession = EtwHelper.NeedSeparateKernelSession(kernelProvider);

            if (needKernelSession && !EtwHelper.CanEnableKernelProvider)
            {
                const string message = "The application is required to run as Administrator in order to capture kernel data.";
                WriteErrorLine(message);
                throw new InvalidOperationException(message);
            }

            TResult result;
            WriteDebugLine("ETW capture start.");
            using (var kernelSession = needKernelSession ? EtwHelper.MakeKernelSession(kernelFileName, UserSessionData.BufferSizeMB) : null)
            {
                kernelSession?.EnableKernelProvider(kernelProvider.Flags, kernelProvider.StackCapture);

                using (var userSession = new EtwSession(UserSessionData))
                {
                    UserProviders.ForEach(provider => {
                        userSession.EnableProvider(provider.Guid, provider.Level, provider.Keywords, provider.Options);
                    });

                    result = runner();
                }
            }
            WriteDebugLine("ETW capture stop.");

            WriteDebugLine("ETW merge start.");
            TraceEventSession.MergeInPlace(UserSessionData.FileName, Console.Out);
            WriteDebugLine("ETW merge stop.");

            return result;
        }
    }
}
