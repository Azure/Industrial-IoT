// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using System;
    using Xunit.Abstractions;

    public sealed class PublisherModuleMqttv5Fixture : IDisposable
    {
        public TestContainer SdkContainer => _publisher.ClientContainer;

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="messageSink"></param>
        public PublisherModuleMqttv5Fixture(IMessageSink messageSink)
        {
            _publisher = new PublisherModule(messageSink, version: MqttVersion.v5);
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
        public TestContainer SdkContainer => _publisher.ClientContainer;

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="messageSink"></param>
        public PublisherModuleMqttv311Fixture(IMessageSink messageSink)
        {
            _publisher = new PublisherModule(messageSink, version: MqttVersion.v311);
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
        public TestContainer SdkContainer => _publisher.ClientContainer;

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
        public TestContainer CreateRestClientContainer(ITestOutputHelper output,
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
