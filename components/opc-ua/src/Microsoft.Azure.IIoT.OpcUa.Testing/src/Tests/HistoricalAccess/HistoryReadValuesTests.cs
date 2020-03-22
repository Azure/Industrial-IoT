// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Tests {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using System.Threading.Tasks;
    using Xunit;
    using System;

    public class HistoryReadValuesTests<T> {

        /// <summary>
        /// Create browse services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="endpoint"></param>
        public HistoryReadValuesTests(Func<IHistorianServices<T>> services, T endpoint) {
            _services = services;
            _endpoint = endpoint;
        }

        public async Task HistoryReadInt64ValuesTest1Async() {

            var services = _services();
            var samples = "s=1:Microsoft.Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_endpoint,
                new HistoryReadRequestModel<ReadValuesDetailsModel> {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel {
                        StartTime = DateTime.UtcNow - TimeSpan.FromDays(600),
                        EndTime = DateTime.UtcNow + TimeSpan.FromDays(1),
                        ReturnBounds = true
                    }
                });

            Assert.NotNull(results.History);
            Assert.Equal(14, results.History.Length);
            Assert.Collection(results.History,
                arg => {
                    Assert.Equal(2161573888, arg.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(10 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(20 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(25 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(30 == arg.Value);
                },
                arg => {
                    Assert.Equal(2147483648, arg.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(40 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(50 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(60 == arg.Value);
                },
                arg => {
                    Assert.Equal(1073741824u, arg.StatusCode);
                    Assert.True(70 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(70 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(80 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(90 == arg.Value);
                },
                arg => {
                    Assert.Equal(2161573888, arg.StatusCode);
                    Assert.Null(arg.Value);
                });
        }

        public async Task HistoryReadInt64ValuesTest2Async() {

            var services = _services();
            var samples = "s=1:Microsoft.Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_endpoint,
                new HistoryReadRequestModel<ReadValuesDetailsModel> {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel {
                        StartTime = DateTime.UtcNow - TimeSpan.FromDays(600),
                        NumValues = 10
                    }
                });

            Assert.NotNull(results.History);
            Assert.Equal(10, results.History.Length);
            Assert.Collection(results.History,
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(10 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(20 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(25 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(30 == arg.Value);
                },
                arg => {
                    Assert.Equal(2147483648u, arg.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(40 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(50 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(60 == arg.Value);
                },
                arg => {
                    Assert.Equal(1073741824u, arg.StatusCode);
                    Assert.True(70 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(70 == arg.Value);
                });
        }

        public async Task HistoryReadInt64ValuesTest3Async() {

            var services = _services();
            var samples = "s=1:Microsoft.Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_endpoint,
                new HistoryReadRequestModel<ReadValuesDetailsModel> {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel {
                        StartTime = DateTime.UtcNow - TimeSpan.FromDays(600),
                        EndTime = DateTime.UtcNow + TimeSpan.FromDays(1),
                    }
                });

            Assert.NotNull(results.History);
            Assert.Equal(12, results.History.Length);
            Assert.Collection(results.History,
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(10 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(20 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(25 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(30 == arg.Value);
                },
                arg => {
                    Assert.Equal(2147483648, arg.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(40 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(50 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(60 == arg.Value);
                },
                arg => {
                    Assert.Equal(1073741824u, arg.StatusCode);
                    Assert.True(70 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(70 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(80 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(90 == arg.Value);
                });
        }

        public async Task HistoryReadInt64ValuesTest4Async() {

            var services = _services();
            var samples = "s=1:Microsoft.Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.Int64.txt";

            var results = await services.HistoryReadValuesAsync(_endpoint,
                new HistoryReadRequestModel<ReadValuesDetailsModel> {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadValuesDetailsModel {
                        EndTime = DateTime.UtcNow + TimeSpan.FromDays(1),
                        NumValues = 10
                    }
                });

            Assert.NotNull(results.History);
            Assert.Equal(10, results.History.Length);
            Assert.Collection(results.History,
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(90 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(80 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(70 == arg.Value);
                },
                arg => {
                    Assert.Equal(1073741824u, arg.StatusCode);
                    Assert.True(70 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(60 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(50 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(40 == arg.Value);
                },
                arg => {
                    Assert.Equal(2147483648, arg.StatusCode);
                    Assert.Null(arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(30 == arg.Value);
                },
                arg => {
                    Assert.Null(arg.StatusCode);
                    Assert.True(25 == arg.Value);
                });
        }

        private readonly T _endpoint;
        private readonly Func<IHistorianServices<T>> _services;
    }
}
