﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller.HistoricalEvents.MsgPack
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Xunit;

    [CollectionDefinition(Name)]
    public sealed class ReadCollection : ICollectionFixture<HistoricalEventsServer>
    {
        public const string Name = "HistoricalEventsServerReadRestMsgPack";
    }
}
