// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class HistoryUpdateValuesTests<T>
    {
        /// <summary>
        /// Create history services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public HistoryUpdateValuesTests(Func<IHistoryServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task HistoryInsertUInt32ValuesTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var toUpsert = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMilliseconds(i * 10000))
                .Select(ts => new HistoricValueModel
                {
                    DataType = "Int32",
                    SourceTimestamp = ts,
                    Value = 77
                })
                .ToArray();

            var insert = await services.HistoryInsertValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toUpsert
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(insert.ErrorInfo);
            Assert.Equal(toUpsert.Length, insert.Results?.Count);
            Assert.All(insert.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var read2 = await services.HistoryReadValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = startTime,
                        NumValues = (uint)toUpsert.Length
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read2.ErrorInfo);
            Assert.NotNull(read2.History);
            Assert.Equal(10, read2.History.Length);
            Assert.All(read2.History,
               arg => Assert.True(arg.Value == 77));
        }

        public async Task HistoryInsertUInt32ValuesTest2Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var toUpsert = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMilliseconds(i * 10000))
                .Select(ts => new HistoricValueModel
                {
                    DataType = "Int32",
                    SourceTimestamp = ts,
                    Value = 77
                })
                .ToArray();

            var insert = await services.HistoryInsertValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toUpsert
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(insert.ErrorInfo);
            Assert.Equal(toUpsert.Length, insert.Results?.Count);
            Assert.All(insert.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var insert2 = await services.HistoryInsertValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toUpsert
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(insert2.ErrorInfo);
            Assert.Equal(toUpsert.Length, insert2.Results?.Count);
            Assert.All(insert2.Results!, arg => Assert.Equal("BadEntryExists", arg.SymbolicId));
        }

        public async Task HistoryUpsertUInt32ValuesTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var toUpsert = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMilliseconds(i * 10000))
                .Select(ts => new HistoricValueModel
                {
                    DataType = "Int32",
                    SourceTimestamp = ts,
                    Value = 5
                })
                .ToArray();

            var upsert = await services.HistoryUpsertValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toUpsert
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(upsert.ErrorInfo);
            Assert.Equal(toUpsert.Length, upsert.Results?.Count);
            Assert.All(upsert.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var read2 = await services.HistoryReadValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = startTime,
                        NumValues = (uint)toUpsert.Length
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read2.ErrorInfo);
            Assert.NotNull(read2.History);
            Assert.Equal(10, read2.History.Length);
            Assert.All(read2.History,
               arg => Assert.True(arg.Value == 5));
        }

        public async Task HistoryUpsertUInt32ValuesTest2Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var reqTimes = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMinutes(i)).ToArray();
            var toUpsert = reqTimes
                .Select(ts => new HistoricValueModel
                {
                    DataType = "Int32",
                    SourceTimestamp = ts,
                    Value = 5
                })
                .ToArray();

            var upsert = await services.HistoryInsertValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toUpsert
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(upsert.ErrorInfo);
            Assert.Equal(toUpsert.Length, upsert.Results?.Count);
            Assert.All(upsert.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var toReplace = toUpsert.Select(v => v with { Value = 99 }).ToArray();
            var replace = await services.HistoryUpsertValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toReplace
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(replace.ErrorInfo);
            Assert.Equal(toReplace.Length, replace.Results?.Count);
            Assert.All(replace.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var read2 = await services.HistoryReadValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = startTime,
                        NumValues = (uint)reqTimes.Length
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read2.ErrorInfo);
            Assert.NotNull(read2.History);
            Assert.Equal(10, read2.History.Length);
            Assert.All(read2.History,
               arg => Assert.True(arg.Value == 99));

            var read3 = await services.HistoryReadModifiedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadModifiedValuesDetailsModel
                    {
                        StartTime = startTime,
                        NumValues = (uint)reqTimes.Length
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read3.ErrorInfo);
            Assert.NotNull(read3.History);
            Assert.Equal(10, read3.History.Length);
            Assert.All(read3.History,
               arg => Assert.True(arg.Value == 5));
        }

        public async Task HistoryReplaceUInt32ValuesTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var reqTimes = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMinutes(i)).ToArray();
            var toUpsert = reqTimes
                .Select(ts => new HistoricValueModel
                {
                    DataType = "Int32",
                    SourceTimestamp = ts,
                    Value = 5
                })
                .ToArray();

            var upsert = await services.HistoryInsertValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toUpsert
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(upsert.ErrorInfo);
            Assert.Equal(toUpsert.Length, upsert.Results?.Count);
            Assert.All(upsert.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var toReplace = toUpsert.Select(v => v with { Value = 99 }).ToArray();
            var replace = await services.HistoryReplaceValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toReplace
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(replace.ErrorInfo);
            Assert.Equal(toReplace.Length, replace.Results?.Count);
            Assert.All(replace.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var read2 = await services.HistoryReadValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = startTime,
                        NumValues = (uint)reqTimes.Length
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read2.ErrorInfo);
            Assert.NotNull(read2.History);
            Assert.Equal(10, read2.History.Length);
            Assert.All(read2.History,
               arg => Assert.True(arg.Value == 99));

            var read3 = await services.HistoryReadModifiedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadModifiedValuesDetailsModel
                    {
                        StartTime = startTime,
                        NumValues = (uint)reqTimes.Length * 2
                    }
                }, ct).ConfigureAwait(false);

            Assert.NotNull(read3.History);
            Assert.Equal(20, read3.History.Length);
            Assert.Null(read3.ErrorInfo);
            Assert.All(read3.History,
               arg =>
               {
                   Assert.True(arg.Value == 5);
                   Assert.NotNull(arg.ModificationInfo);
               });
            Assert.Equal(10, read3.History.Count(
                h => h.ModificationInfo?.UpdateType == HistoryUpdateOperation.Insert));
            Assert.Equal(10, read3.History.Count(
                h => h.ModificationInfo?.UpdateType == HistoryUpdateOperation.Replace));
        }

        public async Task HistoryReplaceUInt32ValuesTest2Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var reqTimes = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMinutes(i)).ToArray();
            var toReplace = reqTimes
                .Select(ts => new HistoricValueModel
                {
                    DataType = "Int32",
                    SourceTimestamp = ts,
                    Value = 5
                })
                .ToArray();

            var replace = await services.HistoryReplaceValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toReplace
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(replace.ErrorInfo);
            Assert.Equal(toReplace.Length, replace.Results?.Count);
            Assert.All(replace.Results!, arg => Assert.Equal("BadNoEntryExists", arg.SymbolicId));
        }

        public async Task HistoryInsertDeleteUInt32ValuesTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var reqTimes = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMinutes(i)).ToArray();
            var toUpsert = reqTimes
                .Select(ts => new HistoricValueModel
                {
                    DataType = null, // Discover data type
                    SourceTimestamp = ts,
                    Value = 5
                })
                .ToArray();

            var upsert = await services.HistoryInsertValuesAsync(_connection, new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
            {
                NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                Details = new UpdateValuesDetailsModel
                {
                    Values = toUpsert
                }
            }, ct).ConfigureAwait(false);
            Assert.Null(upsert.ErrorInfo);
            Assert.Equal(toUpsert.Length, upsert.Results?.Count);
            Assert.All(upsert.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var replace = await services.HistoryDeleteValuesAsync(_connection,
                new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new DeleteValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(replace.ErrorInfo);
            Assert.Empty(replace.Results!);

            var read2 = await services.HistoryReadValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);

            Assert.NotNull(read2.ErrorInfo);
            Assert.Equal("GoodNoData", read2.ErrorInfo.SymbolicId);
            Assert.NotNull(read2.History);
            Assert.Empty(read2.History);

            var read3 = await services.HistoryReadModifiedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadModifiedValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read3.ErrorInfo);

            // Insert + Delete = 20
            Assert.NotNull(read3.History);
            Assert.Equal(20, read3.History.Length);
            Assert.All(read3.History,
               arg =>
               {
                   Assert.NotNull(arg.ModificationInfo);
                   Assert.True(arg.Value == 5);
               });
            Assert.Equal(10, read3.History.Count(
                h => h.ModificationInfo?.UpdateType == HistoryUpdateOperation.Insert));
            Assert.Equal(10, read3.History.Count(
                h => h.ModificationInfo?.UpdateType == HistoryUpdateOperation.Delete));
        }

        public async Task HistoryInsertDeleteUInt32ValuesTest2Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var reqTimes = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMinutes(i)).ToArray();
            var toUpsert = reqTimes
                .Select(ts => new HistoricValueModel
                {
                    DataType = "Int32",
                    SourceTimestamp = ts,
                    Value = 66
                })
                .ToArray();

            var upsert = await services.HistoryInsertValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toUpsert
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(upsert.ErrorInfo);
            Assert.Equal(toUpsert.Length, upsert.Results?.Count);
            Assert.All(upsert.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var read2 = await services.HistoryReadModifiedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadModifiedValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read2.ErrorInfo);
            Assert.NotNull(read2.History);
            Assert.Equal(10, read2.History.Length);
            Assert.All(read2.History,
               arg =>
               {
                   Assert.NotNull(arg.ModificationInfo);
                   Assert.True(arg.Value == 66);
                   Assert.Equal(HistoryUpdateOperation.Insert, arg.ModificationInfo?.UpdateType);
               });

            var deleteModified = await services.HistoryDeleteModifiedValuesAsync(_connection,
                new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new DeleteValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(deleteModified.ErrorInfo);
            Assert.Empty(deleteModified.Results!);

            var read3 = await services.HistoryReadModifiedValuesAsync(_connection, new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
            {
                NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                Details = new ReadModifiedValuesDetailsModel
                {
                    StartTime = startTime,
                    EndTime = reqTimes[^1].AddHours(1)
                }
            }, ct).ConfigureAwait(false);

            Assert.NotNull(read3.ErrorInfo);
            Assert.Equal("GoodNoData", read3.ErrorInfo.SymbolicId);
            Assert.NotNull(read3.History);
            Assert.Empty(read3.History);
        }

        public async Task HistoryInsertDeleteUInt32ValuesTest3Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var reqTimes = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMinutes(i)).ToArray();
            var toUpsert = reqTimes
                .Select(ts => new HistoricValueModel
                {
                    DataType = "Int32",
                    SourceTimestamp = ts,
                    Value = 66
                })
                .ToArray();

            var upsert = await services.HistoryInsertValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toUpsert
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(upsert.ErrorInfo);
            Assert.Equal(toUpsert.Length, upsert.Results?.Count);
            Assert.All(upsert.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var read2 = await services.HistoryReadModifiedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadModifiedValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read2.ErrorInfo);
            Assert.NotNull(read2.History);
            Assert.Equal(10, read2.History.Length);
            Assert.All(read2.History,
               arg =>
               {
                   Assert.NotNull(arg.ModificationInfo);
                   Assert.True(arg.Value == 66);
                   Assert.Equal(HistoryUpdateOperation.Insert, arg.ModificationInfo?.UpdateType);
               });

            var delete = await services.HistoryDeleteValuesAsync(_connection,
                new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new DeleteValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(delete.ErrorInfo);
            Assert.Empty(delete.Results!);

            var deleteModified = await services.HistoryDeleteModifiedValuesAsync(_connection,
                new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new DeleteValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(deleteModified.ErrorInfo);
            Assert.Empty(deleteModified.Results!);

            var read3 = await services.HistoryReadModifiedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadModifiedValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);

            Assert.NotNull(read3.ErrorInfo);
            Assert.Equal("GoodNoData", read3.ErrorInfo.SymbolicId); // TODO: Check this
            Assert.NotNull(read3.History);
            Assert.Empty(read3.History);
        }

        public async Task HistoryInsertDeleteUInt32ValuesTest4Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var reqTimes = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMinutes(i)).ToArray();
            var toInsert = reqTimes
                .Select(ts => new HistoricValueModel
                {
                    DataType = "Int32",
                    SourceTimestamp = ts,
                    Value = 88
                })
                .ToArray();

            var upsert = await services.HistoryInsertValuesAsync(_connection,
                new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new UpdateValuesDetailsModel
                    {
                        Values = toInsert
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(upsert.ErrorInfo);
            Assert.Equal(toInsert.Length, upsert.Results?.Count);
            Assert.All(upsert.Results!, arg => Assert.Equal("Good", arg.SymbolicId));

            var read2 = await services.HistoryReadModifiedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadModifiedValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read2.ErrorInfo);
            Assert.NotNull(read2.History);
            Assert.Equal(10, read2.History.Length);
            Assert.All(read2.History,
               arg =>
               {
                   Assert.NotNull(arg.ModificationInfo);
                   Assert.NotNull(arg.SourceTimestamp);
                   Assert.True(arg.Value == 88);
                   Assert.Equal(HistoryUpdateOperation.Insert, arg.ModificationInfo?.UpdateType);
               });

            var delete = await services.HistoryDeleteValuesAtTimesAsync(_connection,
                new HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new DeleteValuesAtTimesDetailsModel
                    {
                        ReqTimes = new[] { startTime }
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(delete.ErrorInfo);
            var arg = Assert.Single(delete.Results!);
            Assert.Equal("Good", arg.SymbolicId);

            var read4 = await services.HistoryReadValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read4.ErrorInfo);
            Assert.NotNull(read4.History);
            Assert.Equal(9, read4.History.Length);
            Assert.All(read4.History,
               arg => Assert.True(arg.Value == 88));

            var read3 = await services.HistoryReadModifiedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadModifiedValuesDetailsModel
                    {
                        StartTime = startTime,
                        EndTime = reqTimes[^1].AddHours(1)
                    }
                }, ct).ConfigureAwait(false);

            Assert.Null(read3.ErrorInfo);

            // Insert + Delete = 11
            Assert.NotNull(read3.History);
            Assert.Equal(11, read3.History.Length);
            Assert.All(read3.History,
               arg =>
               {
                   Assert.NotNull(arg.ModificationInfo);
                   Assert.True(arg.Value == 88);
               });
            Assert.Equal(10, read3.History.Count(
                h => h.ModificationInfo?.UpdateType == HistoryUpdateOperation.Insert));
            Assert.Single(read3.History.Where(
                h => h.ModificationInfo?.UpdateType == HistoryUpdateOperation.Delete));
        }

        public async Task HistoryDeleteUInt32ValuesTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int32.txt";

            var startTime = StartTime();
            var reqTimes = Enumerable.Repeat(0, 10)
                .Select((_, i) => startTime.AddMinutes(i)).ToArray();
            var delete = await services.HistoryDeleteValuesAtTimesAsync(_connection,
                new HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new DeleteValuesAtTimesDetailsModel
                    {
                        ReqTimes = reqTimes
                    }
                }, ct).ConfigureAwait(false);
            Assert.Null(delete.ErrorInfo);
            Assert.Equal(10, delete.Results?.Count);
            Assert.All(delete.Results!, arg => Assert.Equal("BadNoEntryExists", arg.SymbolicId));
        }

        private static DateTime StartTime()
        {
            return new DateTime(1973, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddYears(Interlocked.Increment(ref _counter));
        }

        private static int _counter;
        private readonly T _connection;
        private readonly Func<IHistoryServices<T>> _services;
    }
}
