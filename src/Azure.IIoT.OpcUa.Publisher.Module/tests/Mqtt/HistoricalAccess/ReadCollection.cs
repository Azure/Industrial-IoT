// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.HistoricalAccess
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Xunit;

    [CollectionDefinition(Name, DisableParallelization = true)]
    public class ReadCollection : ICollectionFixture<HistoricalAccessServer>
    {
        public const string Name = "HistoricalAccessServerReadModuleMqtt";
    }
}
