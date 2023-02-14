// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests.v2.Twin.Api {
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Xunit;

    [CollectionDefinition(Name)]
    public class TestServerWriteCollection : ICollectionFixture<TestServerFixture> {

        public const string Name = "Supervisor.Api.Write";
    }
}
