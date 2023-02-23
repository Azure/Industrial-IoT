// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Testing.Tests {
    using Azure.IIoT.OpcUa.Shared.Models;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public class HistoryReadValuesTests<T> {
        /// <summary>
        /// Create history services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="endpoint"></param>
        public HistoryReadValuesTests(Func<IHistoryServices<T>> services, T endpoint) {
            _services = services;
            _endpoint = endpoint;
        }

        public async Task HistoryReadInt64ValuesTest1Async() {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_endpoint,
                new HistoryReadRequestModel<ReadValuesDetailsModel> {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel {
                        StartTime = DateTime.UtcNow - TimeSpan.FromDays(600),
                        EndTime = DateTime.UtcNow + TimeSpan.FromDays(1),
                        ReturnBounds = true
                    }
                }).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(14, results.History.Length);
            Assert.Collection(results.History,
                arg => {
                    Assert.Equal(2161573888, arg.Status.StatusCode);
                    Assert.Null(arg.Value);
                    Assert.Null(arg.DataLocation);
                    Assert.Null(arg.AdditionalData);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 20);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg => {
                    Assert.Equal(2147483648, arg.Status.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg => {
                    Assert.Equal(1073741824u, arg.Status.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 80);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 90);
                },
                arg => {
                    Assert.Equal(2161573888, arg.Status.StatusCode);
                    Assert.Null(arg.Value);
                });
            Assert.All(results.History,
                arg => {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        public async Task HistoryReadInt64ValuesTest2Async() {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_endpoint,
                new HistoryReadRequestModel<ReadValuesDetailsModel> {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel {
                        StartTime = DateTime.UtcNow - TimeSpan.FromDays(600),
                        NumValues = 10
                    }
                }).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(10, results.History.Length);
            Assert.Collection(results.History,
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 20);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg => {
                    Assert.Equal(2147483648u, arg.Status.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg => {
                    Assert.Equal(1073741824u, arg.Status.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                });
            Assert.All(results.History,
                arg => {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        public async Task HistoryReadInt64ValuesTest3Async() {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_endpoint,
                new HistoryReadRequestModel<ReadValuesDetailsModel> {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel {
                        StartTime = DateTime.UtcNow - TimeSpan.FromDays(600),
                        EndTime = DateTime.UtcNow + TimeSpan.FromDays(1),
                    }
                }).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(12, results.History.Length);
            Assert.Collection(results.History,
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 10);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 20);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg => {
                    Assert.Equal(2147483648, arg.Status.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg => {
                    Assert.Equal(1073741824u, arg.Status.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 80);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 90);
                });
            Assert.All(results.History,
                arg => {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        public async Task HistoryReadInt64ValuesTest4Async() {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_endpoint,
                new HistoryReadRequestModel<ReadValuesDetailsModel> {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel {
                        EndTime = DateTime.UtcNow + TimeSpan.FromDays(1),
                        NumValues = 10
                    }
                }).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(10, results.History.Length);
            Assert.Collection(results.History,
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 90);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 80);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 70);
                },
                arg => {
                    Assert.Equal(1073741824u, arg.Status.StatusCode);
                    Assert.True(arg.Value == 70);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 60);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 50);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 40);
                },
                arg => {
                    Assert.Equal(2147483648, arg.Status.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 30);
                },
                arg => {
                    Assert.Null(arg.Status);
                    Assert.True(arg.Value == 25);
                });
            Assert.All(results.History,
                arg => {
                    Assert.NotNull(arg.SourceTimestamp);
                    Assert.NotNull(arg.ServerTimestamp);
                    Assert.Null(arg.AdditionalData);
                    Assert.Null(arg.DataLocation);
                });
        }

        private readonly T _endpoint;
        private readonly Func<IHistoryServices<T>> _services;
    }
}
