// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures {
    using Xunit;
    using Azure.IIoT.OpcUa.Api.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Azure.IIoT.OpcUa.Protocol;

    [CollectionDefinition(Name)]
    public class ReferenceServerReadCollection : ICollectionFixture<ReferenceServerFixture> {

        public const string Name = "Read";
    }
}
