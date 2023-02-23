// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures {
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Xunit;

    [CollectionDefinition(Name)]
    public class HistoryServerReadCollection : ICollectionFixture<HistoryServerFixture> {
        public const string Name = "HistoryServerRead";
    }
}
