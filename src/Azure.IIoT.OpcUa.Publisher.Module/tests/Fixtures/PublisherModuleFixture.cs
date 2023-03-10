// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Xunit.Abstractions;

    public sealed class PublisherModuleFixture : PublisherModule
    {
        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="messageSink"></param>
        public PublisherModuleFixture(IMessageSink messageSink) : base(messageSink)
        {
            // No op
        }
    }
}
