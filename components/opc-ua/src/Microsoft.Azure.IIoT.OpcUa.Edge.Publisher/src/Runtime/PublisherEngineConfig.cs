// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime {
    using System;

    /// <summary>
    /// Publisher processing engine configuration
    /// </summary>
    public class PublisherEngineConfig : IEngineConfiguration {

        /// <inheritdoc/>
        public int? BatchSize { get; set; }

        /// <inheritdoc/>
        public TimeSpan? DiagnosticsInterval { get; set; }
    }
}