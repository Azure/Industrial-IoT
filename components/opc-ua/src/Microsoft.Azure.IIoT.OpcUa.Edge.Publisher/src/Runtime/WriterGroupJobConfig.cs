// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Configuration for WriterGroup jobs
    /// </summary>
    public class WriterGroupJobConfig : IWriterGroupConfig,
        IEngineConfiguration {

        /// <inheritdoc/>
        public WriterGroupModel WriterGroup { get; set; }

        /// <inheritdoc/>
        public int? BatchSize { get; set; }

        /// <inheritdoc/>
        public TimeSpan? BatchTriggerInterval { get; set; }

        /// <inheritdoc/>
        public int? MaxMessageSize { get; set; }

        /// <inheritdoc/>
        public TimeSpan? DiagnosticsInterval { get; set; }

        /// <inheritdoc/>
        public string PublisherId { get; set; }

        /// <inheritdoc/>
        public int? MaxOutgressMessages { get; set; }

        /// <inheritdoc/>
        public bool EnableRoutingInfo { get; set; }

        /// <inheritdoc/>
        public bool UseStandardsCompliantEncoding { get; set; }
    }
}
