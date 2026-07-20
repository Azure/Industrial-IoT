// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    [Trait("Category", "ServerIntegration")]
    public sealed class ReferenceServerPublishTests : IClassFixture<ReferenceServer>
    {
        public ReferenceServerPublishTests(ReferenceServer server)
        {
            _server = server;
        }

        [Fact]
        public async Task PublishesInitialValueWhileFixtureDataClockIsFrozenAsync()
        {
            var fixtureTime = _server.Now;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var handle = await _server.Client.AcquireSessionAsync(
                _server.GetConnection(), ct: cts.Token);
            var session = Assert.IsType<OpcUaSession>(handle.Session);
            uint subscriptionId = 0;

            try
            {
                var createResponse = await session.CreateSubscriptionAsync(
                    CreateRequestHeader(),
                    100,
                    100,
                    2,
                    0,
                    true,
                    0,
                    cts.Token);
                Assert.True(StatusCode.IsGood(createResponse.ResponseHeader.ServiceResult));
                subscriptionId = createResponse.SubscriptionId;

                var expandedNodeId = ExpandedNodeId.Parse(kBoilerOutputNodeId);
                var nodeId = ExpandedNodeId.ToNodeId(
                    expandedNodeId,
                    handle.Session.MessageContext.NamespaceUris);
                Assert.False(nodeId.IsNull);

                var createItemsResponse = await session.CreateMonitoredItemsAsync(
                    CreateRequestHeader(),
                    subscriptionId,
                    TimestampsToReturn.Both,
                    [
                        new MonitoredItemCreateRequest
                        {
                            ItemToMonitor = new ReadValueId
                            {
                                NodeId = nodeId,
                                AttributeId = Attributes.Value
                            },
                            MonitoringMode = MonitoringMode.Reporting,
                            RequestedParameters = new MonitoringParameters
                            {
                                ClientHandle = 1,
                                SamplingInterval = 0,
                                QueueSize = 1,
                                DiscardOldest = true
                            }
                        }
                    ],
                    cts.Token);
                var createResult = Assert.Single(
                    createItemsResponse.Results.ToArray());
                Assert.True(StatusCode.IsGood(createResult.StatusCode));

                DataChangeNotification? dataChange = null;
                ArrayOf<SubscriptionAcknowledgement> acknowledgements = [];
                for (var attempt = 0; attempt < 5 && dataChange == null; attempt++)
                {
                    var publishResponse = await session.PublishAsync(
                        CreateRequestHeader(),
                        acknowledgements,
                        cts.Token);
                    Assert.True(StatusCode.IsGood(publishResponse.ResponseHeader.ServiceResult));
                    Assert.Equal(subscriptionId, publishResponse.SubscriptionId);

                    acknowledgements =
                    [
                        new SubscriptionAcknowledgement
                        {
                            SubscriptionId = subscriptionId,
                            SequenceNumber = publishResponse.NotificationMessage.SequenceNumber
                        }
                    ];

                    foreach (var notificationData in publishResponse.NotificationMessage.NotificationData)
                    {
                        if (notificationData.TryGetValue(out DataChangeNotification notification))
                        {
                            dataChange = notification;
                            break;
                        }
                    }
                }

                Assert.NotNull(dataChange);
                var monitoredItem = Assert.Single(
                    dataChange.MonitoredItems.ToArray());
                Assert.Equal(1u, monitoredItem.ClientHandle);
                Assert.True(StatusCode.IsGood(monitoredItem.Value.StatusCode));
                Assert.True(monitoredItem.Value.WrappedValue.TryGetValue(out double value));
                Assert.False(double.IsNaN(value));
                Assert.Equal(fixtureTime, _server.Now);
            }
            finally
            {
                if (subscriptionId != 0)
                {
                    await session.DeleteSubscriptionsAsync(
                        CreateRequestHeader(),
                        [subscriptionId],
                        CancellationToken.None);
                }
            }
        }

        private static RequestHeader CreateRequestHeader()
        {
            return new RequestHeader
            {
                Timestamp = DateTime.UtcNow,
                TimeoutHint = 5000
            };
        }

        private readonly ReferenceServer _server;
        private const string kBoilerOutputNodeId =
            "nsu=http://opcfoundation.org/UA/Boiler/;i=1257";
    }
}
