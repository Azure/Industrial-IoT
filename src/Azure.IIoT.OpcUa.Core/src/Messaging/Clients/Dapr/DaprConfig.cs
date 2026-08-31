// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    using Azure.IIoT.OpcUa.Core.Configuration;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Dapr configuration.
    /// </summary>
    public sealed class DaprConfig : PostConfigureOptionBase<DaprOptions>
    {
        /// <summary>
        /// Create configuration.
        /// </summary>
        /// <param name="configuration"></param>
        public DaprConfig(IConfiguration configuration) :
            base(configuration)
        {
        }

        /// <inheritdoc/>
        public override void PostConfigure(string? name, DaprOptions options)
        {
            if (string.IsNullOrEmpty(options.ApiToken))
            {
                options.ApiToken = GetStringOrDefault(EnvironmentVariable.DAPRAPITOKEN);
            }
            if (string.IsNullOrEmpty(options.GrpcEndpoint))
            {
                options.GrpcEndpoint = GetStringOrDefault(EnvironmentVariable.DAPRGRPCENDPOINT);
            }
            if (string.IsNullOrEmpty(options.HttpEndpoint))
            {
                options.HttpEndpoint = GetStringOrDefault(EnvironmentVariable.DAPRHTTPENDPOINT);
            }

            options.GrpcChannelOptions.ThrowOperationCanceledOnCancellation = true;
        }

        /// <inheritdoc/>
        protected override DaprOptions Bind()
        {
            return new DaprOptions
            {
                PubSubComponent = GetStringOrDefault(nameof(DaprOptions.PubSubComponent)),
                StateStoreName = GetStringOrDefault(nameof(DaprOptions.StateStoreName)),
                ApiToken = GetStringOrDefault(nameof(DaprOptions.ApiToken)),
                HttpEndpoint = GetStringOrDefault(nameof(DaprOptions.HttpEndpoint)),
                GrpcEndpoint = GetStringOrDefault(nameof(DaprOptions.GrpcEndpoint)),
                MessageMaxBytes = GetIntOrNull(nameof(DaprOptions.MessageMaxBytes)),
                CheckSideCarHealthBeforeAccess = GetBoolOrDefault(
                    nameof(DaprOptions.CheckSideCarHealthBeforeAccess))
            };
        }
    }
}
