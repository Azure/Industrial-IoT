// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Transport quota configuration
    /// </summary>
    public class TransportQuotaConfig : ConfigBase, ITransportQuotaConfig {

        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string SecurityTokenLifetimeKey = "SecurityTokenLifetime";
        public const string ChannelLifetimeKey = "ChannelLifetime";
        public const string MaxBufferSizeKey = "MaxBufferSize";
        public const string MaxMessageSizeKey = "MaxMessageSize";
        public const string MaxArrayLengthKey = "MaxArrayLength";
        public const string MaxByteStringLengthKey = "MaxByteStringLength";
        public const string MaxStringLengthKey = "MaxStringLength";
        public const string OperationTimeoutKey = "OperationTimeout";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public int OperationTimeout =>
            GetIntOrDefault(OperationTimeoutKey, 120000);
        /// <inheritdoc/>
        public int MaxStringLength =>
            GetIntOrDefault(MaxStringLengthKey, (128 * 1024) - 256);
        /// <inheritdoc/>
        public int MaxByteStringLength =>
            GetIntOrDefault(MaxByteStringLengthKey, ushort.MaxValue);
        /// <inheritdoc/>
        public int MaxArrayLength =>
            GetIntOrDefault(MaxArrayLengthKey, ushort.MaxValue);
        /// <inheritdoc/>
        public int MaxMessageSize =>
            GetIntOrDefault(MaxMessageSizeKey, 4 * 1024 * 1024);
        /// <inheritdoc/>
        public int MaxBufferSize =>
            GetIntOrDefault(MaxBufferSizeKey, ushort.MaxValue);
        /// <inheritdoc/>
        public int ChannelLifetime =>
            GetIntOrDefault(ChannelLifetimeKey, 600000);
        /// <inheritdoc/>
        public int SecurityTokenLifetime =>
            GetIntOrDefault(SecurityTokenLifetimeKey, 3600000);

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="configuration"></param>
        public TransportQuotaConfig(IConfiguration configuration = null) :
            base(configuration) {
        }

    }
}