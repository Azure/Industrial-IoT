// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {

    /// <summary>
    /// Transport quota configuration
    /// </summary>
    public interface ITransportQuotaConfig {

        /// <summary>
        /// Channel lifetime
        /// </summary>
        int ChannelLifetime { get; }

        /// <summary>
        /// Max array length
        /// </summary>
        int MaxArrayLength { get; }

        /// <summary>
        /// Max buffer size
        /// </summary>
        int MaxBufferSize { get; }

        /// <summary>
        /// Max string length
        /// </summary>
        int MaxByteStringLength { get; }

        /// <summary>
        /// Max message size
        /// </summary>
        int MaxMessageSize { get; }

        /// <summary>
        /// Max string length
        /// </summary>
        int MaxStringLength { get; }

        /// <summary>
        /// Operation timeout
        /// </summary>
        int OperationTimeout { get; }

        /// <summary>
        /// Security token lifetime
        /// </summary>
        int SecurityTokenLifetime { get; }
    }
}