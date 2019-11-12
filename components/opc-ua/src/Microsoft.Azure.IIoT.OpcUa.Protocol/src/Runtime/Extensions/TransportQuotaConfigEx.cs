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
                SecurityTokenLifetime = transportQuotaConfig.MaxArrayLength
            };
            return endpointConfiguration;
        }
    }
}