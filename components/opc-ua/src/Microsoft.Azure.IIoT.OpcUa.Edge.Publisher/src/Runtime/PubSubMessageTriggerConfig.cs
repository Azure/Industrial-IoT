// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Message triggering configuration for pub/sub messages
    /// </summary>
    public class PubSubMessageTriggerConfig : IPubSubMessageTriggerConfig {

        /// <inheritdoc/>
        public List<DataSetModel> DataSets { get; set; }

        /// <inheritdoc/>
        public ConnectionModel Connection { get; set; }

        /// <inheritdoc/>
        public TimeSpan? KeyframeMessageInterval { get; set; }

        /// <inheritdoc/>
        public TimeSpan? MetadataMessageInterval { get; set; }

        /// <inheritdoc/>
        public TimeSpan? PublishingInterval { get; set; }

        /// <inheritdoc/>
        public bool? SendChangeMessages { get; set; }
    }

}