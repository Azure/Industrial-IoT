// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;

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

        /// <summary>
        /// Default values for transport quotas.
        /// </summary>
        public const int DefaultSecurityTokenLifetime = 60 * 60 * 1000;
        public const int DefaultChannelLifetime = 300 * 1000;
        public const int DefaultMaxBufferSize = (64 * 1024) - 1;
        public const int DefaultMaxMessageSize = 4 * 1024 * 1024;
        public const int DefaultMaxArrayLength = (64 * 1024) - 1;
        public const int DefaultMaxByteStringLength = 1024 * 1024;
        public const int DefaultMaxStringLength = (128 * 1024) - 256;
        public const int DefaultOperationTimeout = 120 * 1000;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member



        /// <inheritdoc/>
        public int OperationTimeout =>
            GetIntOrDefault(OperationTimeoutKey, DefaultOperationTimeout);
        /// <inheritdoc/>
        public int MaxStringLength =>
            GetIntOrDefault(MaxStringLengthKey, DefaultMaxStringLength);
        /// <inheritdoc/>
        public int MaxByteStringLength =>
            GetIntOrDefault(MaxByteStringLengthKey, DefaultMaxByteStringLength);
        /// <inheritdoc/>
        public int MaxArrayLength =>
            GetIntOrDefault(MaxArrayLengthKey, DefaultMaxArrayLength);
        /// <inheritdoc/>
        public int MaxMessageSize =>
            GetIntOrDefault(MaxMessageSizeKey, DefaultMaxMessageSize);
        /// <inheritdoc/>
        public int MaxBufferSize =>
            GetIntOrDefault(MaxBufferSizeKey, DefaultMaxBufferSize);
        /// <inheritdoc/>
        public int ChannelLifetime =>
            GetIntOrDefault(ChannelLifetimeKey, DefaultChannelLifetime);
        /// <inheritdoc/>
        public int SecurityTokenLifetime =>
            GetIntOrDefault(SecurityTokenLifetimeKey, DefaultSecurityTokenLifetime);

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="configuration"></param>
        public TransportQuotaConfig(IConfiguration configuration = null) :
            base(configuration) {
        }

        /// <summary>
        /// Return service defaults for the TransportQuotas.
        /// </summary>
        public static TransportQuotas DefaultTransportQuotas() {
            return new TransportQuotas {
                MaxMessageSize = TransportQuotaConfig.DefaultMaxMessageSize,
                OperationTimeout = TransportQuotaConfig.DefaultOperationTimeout,
                MaxStringLength = TransportQuotaConfig.DefaultMaxStringLength,
                MaxByteStringLength = TransportQuotaConfig.DefaultMaxByteStringLength,
                MaxArrayLength = TransportQuotaConfig.DefaultMaxArrayLength,
                MaxBufferSize = TransportQuotaConfig.DefaultMaxBufferSize,
                ChannelLifetime = TransportQuotaConfig.DefaultChannelLifetime,
                SecurityTokenLifetime = TransportQuotaConfig.DefaultSecurityTokenLifetime
            };
        }
    }
}