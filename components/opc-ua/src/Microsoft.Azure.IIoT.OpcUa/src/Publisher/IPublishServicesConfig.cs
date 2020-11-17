// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {

    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Configuration for job definition creation logic.
    /// </summary>
    public interface IPublishServicesConfig {

        /// <summary>
        /// Default batch trigger interval for OPC Publisher module.
        /// </summary>
        TimeSpan DefaultBatchTriggerInterval { get; }

        /// <summary>
        /// Default batch trigger size for OPC Publisher module.
        /// </summary>
        int DefaultBatchSize { get; }

        /// <summary>
        /// Default max egress message queue size for OPC Publisher module.
        /// </summary>
        int DefaultMaxEgressMessageQueue { get; }


        /// <summary>
        /// Default messaging mode for jobs of OPC Publisher module.
        /// </summary>
        MessagingMode DefaultMessagingMode { get; }

        /// <summary>
        /// Default message encoding for jobs of OPC Publisher module.
        /// </summary>
        MessageEncoding DefaultMessageEncoding { get; }
    }
}
