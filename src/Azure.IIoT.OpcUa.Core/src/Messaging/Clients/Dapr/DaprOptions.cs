// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    using Grpc.Net.Client;

    /// <summary>
    /// Dapr configuration.
    /// </summary>
    public sealed class DaprOptions
    {
        /// <summary>
        /// The pub sub component to use. If not specified the first part of the
        /// topic path will be used.
        /// </summary>
        public string? PubSubComponent { get; set; }

        /// <summary>
        /// The name of the state store to use. If not specified, "default" is used.
        /// </summary>
        public string? StateStoreName { get; set; }

        /// <summary>
        /// Api token secret.
        /// </summary>
        public string? ApiToken { get; set; }

        /// <summary>
        /// Http endpoint to use.
        /// </summary>
        public string? HttpEndpoint { get; set; }

        /// <summary>
        /// Grpc endpoint to use.
        /// </summary>
        public string? GrpcEndpoint { get; set; }

        /// <summary>
        /// Max message size. Default is 512 MB.
        /// </summary>
        public int? MessageMaxBytes { get; set; }

        /// <summary>
        /// Check and wait until the side car is healthy before sending requests.
        /// </summary>
        public bool CheckSideCarHealthBeforeAccess { get; set; }

        /// <summary>
        /// Grpc channel options.
        /// </summary>
        public GrpcChannelOptions GrpcChannelOptions { get; } = new();
    }
}
