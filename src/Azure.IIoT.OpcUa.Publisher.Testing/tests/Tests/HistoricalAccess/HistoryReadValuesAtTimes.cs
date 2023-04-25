// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class HistoryReadValuesAtTimesTests<T>
    {
        /// <summary>
        /// Create history services tests
        /// </summary>
        /// <param name="server"></param>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public HistoryReadValuesAtTimesTests(BaseServerFixture server,
            Func<IHistoryServices<T>> services, T connection)
        {
            _server = server;
            _services = services;
            _connection = connection;
        }

        public async Task HistoryReadInt32ValuesAtTimesTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = _server.Now.Subtract(TimeSpan.FromHours(1));
            var results = await services.HistoryReadValuesAtTimesAsync(_connection, new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                Details = new ReadValuesAtTimesDetailsModel
                {
                    ReqTimes = Enumerable.Repeat(0, 10).Select((_, i) => startTime.AddMilliseconds(i * 10000)).ToArray(),
                    UseSimpleBounds = true
                }
            }, ct).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(10, results.History.Length);
            Assert.All(results.History,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Equal(DataLocation.Interpolated, arg.DataLocation);
                });
        }

        public async Task HistoryReadInt32ValuesAtTimesTest2Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = _server.Now.Subtract(TimeSpan.FromHours(1));
            var results = await services.HistoryReadValuesAtTimesAsync(_connection, new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                Details = new ReadValuesAtTimesDetailsModel
                {
                    ReqTimes = Enumerable.Repeat(0, 10).Select((_, i) => startTime.AddMilliseconds(i * 10000)).ToArray(),
                    UseSimpleBounds = false
                }
            }, ct).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(10, results.History.Length);
            Assert.All(results.History,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Equal(DataLocation.Interpolated, arg.DataLocation);
                });
        }

        public async Task HistoryReadInt32ValuesAtTimesTest3Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = _server.Now.Subtract(TimeSpan.FromHours(1));
            var results = await services.HistoryReadValuesAtTimesAsync(_connection, new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                Details = new ReadValuesAtTimesDetailsModel
                {
                    ReqTimes = Enumerable.Repeat(0, 1).Select((_, i) => startTime.AddMilliseconds(i * 10000)).ToArray(),
                    UseSimpleBounds = true
                }
            }, ct).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(1, results.History.Length);
            Assert.All(results.History,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Equal(DataLocation.Interpolated, arg.DataLocation);
                });
        }

        public async Task HistoryReadInt32ValuesAtTimesTest4Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = _server.Now.Subtract(TimeSpan.FromHours(1));
            var results = await services.HistoryReadValuesAtTimesAsync(_connection, new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                TimestampsToReturn = TimestampsToReturn.Source,
                Details = new ReadValuesAtTimesDetailsModel
                {
                    ReqTimes = Enumerable.Repeat(0, 1).Select((_, i) => startTime.AddMilliseconds(i * 10000)).ToArray(),
                    UseSimpleBounds = false
                }
            }, ct).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(1, results.History.Length);
            Assert.All(results.History,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                    Assert.NotNull(arg.SourceTimestamp);
                    //   Assert.Null(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Equal(DataLocation.Interpolated, arg.DataLocation);
                });
        }

        public async Task HistoryStreamInt32ValuesAtTimesTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = _server.Now.Subtract(TimeSpan.FromHours(5));
            var history = await services.HistoryStreamValuesAtTimesAsync(_connection, new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                Details = new ReadValuesAtTimesDetailsModel
                {
                    ReqTimes = Enumerable.Repeat(0, 500).Select((_, i) => startTime.AddMilliseconds(i * 10000)).ToArray(),
                    UseSimpleBounds = false
                }
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            Assert.NotNull(history);
            Assert.Equal(500, history.Count);
            Assert.All(history,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                    Assert.Equal(DataLocation.Interpolated, arg.DataLocation);
                });
        }

        public async Task HistoryStreamInt32ValuesAtTimesTest2Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = _server.Now.Subtract(TimeSpan.FromHours(5));
            var history = await services.HistoryStreamValuesAtTimesAsync(_connection, new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                Details = new ReadValuesAtTimesDetailsModel
                {
                    ReqTimes = Enumerable.Repeat(0, 500).Select((_, i) => startTime.AddMilliseconds(i * 10000)).ToArray(),
                    UseSimpleBounds = true
                }
            }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            Assert.NotNull(history);
            Assert.Equal(500, history.Count);
            Assert.All(history,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                    Assert.Equal(DataLocation.Interpolated, arg.DataLocation);
                });
        }
        private readonly T _connection;
        private readonly BaseServerFixture _server;
        private readonly Func<IHistoryServices<T>> _services;
    }
}
