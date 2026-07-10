// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using global::Dapr.Client;

    internal static class DaprClientExtensions
    {
        /// <summary>
        /// Create Dapr client.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="useJsonOptions"></param>
        /// <returns></returns>
        public static DaprClient CreateClient(this DaprOptions options,
            bool useJsonOptions = false)
        {
            var builder = new DaprClientBuilder()
                .UseGrpcChannelOptions(options.GrpcChannelOptions);
            if (options.ApiToken != null)
            {
                builder.UseDaprApiToken(options.ApiToken);
            }
            if (options.HttpEndpoint != null)
            {
                builder.UseHttpEndpoint(options.HttpEndpoint);
            }
            if (options.GrpcEndpoint != null)
            {
                builder.UseGrpcEndpoint(options.GrpcEndpoint);
            }
            if (useJsonOptions)
            {
                builder.UseJsonSerializationOptions(Json.Options);
            }
            return builder.Build();
        }
    }
}
