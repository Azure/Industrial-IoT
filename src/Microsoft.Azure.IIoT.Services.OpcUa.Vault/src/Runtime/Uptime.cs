// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime {
using System;

    /// <summary>
    /// Simple helper capturing uptime information
    /// </summary>
    public static class Uptime {
        /// <summary>
        /// When the service started
        /// </summary>
        public static DateTime Start { get; } = DateTime.UtcNow;

        /// <summary>
        /// How long the service has been running
        /// </summary>
        public static TimeSpan Duration => DateTime.UtcNow.Subtract(Start);

        /// <summary>
        /// A randomly generated ID used to identify the process in the logs
        /// </summary>
        public static string ProcessId { get; } = "WebService." + Guid.NewGuid();
    }
}
