// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Testing.Tests
{
    using Azure.IIoT.OpcUa.Shared.Models;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class HistoryReadValuesProcessedTests<T>
    {
        /// <summary>
        /// Create history services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public HistoryReadValuesProcessedTests(Func<IHistoryServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task HistoryReadUInt64ProcessedValuesTest1Async()
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.UInt64.txt";

            var now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0, DateTimeKind.Utc);
            var results = await services.HistoryReadProcessedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadProcessedValuesDetailsModel
                    {
                        StartTime = now - TimeSpan.FromHours(1),
                        EndTime = now,
                        ProcessingInterval = TimeSpan.FromMinutes(1),
                        AggregateType = "i=2347"
                    }
                }).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.All(results.History.Where(h => h.Value == null), arg =>
            {
                Assert.Equal("BadNoData", arg.Status.SymbolicId);
                Assert.NotNull(arg.SourceTimestamp);
                Assert.NotNull(arg.ServerTimestamp);
                Assert.Null(arg.Value);
                Assert.Null(arg.DataLocation);
                Assert.Null(arg.AdditionalData);
            });
            var values = results.History.Where(h => h.Value != null).ToList();
            Assert.Equal(2, values.Count);
            Assert.Collection(values,
                arg =>
                {
                    Assert.Equal("UncertainDataSubNormal", arg.Status.SymbolicId);
                    Assert.True(arg.Value == 50);
                    Assert.Equal(DataLocation.Calculated, arg.DataLocation);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Equal(AdditionalData.Partial, arg.AdditionalData);
                },
                arg =>
                {
                    Assert.Equal("UncertainDataSubNormal", arg.Status.SymbolicId);
                    Assert.True(arg.Value == 90);
                    Assert.Equal(DataLocation.Calculated, arg.DataLocation);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Equal(AdditionalData.Partial, arg.AdditionalData);
                });
        }

        public async Task HistoryReadUInt64ProcessedValuesTest2Async()
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.UInt64.txt";

            var now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0, DateTimeKind.Utc);
            var results = await services.HistoryReadProcessedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadProcessedValuesDetailsModel
                    {
                        StartTime = now - TimeSpan.FromHours(1),
                        EndTime = now,
                        ProcessingInterval = TimeSpan.FromMinutes(1),
                        AggregateType = "Count"
                    }
                }).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.All(results.History.Where(h => h.Value == null), arg =>
            {
                Assert.Equal("BadNoData", arg.Status.SymbolicId);
                Assert.NotNull(arg.SourceTimestamp);
                Assert.NotNull(arg.ServerTimestamp);
                Assert.Null(arg.Value);
                Assert.Null(arg.DataLocation);
                Assert.Null(arg.AdditionalData);
            });
            var values = results.History.Where(h => h.Value != null).ToList();
            Assert.Equal(2, values.Count);
            Assert.Collection(values,
                arg =>
                {
                    Assert.Equal("UncertainDataSubNormal", arg.Status.SymbolicId);
                    Assert.True(arg.Value == 6);
                    Assert.Equal(DataLocation.Calculated, arg.DataLocation);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Equal(AdditionalData.Partial, arg.AdditionalData);
                },
                arg =>
                {
                    Assert.Equal("UncertainDataSubNormal", arg.Status.SymbolicId);
                    Assert.True(arg.Value == 4);
                    Assert.Equal(DataLocation.Calculated, arg.DataLocation);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Equal(AdditionalData.Partial, arg.AdditionalData);
                });
        }

        public async Task HistoryReadUInt64ProcessedValuesTest3Async()
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.UInt64.txt";

            var now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0, DateTimeKind.Utc);
            var results = await services.HistoryReadProcessedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadProcessedValuesDetailsModel
                    {
                        StartTime = now - TimeSpan.FromHours(1),
                        EndTime = now,
                        ProcessingInterval = TimeSpan.FromMinutes(20),
                        AggregateType = "Delta"
                    }
                }).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.All(results.History.Where(h => h.Value == null), arg =>
            {
                Assert.Equal("BadNoData", arg.Status.SymbolicId);
                Assert.NotNull(arg.SourceTimestamp);
                Assert.NotNull(arg.ServerTimestamp);
                Assert.Null(arg.Value);
                Assert.Null(arg.DataLocation);
                Assert.Null(arg.AdditionalData);
            });
            var arg = Assert.Single(results.History.Where(h => h.Value != null));
            Assert.Null(arg.Status);
            Assert.True(arg.Value == 80);
            Assert.Equal(DataLocation.Calculated, arg.DataLocation);
            Assert.NotNull(arg.SourceTimestamp);
            Assert.NotNull(arg.ServerTimestamp);
            Assert.Equal(AdditionalData.Partial, arg.AdditionalData);
        }

        public async Task HistoryStreamUInt64ProcessedValuesTest1Async()
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.UInt64.txt";

            var now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0, DateTimeKind.Utc);
            var history = await services.HistoryStreamProcessedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadProcessedValuesDetailsModel
                    {
                        StartTime = now - TimeSpan.FromHours(1),
                        EndTime = now,
                        ProcessingInterval = TimeSpan.FromMinutes(1),
                        AggregateType = "i=2347"
                    }
                }).ToListAsync().ConfigureAwait(false);

            Assert.NotNull(history);
            Assert.All(history.Where(h => h.Value == null), arg =>
            {
                Assert.Equal("BadNoData", arg.Status.SymbolicId);
                Assert.NotNull(arg.SourceTimestamp);
                Assert.NotNull(arg.ServerTimestamp);
                Assert.Null(arg.Value);
                Assert.Null(arg.DataLocation);
                Assert.Null(arg.AdditionalData);
            });
            var values = history.Where(h => h.Value != null).ToList();
            Assert.Equal(2, values.Count);
            Assert.Collection(values,
                arg =>
                {
                    Assert.Equal("UncertainDataSubNormal", arg.Status.SymbolicId);
                    Assert.True(arg.Value == 50);
                    Assert.Equal(DataLocation.Calculated, arg.DataLocation);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Equal(AdditionalData.Partial, arg.AdditionalData);
                },
                arg =>
                {
                    Assert.Equal("UncertainDataSubNormal", arg.Status.SymbolicId);
                    Assert.True(arg.Value == 90);
                    Assert.Equal(DataLocation.Calculated, arg.DataLocation);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Equal(AdditionalData.Partial, arg.AdditionalData);
                });
        }

        public async Task HistoryStreamUInt64ProcessedValuesTest2Async()
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.UInt64.txt";

            var now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0, DateTimeKind.Utc);
            var history = await services.HistoryStreamProcessedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadProcessedValuesDetailsModel
                    {
                        StartTime = now - TimeSpan.FromHours(1),
                        EndTime = now,
                        ProcessingInterval = TimeSpan.FromMinutes(1),
                        AggregateType = "Count"
                    }
                }).ToListAsync().ConfigureAwait(false);

            Assert.NotNull(history);
            Assert.All(history.Where(h => h.Value == null), arg =>
            {
                Assert.Equal("BadNoData", arg.Status.SymbolicId);
                Assert.NotNull(arg.SourceTimestamp);
                Assert.NotNull(arg.ServerTimestamp);
                Assert.Null(arg.Value);
                Assert.Null(arg.DataLocation);
                Assert.Null(arg.AdditionalData);
            });
            var values = history.Where(h => h.Value != null).ToList();
            Assert.Equal(2, values.Count);
            Assert.Collection(values,
                arg =>
                {
                    Assert.Equal("UncertainDataSubNormal", arg.Status.SymbolicId);
                    Assert.True(arg.Value == 6);
                    Assert.Equal(DataLocation.Calculated, arg.DataLocation);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Equal(AdditionalData.Partial, arg.AdditionalData);
                },
                arg =>
                {
                    Assert.Equal("UncertainDataSubNormal", arg.Status.SymbolicId);
                    Assert.True(arg.Value == 4);
                    Assert.Equal(DataLocation.Calculated, arg.DataLocation);
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Equal(AdditionalData.Partial, arg.AdditionalData);
                });
        }

        public async Task HistoryStreamUInt64ProcessedValuesTest3Async()
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.UInt64.txt";

            var now = DateTime.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0, DateTimeKind.Utc);
            var history = await services.HistoryStreamProcessedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadProcessedValuesDetailsModel
                    {
                        StartTime = now - TimeSpan.FromHours(1),
                        EndTime = now,
                        ProcessingInterval = TimeSpan.FromMinutes(10),
                        AggregateType = "Delta"
                    }
                }).ToListAsync().ConfigureAwait(false);

            Assert.NotNull(history);
            Assert.All(history.Where(h => h.Value == null), arg =>
            {
                Assert.Equal("BadNoData", arg.Status.SymbolicId);
                Assert.NotNull(arg.SourceTimestamp);
                Assert.NotNull(arg.ServerTimestamp);
                Assert.Null(arg.Value);
                Assert.Null(arg.DataLocation);
                Assert.Null(arg.AdditionalData);
            });
            var arg = Assert.Single(history.Where(h => h.Value != null));
            Assert.Null(arg.Status);
            Assert.True(arg.Value == 80);
            Assert.Equal(DataLocation.Calculated, arg.DataLocation);
            Assert.NotNull(arg.SourceTimestamp);
            Assert.NotNull(arg.ServerTimestamp);
            Assert.Equal(AdditionalData.Partial, arg.AdditionalData);
        }

        private readonly T _connection;
        private readonly Func<IHistoryServices<T>> _services;
    }
}
