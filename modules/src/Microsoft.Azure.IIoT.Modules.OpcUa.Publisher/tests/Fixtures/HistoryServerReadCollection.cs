// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests.Fixtures {
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Xunit;

    [CollectionDefinition(Name)]
    public class HistoryServerReadCollection : ICollectionFixture<HistoryServerFixture> {

        public const string Name = "HistoryServerRead";
    }
}
