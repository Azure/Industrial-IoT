// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Mqtt.HistoricalAccess
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Testing.Tests;
    using Autofac;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [Collection(ReadCollection.Name)]
    public class ReadModifiedTests : TwinIntegrationTestBase, IClassFixture<PublisherModuleMqttv311Fixture>
    {
        public ReadModifiedTests(HistoricalAccessServer server,
            PublisherModuleMqttv311Fixture module, ITestOutputHelper output) : base(output)
        {
            _server = server;
            _module = module;
        }

        private HistoryReadValuesModifiedTests<ConnectionModel> GetTests()
        {
            return new HistoryReadValuesModifiedTests<ConnectionModel>(_server,
                _module.SdkContainer.Resolve<IHistoryServices<ConnectionModel>>,
                _server.GetConnection());
        }

        private readonly HistoricalAccessServer _server;
        private readonly PublisherModuleMqttv311Fixture _module;

        [Fact]
        public Task HistoryReadInt16ValuesModifiedTestAsync()
        {
            return GetTests().HistoryReadInt16ValuesModifiedTestAsync(Ct);
        }

        [SkippableFact]
        public Task HistoryStreamInt16ValuesModifiedTestAsync()
        {
            Skip.If(true, "Not yet supported");
            return GetTests().HistoryStreamInt16ValuesModifiedTestAsync(Ct);
        }
    }
}
