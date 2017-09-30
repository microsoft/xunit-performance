﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    internal sealed class EtwManagedMethod
    {
        public long Id { get; set; }

        public string MethodName { get; set; }

        public string MethodNamespace { get; set; }

        internal bool IsLoaded { get; set; } = false;

        internal bool IsDynamic { get; set; }

        internal bool IsGeneric { get; set; }

        internal bool IsJitted { get; set; }

        /// <summary>
        /// Represents the address range where this method was loaded.
        /// </summary>
        internal EtwAddressRange AddressRange { get; set; }
    }
}
