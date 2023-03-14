// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module
{
    using Azure.IIoT.OpcUa.Publisher.Module.Controller;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher;
    using Furly.Tunnel.Router.Services;
    using Autofac;
    using Microsoft.Extensions.Configuration;
    using Furly.Azure.IoT.Edge;
    using Furly.Extensions.Mqtt;

    /// <summary>
    /// Container builder extensions
    /// </summary>
    public static class ContainerBuilderEx
    {
        /// <summary>
        /// Add all publisher dependencies minus connectivity components.
        /// </summary>
        /// <param name="builder"></param>
        public static void AddPublisherServices(this ContainerBuilder builder)
        {
            builder.AddNewtonsoftJsonSerializer();
            builder.AddDiagnostics();
            builder.AddPublisherCore();

            builder.RegisterType<PublisherCliOptions>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterType<LoggerFilterConfig>()
                .AsImplementedInterfaces();

            // Register and configure controllers
            builder.RegisterType<MethodRouter>()
                .AsImplementedInterfaces().SingleInstance()
                .PropertiesAutowired(
                    PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType<MethodRouterConfig>()
                .AsImplementedInterfaces();

            builder.RegisterType<PublisherMethodsController>()
                .AsImplementedInterfaces();
            builder.RegisterType<TwinMethodsController>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryMethodsController>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryMethodsController>()
                .AsImplementedInterfaces();
        }

        /// <summary>
        /// Add transports
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        public static void AddTransports(this ContainerBuilder builder,
            IConfiguration configuration)
        {
            var mqttOptions = new MqttOptions();
            new MqttBrokerConfig(configuration).Configure(mqttOptions);
            if (mqttOptions.HostName != null)
            {
                builder.AddMqttClient();
                builder.RegisterType<MqttBrokerConfig>()
                    .AsImplementedInterfaces();
            }

            // Validate edge configuration
            var iotEdgeOptions = new IoTEdgeClientOptions();
            new IoTEdgeClientConfig(configuration).Configure(iotEdgeOptions);
            if (iotEdgeOptions.EdgeHubConnectionString != null)
            {
                builder.AddIoTEdgeServices();
                builder.RegisterType<IoTEdgeClientConfig>()
                    .AsImplementedInterfaces();
            }
        }
    }
}
