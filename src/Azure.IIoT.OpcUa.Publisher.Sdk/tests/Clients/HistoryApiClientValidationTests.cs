// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Tests.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Every history method validates four things - the connection, its
    /// endpoint url, the request and the request's details - and this sweeps
    /// all seventeen rather than picking representatives.
    /// </summary>
    /// <remarks>
    /// The details are what say <em>which</em> history operation is meant, so a
    /// request without them is not an incomplete call, it is an ambiguous one.
    /// Catching that locally is the difference between a clear argument error
    /// and a server-side rejection that has to be traced back.
    /// </remarks>
    public sealed class HistoryApiClientValidationTests : ApiClientTestBase
    {
        private HistoryApiClient Client => new(MethodClient.Object, Target);

        /// <summary>
        /// Each method invoked with a caller-supplied connection and a request
        /// that is otherwise valid.
        /// </summary>
        private static IEnumerable<(string Name, Func<HistoryApiClient, ConnectionModel, Task> Call)> WithConnection()
        {
            yield return ("HistoryReadValues", (c, x) => c.HistoryReadValuesAsync(x, Read(new ReadValuesDetailsModel()), default));
            yield return ("HistoryReadModifiedValues", (c, x) => c.HistoryReadModifiedValuesAsync(x, Read(new ReadModifiedValuesDetailsModel()), default));
            yield return ("HistoryReadValuesAtTimes", (c, x) => c.HistoryReadValuesAtTimesAsync(x, Read(new ReadValuesAtTimesDetailsModel { ReqTimes = [] }), default));
            yield return ("HistoryReadProcessedValues", (c, x) => c.HistoryReadProcessedValuesAsync(x, Read(new ReadProcessedValuesDetailsModel()), default));
            yield return ("HistoryReadValuesNext", (c, x) => c.HistoryReadValuesNextAsync(x, Next(), default));
            yield return ("HistoryReplaceValues", (c, x) => c.HistoryReplaceValuesAsync(x, Update(new UpdateValuesDetailsModel { Values = [] }), default));
            yield return ("HistoryInsertValues", (c, x) => c.HistoryInsertValuesAsync(x, Update(new UpdateValuesDetailsModel { Values = [] }), default));
            yield return ("HistoryUpsertValues", (c, x) => c.HistoryUpsertValuesAsync(x, Update(new UpdateValuesDetailsModel { Values = [] }), default));
            yield return ("HistoryDeleteValues", (c, x) => c.HistoryDeleteValuesAsync(x, Update(new DeleteValuesDetailsModel()), default));
            yield return ("HistoryDeleteModifiedValues", (c, x) => c.HistoryDeleteModifiedValuesAsync(x, Update(new DeleteValuesDetailsModel()), default));
            yield return ("HistoryDeleteValuesAtTimes", (c, x) => c.HistoryDeleteValuesAtTimesAsync(x, Update(new DeleteValuesAtTimesDetailsModel { ReqTimes = [] }), default));
            yield return ("HistoryReadEvents", (c, x) => c.HistoryReadEventsAsync(x, Read(new ReadEventsDetailsModel()), default));
            yield return ("HistoryReadEventsNext", (c, x) => c.HistoryReadEventsNextAsync(x, Next(), default));
            yield return ("HistoryReplaceEvents", (c, x) => c.HistoryReplaceEventsAsync(x, Update(new UpdateEventsDetailsModel { Events = [] }), default));
            yield return ("HistoryInsertEvents", (c, x) => c.HistoryInsertEventsAsync(x, Update(new UpdateEventsDetailsModel { Events = [] }), default));
            yield return ("HistoryUpsertEvents", (c, x) => c.HistoryUpsertEventsAsync(x, Update(new UpdateEventsDetailsModel { Events = [] }), default));
            yield return ("HistoryDeleteEvents", (c, x) => c.HistoryDeleteEventsAsync(x, Update(new DeleteEventsDetailsModel { EventIds = [] }), default));
        }

        /// <summary>
        /// Each method invoked with a valid connection and a null request.
        /// </summary>
        private static IEnumerable<(string Name, Func<HistoryApiClient, Task> Call)> WithNullRequest()
        {
            var c = Valid();
            yield return ("HistoryReadValues", x => x.HistoryReadValuesAsync(c, null!, default));
            yield return ("HistoryReadModifiedValues", x => x.HistoryReadModifiedValuesAsync(c, null!, default));
            yield return ("HistoryReadValuesAtTimes", x => x.HistoryReadValuesAtTimesAsync(c, null!, default));
            yield return ("HistoryReadProcessedValues", x => x.HistoryReadProcessedValuesAsync(c, null!, default));
            yield return ("HistoryReadValuesNext", x => x.HistoryReadValuesNextAsync(c, null!, default));
            yield return ("HistoryReplaceValues", x => x.HistoryReplaceValuesAsync(c, null!, default));
            yield return ("HistoryInsertValues", x => x.HistoryInsertValuesAsync(c, null!, default));
            yield return ("HistoryUpsertValues", x => x.HistoryUpsertValuesAsync(c, null!, default));
            yield return ("HistoryDeleteValues", x => x.HistoryDeleteValuesAsync(c, null!, default));
            yield return ("HistoryDeleteModifiedValues", x => x.HistoryDeleteModifiedValuesAsync(c, null!, default));
            yield return ("HistoryDeleteValuesAtTimes", x => x.HistoryDeleteValuesAtTimesAsync(c, null!, default));
            yield return ("HistoryReadEvents", x => x.HistoryReadEventsAsync(c, null!, default));
            yield return ("HistoryReadEventsNext", x => x.HistoryReadEventsNextAsync(c, null!, default));
            yield return ("HistoryReplaceEvents", x => x.HistoryReplaceEventsAsync(c, null!, default));
            yield return ("HistoryInsertEvents", x => x.HistoryInsertEventsAsync(c, null!, default));
            yield return ("HistoryUpsertEvents", x => x.HistoryUpsertEventsAsync(c, null!, default));
            yield return ("HistoryDeleteEvents", x => x.HistoryDeleteEventsAsync(c, null!, default));
        }

        /// <summary>
        /// Each method invoked with a request whose details are absent.
        /// </summary>
        private static IEnumerable<(string Name, Func<HistoryApiClient, Task> Call)> WithNoDetails()
        {
            var c = Valid();
            yield return ("HistoryReadValues", x => x.HistoryReadValuesAsync(c, Read<ReadValuesDetailsModel>(null!), default));
            yield return ("HistoryReadModifiedValues", x => x.HistoryReadModifiedValuesAsync(c, Read<ReadModifiedValuesDetailsModel>(null!), default));
            yield return ("HistoryReadValuesAtTimes", x => x.HistoryReadValuesAtTimesAsync(c, Read<ReadValuesAtTimesDetailsModel>(null!), default));
            yield return ("HistoryReadProcessedValues", x => x.HistoryReadProcessedValuesAsync(c, Read<ReadProcessedValuesDetailsModel>(null!), default));
            yield return ("HistoryReplaceValues", x => x.HistoryReplaceValuesAsync(c, Update<UpdateValuesDetailsModel>(null!), default));
            yield return ("HistoryInsertValues", x => x.HistoryInsertValuesAsync(c, Update<UpdateValuesDetailsModel>(null!), default));
            yield return ("HistoryUpsertValues", x => x.HistoryUpsertValuesAsync(c, Update<UpdateValuesDetailsModel>(null!), default));
            yield return ("HistoryDeleteValues", x => x.HistoryDeleteValuesAsync(c, Update<DeleteValuesDetailsModel>(null!), default));
            yield return ("HistoryDeleteModifiedValues", x => x.HistoryDeleteModifiedValuesAsync(c, Update<DeleteValuesDetailsModel>(null!), default));
            yield return ("HistoryDeleteValuesAtTimes", x => x.HistoryDeleteValuesAtTimesAsync(c, Update<DeleteValuesAtTimesDetailsModel>(null!), default));
            yield return ("HistoryReadEvents", x => x.HistoryReadEventsAsync(c, Read<ReadEventsDetailsModel>(null!), default));
            yield return ("HistoryReplaceEvents", x => x.HistoryReplaceEventsAsync(c, Update<UpdateEventsDetailsModel>(null!), default));
            yield return ("HistoryInsertEvents", x => x.HistoryInsertEventsAsync(c, Update<UpdateEventsDetailsModel>(null!), default));
            yield return ("HistoryUpsertEvents", x => x.HistoryUpsertEventsAsync(c, Update<UpdateEventsDetailsModel>(null!), default));
            yield return ("HistoryDeleteEvents", x => x.HistoryDeleteEventsAsync(c, Update<DeleteEventsDetailsModel>(null!), default));
        }

        [Fact]
        public async Task EveryMethodRejectsANullConnectionAsync()
        {
            Assert.Empty(await SweepAsync(WithConnection().Select(m =>
                (m.Name, new Func<HistoryApiClient, Task>(x => m.Call(x, null!)))),
                typeof(ArgumentNullException)));
        }

        [Fact]
        public async Task EveryMethodRejectsAConnectionWithNoEndpointUrlAsync()
        {
            Assert.Empty(await SweepAsync(WithConnection().Select(m =>
                (m.Name, new Func<HistoryApiClient, Task>(x => m.Call(x, new ConnectionModel
                {
                    Endpoint = new EndpointModel { Url = string.Empty }
                })))),
                typeof(ArgumentException)));
        }

        [Fact]
        public async Task EveryMethodRejectsANullRequestAsync()
        {
            Assert.Empty(await SweepAsync(WithNullRequest(), typeof(ArgumentNullException)));
        }

        [Fact]
        public async Task EveryMethodRejectsARequestWithNoDetailsAsync()
        {
            Assert.Empty(await SweepAsync(WithNoDetails(), typeof(ArgumentException)));
        }

        [Fact]
        public void TheSweepCoversEveryPublicClientMethod()
        {
            var swept = WithConnection().Select(m => m.Name).ToHashSet(StringComparer.Ordinal);
            var declared = typeof(HistoryApiClient).GetMethods()
                .Where(m => m.IsPublic && !m.IsStatic && m.Name.EndsWith("Async", StringComparison.Ordinal))
                .Select(m => m.Name[..^"Async".Length])
                .ToHashSet(StringComparer.Ordinal);

            Assert.Empty(declared.Except(swept));
        }

        private async Task<List<string>> SweepAsync(
            IEnumerable<(string Name, Func<HistoryApiClient, Task> Call)> methods, Type expected)
        {
            var offenders = new List<string>();
            foreach (var (name, call) in methods)
            {
                try
                {
                    await call(Client);
                    offenders.Add($"{name}: no exception");
                }
                catch (Exception ex) when (expected.IsInstanceOfType(ex))
                {
                }
                catch (Exception ex)
                {
                    offenders.Add($"{name}: {ex.GetType().Name}");
                }
            }
            AssertNotCalled();
            return offenders;
        }

        private static HistoryReadRequestModel<T> Read<T>(T details) where T : class
        {
            return new HistoryReadRequestModel<T> { Details = details, NodeId = "i=1" };
        }

        private static HistoryReadNextRequestModel Next()
        {
            return new HistoryReadNextRequestModel { ContinuationToken = "token" };
        }

        private static HistoryUpdateRequestModel<T> Update<T>(T details) where T : class
        {
            return new HistoryUpdateRequestModel<T> { Details = details, NodeId = "i=1" };
        }

        private static ConnectionModel Valid()
        {
            return new ConnectionModel
            {
                Endpoint = new EndpointModel { Url = "opc.tcp://server:4840" }
            };
        }
    }
}
