// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Xunit;

    [CollectionDefinition(Name)]
    public class SignalRCollection : ICollectionFixture<SignalRTestFixture>
    {
        public const string Name = "SignalRTest";
    }
}
