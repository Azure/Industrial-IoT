// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Furly.Azure.IoT.Edge;
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Configure edge client
    /// </summary>
    public sealed class IoTEdgeClientConfig : ConfigureOptionBase<IoTEdgeClientOptions>
    {
        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string HubTransport = "Transport";
        public const string EdgeHubConnectionString = "EdgeHubConnectionString";
        public const string BypassCertVerificationKey = "BypassCertVerification";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void Configure(string name, IoTEdgeClientOptions options)
        {
            if (string.IsNullOrEmpty(options.EdgeHubConnectionString))
            {
                options.EdgeHubConnectionString = GetStringOrDefault(EdgeHubConnectionString);
            }
            if (options.EdgeHubConnectionString == null &&
                Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID") != null)
            {
                options.EdgeHubConnectionString = string.Empty;
            }
            if (options.Transport == TransportOption.None &&
                Enum.TryParse<TransportOption>(GetStringOrDefault(HubTransport),
                    out var transport))
            {
                options.Transport = transport;
            }
            options.Product = $"OpcPublisher_{GetType().Assembly.GetReleaseVersion()}";
        }

        /// <summary>
        /// Transport configuration
        /// </summary>
        /// <param name="configuration"></param>
        public IoTEdgeClientConfig(IConfiguration configuration)
            : base(configuration)
        {
        }
    }
}
