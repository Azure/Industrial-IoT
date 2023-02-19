// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures {
    using Azure.IIoT.OpcUa.Shared.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Xunit;

    [CollectionDefinition(Name)]
    public class ReferenceServerReadCollection : ICollectionFixture<ReferenceServerFixture> {

        public const string Name = "Read";
    }
}
