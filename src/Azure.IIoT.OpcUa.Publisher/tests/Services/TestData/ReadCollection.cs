// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services.TestData
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Xunit;

    [CollectionDefinition(Name)]
    public class ReadCollection : ICollectionFixture<TestDataServer>
    {
        public const string Name = "TestDataRead";
    }
}