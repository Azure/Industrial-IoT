// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using Xunit;

    public sealed class IoTEdgeTwinStoreTests
    {
        [Fact]
        public void ConstructorRejectsNullClientBeforeStartingSynchronization()
        {
            Assert.Throws<ArgumentNullException>(() => new IoTEdgeTwinStore(
                null!, NullLogger<IoTEdgeTwinStore>.Instance));
        }
    }
}
