// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class NodeHistoricalEventsTests<T>
    {
        /// <summary>
        /// Create history services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public NodeHistoricalEventsTests(Func<INodeServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task GetServerCapabilitiesTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var results = await services.GetServerCapabilitiesAsync(_connection,
                null, ct).ConfigureAwait(false);

            Assert.NotNull(results);
            Assert.NotNull(results.AggregateFunctions);
            Assert.Equal(37, results.AggregateFunctions?.Count);
            Assert.Single(results.SupportedLocales!);
            Assert.Equal("en-US", results.SupportedLocales?[0]);
            Assert.NotEmpty(results.ServerProfiles!);
            Assert.Equal(3, results.ServerProfiles!.Count);
            Assert.NotNull(results.OperationLimits);
            Assert.Null(results.OperationLimits.MinSupportedSampleRate);
            Assert.Equal(1000u, results.OperationLimits.MaxBrowseContinuationPoints!.Value);
            Assert.Equal(1000u, results.OperationLimits.MaxQueryContinuationPoints!.Value);
            Assert.Equal(1000u, results.OperationLimits.MaxHistoryContinuationPoints!.Value);
            Assert.Null(results.OperationLimits.MaxNodesPerBrowse);
            Assert.Null(results.OperationLimits.MaxNodesPerRegisterNodes);
            Assert.Null(results.OperationLimits.MaxNodesPerWrite);
            Assert.Null(results.OperationLimits.MaxNodesPerMethodCall);
            Assert.Null(results.OperationLimits.MaxNodesPerNodeManagement);
            Assert.Null(results.OperationLimits.MaxMonitoredItemsPerCall);
            Assert.Null(results.OperationLimits.MaxNodesPerHistoryReadData);
            Assert.Null(results.OperationLimits.MaxNodesPerHistoryReadEvents);
            Assert.Null(results.OperationLimits.MaxNodesPerHistoryUpdateData);
            Assert.Null(results.OperationLimits.MaxNodesPerHistoryUpdateEvents);
            Assert.Equal(65535u, results.OperationLimits.MaxArrayLength);
            Assert.Equal(130816u, results.OperationLimits.MaxStringLength);
            Assert.Equal(1048576u, results.OperationLimits.MaxByteStringLength);
            Assert.Null(results.ModellingRules);
        }

        public async Task HistoryGetServerCapabilitiesTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var results = await services.HistoryGetServerCapabilitiesAsync(_connection,
                null, ct).ConfigureAwait(false);

            Assert.NotNull(results);
            //  Assert.NotNull(results.AggregateFunctions);
            //  Assert.Equal(37, results.AggregateFunctions.Count);
            //  Assert.False(results.AccessHistoryDataCapability);
            //  Assert.True(results.AccessHistoryEventsCapability);
            //  Assert.False(results.InsertDataCapability);
            //  Assert.True(results.InsertEventCapability);
            //  Assert.True(results.InsertAnnotationCapability);
            //  Assert.False(results.ReplaceDataCapability);
            //  Assert.True(results.ReplaceEventCapability);
            //  Assert.False(results.DeleteRawCapability);
            //  Assert.False(results.DeleteAtTimeCapability);
            //  Assert.True(results.DeleteEventCapability);
            //  Assert.False(results.ServerTimestampSupported);
            //  Assert.False(results.UpdateDataCapability);
            //  Assert.True(results.UpdateEventCapability);
            //  Assert.Null(results.MaxReturnDataValues);
            //  Assert.NotNull(results.MaxReturnEventValues);
            //  Assert.Equal(uint.MaxValue, results.MaxReturnEventValues.Value);
        }

        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;
    }
}
