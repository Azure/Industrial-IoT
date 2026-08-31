// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    using System;
    using System.Runtime.CompilerServices;
    using Xunit;

    public sealed class DaprStateStoreClientTests
    {
        [Fact]
        public void ConstructorRequiresOptions()
        {
            Assert.Throws<ArgumentNullException>(() => new DaprStateStoreClient(null!,
                null!));
        }

        [Fact]
        public void NameIsDapr()
        {
            var client = Assert.IsType<DaprStateStoreClient>(
                RuntimeHelpers.GetUninitializedObject(typeof(DaprStateStoreClient)));

            Assert.Equal("Dapr", client.Name);
        }
    }
}
