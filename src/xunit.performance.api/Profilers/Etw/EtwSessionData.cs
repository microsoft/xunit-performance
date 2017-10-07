// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Provides a simple interface to ETW session data.
    /// </summary>
    internal sealed class EtwSessionData
    {
        /// <summary>
        /// Initializes a new instance of the EtwSessionData class, using the session name and file name to write to.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fileName"></param>
        public EtwSessionData(string name, string fileName)
        {
            Name = name;
            FileName = fileName;
            BufferSizeMB = DefaultBufferSizeMB;
        }

        /// <summary>
        /// Session name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Name of the file where the ETW events are be written to.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// ETW buffer size used to capture events.
        /// </summary>
        public int BufferSizeMB { get; set; }

        /// <summary>
        /// Default ETW buffer size used to capture events.
        /// </summary>
        private static int DefaultBufferSizeMB => Math.Max(64, Environment.ProcessorCount * 2);
    }
}
