// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Autofac;
    using System;
    using System.IO;
    using Xunit.Abstractions;

    public sealed class PublisherModuleMqttv5Fixture : IDisposable
    {
        public IContainer SdkContainer => _publisher.ClientContainer;

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="messageSink"></param>
        public PublisherModuleMqttv5Fixture(IMessageSink messageSink)
        {
            _publisher = new PublisherModule(messageSink, version: Furly.Extensions.Mqtt.MqttVersion.v5);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _publisher.Dispose();
        }

        private readonly PublisherModule _publisher;
    }

    public sealed class PublisherModuleMqttv311Fixture : IDisposable
    {
        public IContainer SdkContainer => _publisher.ClientContainer;

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="messageSink"></param>
        public PublisherModuleMqttv311Fixture(IMessageSink messageSink)
        {
            _publisher = new PublisherModule(messageSink, version: Furly.Extensions.Mqtt.MqttVersion.v311);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _publisher.Dispose();
        }

        private readonly PublisherModule _publisher;
    }

    public sealed class PublisherModuleFixture : IDisposable
    {
        public IContainer SdkContainer => _publisher.ClientContainer;

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="messageSink"></param>
        public PublisherModuleFixture(IMessageSink messageSink)
        {
            _publisher = new PublisherModule(messageSink);
        }

        /// <summary>
        /// Create rest client scope
        /// </summary>
        /// <param name="output"></param>
        /// <param name="serializerType"></param>
        /// <returns></returns>
        public IContainer CreateRestClientContainer(ITestOutputHelper output,
            TestSerializerType serializerType)
        {
            return _publisher.CreateClientScope(output, serializerType);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _publisher.Dispose();
        }

        private readonly PublisherModule _publisher;
    }
}
