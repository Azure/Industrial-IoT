// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using Xunit;

    public sealed class IoTEdgeTransportTests
    {
        [Fact]
        public void ConstructorRejectsNullClient()
        {
            Assert.Throws<ArgumentNullException>(() => new IoTEdgeTransport(
                null!, NullLogger<IoTEdgeTransport>.Instance));
        }
    }
}
