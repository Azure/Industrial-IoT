// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Message triggering configuration
    /// </summary>
    public interface IPubSubMessageTriggerConfig {

        /// <summary>
        /// The datasets to publish
        /// </summary>
        List<DataSetModel> DataSets { get; }

        /// <summary>
        /// Session information
        /// </summary>
        ConnectionModel Connection { get; }

        /// <summary>
        /// Key frame interval
        /// </summary>
        TimeSpan? KeyframeMessageInterval { get; }

        /// <summary>
        /// Metadata message interval
        /// </summary>
        TimeSpan? MetadataMessageInterval { get; }

        /// <summary>
        /// Publishing interval
        /// </summary>
        TimeSpan? PublishingInterval { get; }

        /// <summary>
        /// Whether to send message changes
        /// </summary>
        bool? SendChangeMessages { get; }
    }
}