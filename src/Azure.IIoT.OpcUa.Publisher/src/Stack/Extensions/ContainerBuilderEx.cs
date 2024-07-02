// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Encoders;
    using Autofac;

    /// <summary>
    /// Container builder extensions
    /// </summary>
    public static class ContainerBuilderEx
    {
        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="builder"></param>
        public static void AddOpcUaStack(this ContainerBuilder builder)
        {
            builder.RegisterInstance(IMetricsContext.Empty)
                .AsImplementedInterfaces().IfNotRegistered(typeof(IMetricsContext));

            builder.RegisterType<OpcUaStack>()
                .AsImplementedInterfaces().SingleInstance().AutoActivate();
            builder.RegisterType<OpcUaStackKeySetLogger>()
                .AsImplementedInterfaces().SingleInstance().AutoActivate();
            builder.RegisterType<OpcUaApplication>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<OpcUaClientManager>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<OpcUaClientConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<OpcUaSubscriptionConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();
            builder.RegisterType<ConsoleWriter>()
                .AsImplementedInterfaces();
            builder.RegisterType<AvroFileWriter>()
                .AsImplementedInterfaces();
            builder.RegisterType<ZipFileWriter>()
                .AsImplementedInterfaces();

            builder.RegisterType<FilterQueryParser>()
                .AsImplementedInterfaces();
        }
    }
}
