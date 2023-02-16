// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests.Api {
    using Azure.IIoT.OpcUa.Api.Clients;
    using Azure.IIoT.OpcUa.Api.Models;
    using Azure.IIoT.OpcUa.Api.Publisher.Adapter;
    using Azure.IIoT.OpcUa.Protocol;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Testing.Tests;
    using Xunit;

    [CollectionDefinition(Name)]
    public class ReadBinaryCollection : ICollectionFixture<TestServerFixture> {
        public const string Name = "ReadBinaryApi";
    }
}
