// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Scanner interface
    /// </summary>
    public interface IScanner : IDisposable {

        /// <summary>
        /// Number of currently active probes
        /// </summary>
        int ActiveProbes { get; }

        /// <summary>
        /// Items scanned so far
        /// </summary>
        int ScanCount { get; }

        /// <summary>
        /// Task that completes when scan is done.
        /// </summary>
        Task Completion { get; }
    }
}