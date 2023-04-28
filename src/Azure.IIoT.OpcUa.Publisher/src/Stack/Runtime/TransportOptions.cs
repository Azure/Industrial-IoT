// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    /// <summary>
    /// Transport quota configuration
    /// </summary>
    public sealed class TransportOptions
    {
        /// <summary>
        /// Channel lifetime in milliseconds.
        /// </summary>
        public int ChannelLifetime { get; set; }

        /// <summary>
        /// Max array length
        /// </summary>
        public int MaxArrayLength { get; set; }

        /// <summary>
        /// Max buffer size
        /// </summary>
        public int MaxBufferSize { get; set; }

        /// <summary>
        /// Max string length
        /// </summary>
        public int MaxByteStringLength { get; set; }

        /// <summary>
        /// Max message size
        /// </summary>
        public int MaxMessageSize { get; set; }

        /// <summary>
        /// Max string length
        /// </summary>
        public int MaxStringLength { get; set; }

        /// <summary>
        /// Operation timeout in milliseconds.
        /// </summary>
        public int OperationTimeout { get; set; }

        /// <summary>
        /// Security token lifetime in milliseconds.
        /// </summary>
        public int SecurityTokenLifetime { get; set; }
    }
}
