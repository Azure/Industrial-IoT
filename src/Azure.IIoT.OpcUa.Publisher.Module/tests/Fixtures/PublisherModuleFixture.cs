// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Autofac;
    using System;
    using Xunit.Abstractions;

    public sealed class PublisherModuleFixture : IDisposable
    {
        public IContainer ClientContainer => _publisher.ClientContainer;

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="messageSink"></param>
        public PublisherModuleFixture(IMessageSink messageSink)
        {
            _publisher = new PublisherModule(messageSink);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _publisher.Dispose();
        }

        private readonly PublisherModule _publisher;
    }
}
