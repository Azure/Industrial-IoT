// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Opc.Ua;

    /// <summary>
    /// Transport quota config extensions
    /// </summary>
    public static class TransportQuotaConfigEx {

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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

        /// <summary>
        /// Return service defaults for the TransportQuotas.
        /// </summary>
        public static TransportQuotas DefaultTransportQuotas() {
            return new TransportQuotas {
                MaxMessageSize = DefaultMaxMessageSize,
                OperationTimeout = DefaultOperationTimeout,
                MaxStringLength = DefaultMaxStringLength,
                MaxByteStringLength = DefaultMaxByteStringLength,
                MaxArrayLength = DefaultMaxArrayLength,
                MaxBufferSize = DefaultMaxBufferSize,
                ChannelLifetime = DefaultChannelLifetime,
                SecurityTokenLifetime = DefaultSecurityTokenLifetime
            };
        }

        /// <summary>
        /// Convert to transport quota
        /// </summary>
        /// <param name="transportQuotaConfig"></param>
        /// <returns></returns>
        public static TransportQuotas ToTransportQuotas(this ITransportQuotaConfig transportQuotaConfig) {
            var transportQuotas = new TransportQuotas {
                OperationTimeout = transportQuotaConfig.OperationTimeout,
                MaxStringLength = transportQuotaConfig.MaxStringLength,
                MaxByteStringLength = transportQuotaConfig.MaxByteStringLength,
                MaxArrayLength = transportQuotaConfig.MaxArrayLength,
                MaxMessageSize = transportQuotaConfig.MaxMessageSize,
                MaxBufferSize = transportQuotaConfig.MaxBufferSize,
                ChannelLifetime = transportQuotaConfig.ChannelLifetime,
                SecurityTokenLifetime = transportQuotaConfig.SecurityTokenLifetime
            };
            return transportQuotas;
        }

        /// <summary>
        /// Convert to endpoint configuration
        /// </summary>
        /// <param name="transportQuotaConfig"></param>
        /// <returns></returns>
        public static EndpointConfiguration ToEndpointConfiguration(
            this ITransportQuotaConfig transportQuotaConfig) {
            var endpointConfiguration = new EndpointConfiguration {
                OperationTimeout = transportQuotaConfig.OperationTimeout,
                UseBinaryEncoding = true,
                MaxArrayLength = transportQuotaConfig.MaxArrayLength,
                MaxByteStringLength = transportQuotaConfig.MaxByteStringLength,
                MaxMessageSize = transportQuotaConfig.MaxMessageSize,
                MaxStringLength = transportQuotaConfig.MaxStringLength,
                MaxBufferSize = transportQuotaConfig.MaxBufferSize,
                ChannelLifetime = transportQuotaConfig.ChannelLifetime,
                SecurityTokenLifetime = transportQuotaConfig.SecurityTokenLifetime
            };
            return endpointConfiguration;
        }
    }
}