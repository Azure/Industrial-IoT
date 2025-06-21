// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class HistoryReadValuesTests<T>
    {
        /// <summary>
        /// Create history services tests
        /// </summary>
        /// <param name="server"></param>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public HistoryReadValuesTests(BaseServerFixture server,
            Func<IHistoryServices<T>> services, T connection)
        {
            _server = server;
            _services = services;
            _connection = connection;
        }

        public async Task HistoryReadInt64ValuesTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = _server.Now - TimeSpan.FromDays(600),
                        EndTime = _server.Now + TimeSpan.FromDays(1),
                        ReturnBounds = true
                    }
                }, ct).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(14, results.History.Length);
            Assert.Collection(results.History,
                arg =>
                {
                    Assert.Equal(2161573888, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                    Assert.Null(arg.DataLocation);
                    Assert.Null(arg.AdditionalData);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 20);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg =>
                {
                    Assert.Equal(2147483648, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg =>
                {
                    Assert.Equal(1073741824u, arg.Status?.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 80);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 90);
                },
                arg =>
                {
                    Assert.Equal(2161573888, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                });
            Assert.All(results.History,
                arg =>
                {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        public async Task HistoryReadInt64ValuesTest2Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = _server.Now - TimeSpan.FromDays(600),
                        NumValues = 10
                    }
                }, ct).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(10, results.History.Length);
            Assert.Collection(results.History,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 20);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg =>
                {
                    Assert.Equal(2147483648u, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg =>
                {
                    Assert.Equal(1073741824u, arg.Status?.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                });
            Assert.All(results.History,
                arg =>
                {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        public async Task HistoryReadInt64ValuesTest3Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = _server.Now - TimeSpan.FromDays(600),
                        EndTime = _server.Now + TimeSpan.FromDays(1)
                    }
                }, ct).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(12, results.History.Length);
            Assert.Collection(results.History,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 20);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg =>
                {
                    Assert.Equal(2147483648, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg =>
                {
                    Assert.Equal(1073741824u, arg.Status?.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 80);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 90);
                });
            Assert.All(results.History,
                arg =>
                {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        public async Task HistoryReadInt64ValuesTest4Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        EndTime = _server.Now + TimeSpan.FromDays(1),
                        NumValues = 10
                    }
                }, ct).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(10, results.History.Length);
            Assert.Collection(results.History,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 90);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 80);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Equal(1073741824u, arg.Status?.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg =>
                {
                    Assert.Equal(2147483648, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                });
            Assert.All(results.History,
                arg =>
                {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        public async Task HistoryStreamInt64ValuesTest1Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var history = await services.HistoryStreamValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = _server.Now - TimeSpan.FromDays(600),
                        EndTime = _server.Now + TimeSpan.FromDays(1),
                        ReturnBounds = true
                    }
                }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            Assert.NotNull(history);
            Assert.Equal(14, history.Count);
            Assert.Collection(history,
                arg =>
                {
                    Assert.Equal(2161573888, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 20);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg =>
                {
                    Assert.Equal(2147483648, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg =>
                {
                    Assert.Equal(1073741824u, arg.Status?.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 80);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 90);
                },
                arg =>
                {
                    Assert.Equal(2161573888, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                });
            Assert.All(history,
                arg =>
                {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        public async Task HistoryStreamInt64ValuesTest2Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var history = await services.HistoryStreamValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = _server.Now - TimeSpan.FromDays(600),
                        NumValues = 10
                    }
                }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            Assert.NotNull(history);
            Assert.Equal(10, history.Count);
            Assert.Collection(history,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 20);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg =>
                {
                    Assert.Equal(2147483648u, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg =>
                {
                    Assert.Equal(1073741824u, arg.Status?.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                });
            Assert.All(history,
                arg =>
                {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        public async Task HistoryStreamInt64ValuesTest3Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var history = await services.HistoryStreamValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        StartTime = _server.Now - TimeSpan.FromDays(600),
                        EndTime = _server.Now + TimeSpan.FromDays(1)
                    }
                }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            Assert.NotNull(history);
            Assert.Equal(12, history.Count);
            Assert.Collection(history,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 20);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg =>
                {
                    Assert.Equal(2147483648, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg =>
                {
                    Assert.Equal(1073741824u, arg.Status?.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 80);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 90);
                });
            Assert.All(history,
                arg =>
                {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        public async Task HistoryStreamInt64ValuesTest4Async(CancellationToken ct = default)
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Publisher.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var history = await services.HistoryStreamValuesAsync(_connection,
                new HistoryReadRequestModel<ReadValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel
                    {
                        EndTime = _server.Now + TimeSpan.FromDays(1),
                        NumValues = 10
                    }
                }, ct).ToListAsync(cancellationToken: ct).ConfigureAwait(false);

            Assert.NotNull(history);
            Assert.Equal(10, history.Count);
            Assert.Collection(history,
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 90);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 80);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Equal(1073741824u, arg.Status?.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg =>
                {
                    Assert.Equal(2147483648, arg.Status?.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg =>
                {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                });
            Assert.All(history,
                arg =>
                {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        private readonly T _connection;
        private readonly BaseServerFixture _server;
        private readonly Func<IHistoryServices<T>> _services;
    }
}
