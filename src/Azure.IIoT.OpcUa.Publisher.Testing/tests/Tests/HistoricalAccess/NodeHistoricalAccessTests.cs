// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class NodeHistoricalAccessTests<T>
    {
        /// <summary>
        /// Create history services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public NodeHistoricalAccessTests(Func<INodeServices<T>> services, T connection)
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
            Assert.Equal(0, results.OperationLimits.MinSupportedSampleRate);
            Assert.Equal(1000u, results.OperationLimits.MaxBrowseContinuationPoints!.Value);
            Assert.Equal(1000u, results.OperationLimits.MaxQueryContinuationPoints!.Value);
            Assert.Equal(1000u, results.OperationLimits.MaxHistoryContinuationPoints!.Value);
            Assert.Equal(2147483647u, results.OperationLimits.MaxNodesPerBrowse);
            Assert.Equal(2147483647u, results.OperationLimits.MaxNodesPerRegisterNodes);
            Assert.Equal(2147483647u, results.OperationLimits.MaxNodesPerWrite);
            Assert.Equal(2147483647u, results.OperationLimits.MaxNodesPerMethodCall);
            Assert.Equal(2147483647u, results.OperationLimits.MaxNodesPerNodeManagement);
            Assert.Equal(2147483647u, results.OperationLimits.MaxMonitoredItemsPerCall);
            Assert.Equal(2147483647u, results.OperationLimits.MaxNodesPerHistoryReadData);
            Assert.Equal(2147483647u, results.OperationLimits.MaxNodesPerHistoryReadEvents);
            Assert.Equal(2147483647u, results.OperationLimits.MaxNodesPerHistoryUpdateData);
            Assert.Equal(2147483647u, results.OperationLimits.MaxNodesPerHistoryUpdateEvents);
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
            Assert.NotNull(results.AggregateFunctions);
            Assert.Equal(37, results.AggregateFunctions?.Count);
            Assert.True(results.AccessHistoryDataCapability);
            Assert.False(results.AccessHistoryEventsCapability);
            Assert.True(results.InsertDataCapability);
            Assert.False(results.InsertEventCapability);
            Assert.True(results.InsertAnnotationCapability);
            Assert.True(results.ReplaceDataCapability);
            Assert.False(results.ReplaceEventCapability);
            Assert.True(results.DeleteRawCapability);
            Assert.True(results.DeleteAtTimeCapability);
            Assert.False(results.DeleteEventCapability);
            Assert.False(results.ServerTimestampSupported);
            Assert.True(results.UpdateDataCapability);
            Assert.False(results.UpdateEventCapability);
            Assert.NotNull(results.MaxReturnDataValues);
            Assert.Equal(uint.MaxValue, results.MaxReturnDataValues!.Value);
            Assert.Null(results.MaxReturnEventValues);
        }

        public async Task HistoryGetInt16NodeHistoryConfigurationAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int16.txt";

            var results = await services.HistoryGetConfigurationAsync(_connection,
                new HistoryConfigurationRequestModel
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples
                }, ct).ConfigureAwait(false);

            Assert.NotNull(results);
            Assert.NotNull(results.Configuration);
            Assert.NotNull(results.Configuration?.StartOfArchive);
            Assert.NotNull(results.Configuration?.EndOfArchive);
            Assert.NotNull(results.Configuration?.StartOfOnlineArchive);
            Assert.Equal(ExceptionDeviationType.AbsoluteValue, results.Configuration?.ExceptionDeviationType);
            Assert.Equal(0, results.Configuration?.ExceptionDeviation);
            Assert.Null(results.Configuration?.AggregateFunctions);
            Assert.NotNull(results.Configuration?.AggregateConfiguration);
            Assert.Equal((byte)100, results.Configuration?.AggregateConfiguration?.PercentDataGood);
            Assert.Equal((byte)100, results.Configuration?.AggregateConfiguration?.PercentDataBad);
            Assert.False(results.Configuration?.AggregateConfiguration?.UseSlopedExtrapolation);
            Assert.True(results.Configuration?.AggregateConfiguration?.TreatUncertainAsBad);
            Assert.Equal(TimeSpan.FromSeconds(10), results.Configuration?.MinTimeInterval);
            Assert.Equal(TimeSpan.FromSeconds(10), results.Configuration?.MaxTimeInterval);
            Assert.False(results.Configuration?.ServerTimestampSupported);
            Assert.False(results.Configuration?.Stepped);
            Assert.Null(results.ErrorInfo);
        }

        public async Task HistoryGetInt64NodeHistoryConfigurationAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryGetConfigurationAsync(_connection,
                new HistoryConfigurationRequestModel
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples
                }, ct).ConfigureAwait(false);

            Assert.NotNull(results);
            Assert.NotNull(results.Configuration);
            Assert.NotNull(results.Configuration?.StartOfArchive);
            Assert.NotNull(results.Configuration?.EndOfArchive);
            Assert.NotNull(results.Configuration?.StartOfOnlineArchive);
            Assert.Equal(ExceptionDeviationType.AbsoluteValue, results.Configuration?.ExceptionDeviationType);
            Assert.Equal(0, results.Configuration?.ExceptionDeviation);
            Assert.Null(results.Configuration?.AggregateFunctions);
            Assert.NotNull(results.Configuration?.AggregateConfiguration);
            Assert.Equal((byte)100, results.Configuration?.AggregateConfiguration?.PercentDataGood);
            Assert.Equal((byte)100, results.Configuration?.AggregateConfiguration?.PercentDataBad);
            Assert.False(results.Configuration?.AggregateConfiguration?.UseSlopedExtrapolation);
            Assert.True(results.Configuration?.AggregateConfiguration?.TreatUncertainAsBad);
            Assert.Equal(TimeSpan.FromSeconds(10), results.Configuration?.MinTimeInterval);
            Assert.Equal(TimeSpan.FromSeconds(10), results.Configuration?.MaxTimeInterval);
            Assert.False(results.Configuration?.ServerTimestampSupported);
            Assert.True(results.Configuration?.Stepped);
            Assert.Null(results.ErrorInfo);
        }

        public async Task HistoryGetNodeHistoryConfigurationFromBadNodeAsync(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Unknown.txt";

            var results = await services.HistoryGetConfigurationAsync(_connection,
                new HistoryConfigurationRequestModel
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples
                }, ct).ConfigureAwait(false);

            Assert.NotNull(results);
            Assert.Null(results.Configuration);
            Assert.NotNull(results.ErrorInfo);
            // Assert.Equal("BadNodeIdUnknown", results.ErrorInfo.SymbolicId);
            Assert.Equal("BadUnexpectedError", results.ErrorInfo.SymbolicId);
        }

        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;
    }
}
