// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.v2.Twin.Api {
    using Xunit;
    using Azure.IIoT.OpcUa.Api.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;

    [CollectionDefinition(Name)]
    public class TestServerWriteCollection : ICollectionFixture<TestServerFixture> {

        public const string Name = "Supervisor.Api.Write";
    }
}
