// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Testing.Tests
{
    using Azure.IIoT.OpcUa.Models;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class HistoryReadValuesModifiedTests<T>
    {
        /// <summary>
        /// Create history services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public HistoryReadValuesModifiedTests(Func<IHistoryServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task HistoryReadInt16ValuesModifiedTestAsync()
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.Int16.txt";

            var results = await services.HistoryReadModifiedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadModifiedValuesDetailsModel
                    {
                        StartTime = DateTime.UtcNow - TimeSpan.FromDays(600),
                        EndTime = DateTime.UtcNow + TimeSpan.FromDays(1),
                        NumValues = 14
                    }
                }).ConfigureAwait(false);

            Assert.NotNull(results.History);
            Assert.Equal(0, results.History.Length);
            Assert.Empty(results.History);
        }

        public async Task HistoryStreamInt16ValuesModifiedTestAsync()
        {
            var services = _services();
            const string samples = "s=1:Azure.IIoT.OpcUa.Testing.Servers.HistoricalAccess.Data.Sample.Int16.txt";

            var history = await services.HistoryStreamModifiedValuesAsync(_connection,
                new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
                {
                    NodeId = "http://opcfoundation.org/HistoricalAccess#" + samples,
                    Details = new ReadModifiedValuesDetailsModel
                    {
                        EndTime = DateTime.UtcNow + TimeSpan.FromDays(1),
                        NumValues = 10
                    }
                }).ToListAsync().ConfigureAwait(false);

            Assert.NotNull(history);
            Assert.Equal(0, history.Count);
        }

        private readonly T _connection;
        private readonly Func<IHistoryServices<T>> _services;
    }
}
