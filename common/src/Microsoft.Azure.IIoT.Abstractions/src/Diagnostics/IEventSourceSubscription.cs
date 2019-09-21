// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Configure or disable subscription
    /// </summary>
    public interface IEventSourceSubscription : IDisposable {

        /// <summary>
        /// Event level
        /// </summary>
        EventLevel Level { get; }
    }
}

