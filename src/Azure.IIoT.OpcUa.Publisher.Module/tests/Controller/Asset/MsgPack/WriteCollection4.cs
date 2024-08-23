// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.Asset.MsgPack
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Xunit;

    [CollectionDefinition(Name)]
    public class WriteCollection4 : ICollectionFixture<AssetServer>
    {
        public const string Name = "AssetWrite4RestMsgPack";
    }
}
