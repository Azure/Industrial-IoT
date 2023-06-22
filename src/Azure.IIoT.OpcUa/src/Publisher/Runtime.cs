// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using System;

    /// <summary>
    /// Runtime operations
    /// </summary>
    public static class Runtime
    {
        /// <summary>
        /// Crash the process with optional exception
        /// </summary>
        public static Action<string, Exception?> FailFast { get; set; }
            = Environment.FailFast;

        /// <summary>
        /// Exit process
        /// </summary>
        public static Action<int> Exit { get; set; }
            = Environment.Exit;
    }
}
