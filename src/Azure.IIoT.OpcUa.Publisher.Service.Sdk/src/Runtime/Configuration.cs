// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR;
    using Autofac;
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Sdk configuration
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Configure service sdk
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <param name="signalR"></param>
        /// <returns></returns>
        public static ContainerBuilder ConfigureServiceSdk(this ContainerBuilder builder,
            Action<ServiceSdkOptions> configure = null,
            Action<SignalRClientOptions> signalR = null)
        {
            builder.AddOptions();
            if (configure != null)
            {
                builder.Configure(configure);
            }

            if (signalR != null)
            {
                builder.Configure(signalR);
            }

            builder.RegisterType<SdkConfig>()
                .AsImplementedInterfaces().SingleInstance();
            return builder;
        }

        /// <summary>
        /// Sdk configurator
        /// </summary>
        internal sealed class SdkConfig : PostConfigureOptionBase<ServiceSdkOptions>
        {
            /// <summary>
            /// Configuration keys
            /// </summary>
            private const string kServiceUrlKey = "ServiceUrl";

            /// <inheritdoc/>
            public override void PostConfigure(string name, ServiceSdkOptions options)
            {
                if (options.ServiceUrl == null)
                {
                    options.ServiceUrl = GetStringOrDefault(kServiceUrlKey,
                        GetStringOrDefault(EnvVars.PCS_PUBLISHER_SERVICE_URL,
                        GetServiceUrl("9045", "publisher")));
                }
            }

            /// <inheritdoc/>
            public SdkConfig(IConfiguration configuration) :
                base(configuration)
            {
            }

            /// <summary>
            /// Get endpoint url
            /// </summary>
            /// <param name="port"></param>
            /// <param name="path"></param>
            /// <returns></returns>
            private string GetServiceUrl(string port, string path)
            {
                var cloudEndpoint = GetStringOrDefault(EnvVars.PCS_SERVICE_URL)?.Trim()?.TrimEnd('/');
                if (string.IsNullOrEmpty(cloudEndpoint))
                {
                    // Test port is open
                    if (!int.TryParse(port, out var nPort))
                    {
                        return $"http://localhost:9080/{path}";
                    }
                    using (var socket = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Unspecified))
                    {
                        try
                        {
                            socket.Connect(IPAddress.Loopback, nPort);
                            return $"http://localhost:{port}";
                        }
                        catch
                        {
                            return $"http://localhost:9080/{path}";
                        }
                    }
                }
                return $"{cloudEndpoint}/{path}";
            }
        }
    }
}
